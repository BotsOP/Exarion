using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using UI;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;


public enum PaintType
{
    PaintUnderOwnLine,
    PaintUnderEverything,
    PaintOverOwnLine,
    PaintOverEverything,
    Erase
}


namespace Drawing
{
    public class Drawing : DrawingLib
    {
        private ComputeShader paintShader;
        private int paintUnderOwnLineKernelID;
        private int paintUnderEverythingKernelID;
        private int paintOverEverythingKernelID;
        private int paintOverOwnLineKernelID;
        
        public Drawing(int _imageWidth, int _imageHeight) : base(_imageWidth, _imageHeight)
        {
            paintShader = Resources.Load<ComputeShader>("DrawingShader");
            paintUnderOwnLineKernelID = paintShader.FindKernel("PaintUnderOwnLine");
            paintUnderEverythingKernelID = paintShader.FindKernel("PaintUnderEverything");
            paintOverEverythingKernelID = paintShader.FindKernel("PaintOverEverything");
            paintOverOwnLineKernelID = paintShader.FindKernel("PaintOverOwnLine");
            
            addRT();
            addRTID();
            addRTWholeTemp();
            addRTWholeIDTemp();
        }
    
        public virtual void Draw(Vector2 _lastPos, Vector2 _currentPos, float _strokeBrushSize, PaintType _paintType, float _lastTime = 0, float _brushTime = 0, bool _firstStroke = false, int _strokeID = 0)
        {
            int threadGroupX = Mathf.CeilToInt((math.abs(_lastPos.x - _currentPos.x) + _strokeBrushSize * 2) / threadGroupSizeOut.x);
            int threadGroupY = Mathf.CeilToInt((math.abs(_lastPos.y - _currentPos.y) + _strokeBrushSize * 2) / threadGroupSizeOut.y);
        
            Vector2 startPos = GetStartPos(_lastPos, _currentPos, _strokeBrushSize);

            int kernelID = 0;
            switch (_paintType)
            {
                case PaintType.PaintUnderEverything:
                    kernelID = paintUnderEverythingKernelID;
                    Vector2 AtoB = _currentPos - _lastPos;
                    AtoB = AtoB.normalized;

                    Vector2 lastPos = _lastPos - AtoB;
                    Vector2 currentPos = _currentPos + AtoB;
                    paintShader.SetVector("_CursorPosPlusBrushSize", currentPos);
                    paintShader.SetVector("_LastCursorPosPlusBrushSize", lastPos);
                    break;
                case PaintType.PaintOverEverything:
                    kernelID = paintOverEverythingKernelID;
                    break;
                case PaintType.PaintOverOwnLine:
                    kernelID = paintOverOwnLineKernelID;
                    break;
                case PaintType.PaintUnderOwnLine:
                    kernelID = paintUnderOwnLineKernelID;
                    break;
            }

            paintShader.SetBool("_FirstStroke", _firstStroke);
            paintShader.SetVector("_CursorPos", _currentPos);
            paintShader.SetVector("_LastCursorPos", _lastPos);
            paintShader.SetVector("_StartPos", startPos);
            paintShader.SetFloat("_BrushSize", _strokeBrushSize);
            paintShader.SetFloat("_TimeColor", _brushTime);
            paintShader.SetFloat("_PreviousTimeColor", _lastTime);
            paintShader.SetInt("_StrokeID", _strokeID);
            paintShader.SetTexture(kernelID, "_IDTex", rtWholeIDTemps[0]);
            paintShader.SetTexture(kernelID, "_ResultTex", rtWholeTemps[0]);
            
            paintShader.Dispatch(kernelID, threadGroupX, threadGroupY, 1);
        }
        
        private Vector2 GetStartPos(Vector2 a, Vector2 b, float _brushSize)
        {
            float lowestX = (a.x < b.x ? a.x : b.x) - _brushSize;
            float lowestY = (a.y < b.y ? a.y : b.y) - _brushSize;
            return new Vector2(lowestX, lowestY);
        }
        
        public void SetupDrawBrushStroke(BrushStrokeID _brushStrokeID, bool _getPixels = true)
        {
            float oldStartTime = _brushStrokeID.brushStrokes[0].startTime;
            float oldEndTime = _brushStrokeID.brushStrokes[^1].endTime;
            
            bool firstStroke = true;
            foreach (var brushStroke in _brushStrokeID.brushStrokes)
            {
                float startTime = brushStroke.startTime.Remap(oldStartTime, oldEndTime, _brushStrokeID.startTime, _brushStrokeID.endTime);
                float endTime = brushStroke.endTime.Remap(oldStartTime, oldEndTime, _brushStrokeID.startTime, _brushStrokeID.endTime);
                
                Draw(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, _brushStrokeID.paintType, 
                     startTime, endTime, firstStroke, _brushStrokeID.indexWhenDrawn);
                firstStroke = false;
            }
            
            (List<BrushStrokePixel[]>, List<uint[]>) result = FinishDrawing(_brushStrokeID.indexWhenDrawn, _getPixels);
            _brushStrokeID.pixels = result.Item1;
            _brushStrokeID.bounds = result.Item2;
        }
        public void SetupDrawBrushStroke(List<BrushStrokeID> _brushStrokeIDs, bool _getPixels = true)
        {
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                float oldStartTime = brushStrokeID.brushStrokes[0].startTime;
                float oldEndTime = brushStrokeID.brushStrokes[^1].endTime;
            
                bool firstStroke = true;
                foreach (var brushStroke in brushStrokeID.brushStrokes)
                {
                    float startTime = brushStroke.startTime.Remap(oldStartTime, oldEndTime, brushStrokeID.startTime, brushStrokeID.endTime);
                    float endTime = brushStroke.endTime.Remap(oldStartTime, oldEndTime, brushStrokeID.startTime, brushStrokeID.endTime);
                
                    Draw(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, brushStrokeID.paintType, 
                        startTime, endTime, firstStroke, brushStrokeID.indexWhenDrawn);
                    firstStroke = false;
                }
            
                (List<BrushStrokePixel[]>, List<uint[]>) result = FinishDrawing(brushStrokeID.indexWhenDrawn, _getPixels);
                brushStrokeID.pixels = result.Item1;
                brushStrokeID.bounds = result.Item2;
            }
        }

        public void ReverseBrushStroke(BrushStrokeID _brushStrokeID)
        {
            // RedrawStroke(_brushStrokeID, PaintType.Erase);
            // _brushStrokeID.Reverse();
            // RedrawStrokeInterpolation(_brushStrokeID);
        }
        
        public bool IsMouseOverBrushStroke(BrushStrokeID _brushStrokeID, Vector2 _mousePos)
        {
            Vector4 collisionBox = _brushStrokeID.GetCollisionBox();
            if (CheckCollision(collisionBox, _mousePos))
            {
                foreach (var brushStroke in _brushStrokeID.brushStrokes)
                {
                    if (DistancePointToLine(brushStroke.GetStartPos(), brushStroke.GetEndPos(), _mousePos) < brushStroke.brushSize)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public CustomRenderTexture ReverseRtoB()
        {
            return null;
            // CustomRenderTexture tempRT = new CustomRenderTexture(UIManager.imageWidth, UIManager.imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            // {
            //     filterMode = FilterMode.Point,
            //     enableRandomWrite = true,
            //     name = "rtReverse",
            // };
            //
            // int kernelReverse = paintShader.FindKernel("WriteToReverse");
            //
            // paintShader.SetTexture(kernelReverse, "_ResultTexReverse", tempRT);
            // paintShader.SetTexture(kernelReverse, "_ResultTex", rt);
            // paintShader.SetBool("_WriteToG", false);
            // paintShader.Dispatch(kernelReverse, Mathf.CeilToInt(UIManager.imageWidth / 32f), Mathf.CeilToInt(UIManager.imageHeight / 32f), 1);
            //
            // for (var i = brushStrokesID.Count - 1; i >= 0; i--)
            // {
            //     var brushStrokeID = brushStrokesID[i];
            //     RedrawStroke(brushStrokeID, PaintType.Erase);
            //     brushStrokeID.Reverse();
            //     float newStartTime = brushStrokeID.endTime;
            //     float newEndTime = brushStrokeID.startTime;
            //     brushStrokeID.startTime = newStartTime;
            //     brushStrokeID.endTime = newEndTime;
            //     RedrawStrokeInterpolation(brushStrokeID);
            // }
            //
            // paintShader.SetBool("_WriteToG", true);
            // paintShader.Dispatch(kernelReverse, Mathf.CeilToInt(UIManager.imageWidth / 32f), Mathf.CeilToInt(UIManager.imageHeight / 32f), 1);
            //
            // for (var i = brushStrokesID.Count - 1; i >= 0; i--)
            // {
            //     var brushStrokeID = brushStrokesID[i];
            //     RedrawStroke(brushStrokeID, PaintType.Erase);
            //     brushStrokeID.Reverse();
            //     float newStartTime = brushStrokeID.endTime;
            //     float newEndTime = brushStrokeID.startTime;
            //     brushStrokeID.startTime = newStartTime;
            //     brushStrokeID.endTime = newEndTime;
            //     RedrawStrokeInterpolation(brushStrokeID);
            // }
            //
            // return tempRT;
        }
        private bool CheckCollision(Vector4 _box1, Vector2 _point)
        {
            return _point.x >= _box1.x && _point.x <= _box1.z && _point.y >= _box1.y && _point.y <= _box1.w;
        }
        
        private float DistancePointToLine(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
        {
            //Get heading
            Vector2 heading = (lineEnd - lineStart);
            float magnitudeMax = heading.magnitude;
            heading.Normalize();

            //Do projection from the point but clamp it
            Vector2 lhs = point - lineStart;
            float dotP = Vector2.Dot(lhs, heading);
            dotP = Mathf.Clamp(dotP, 0f, magnitudeMax);
            return Vector2.Distance(lineStart + heading * dotP, point);
        }
    }
}