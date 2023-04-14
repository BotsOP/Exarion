using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
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
    public class Drawing
    {
        public readonly CustomRenderTexture rt;
        public readonly CustomRenderTexture rtID;
        public List<BrushStrokeID> brushStrokesID = new List<BrushStrokeID>();

        private ComputeShader paintShader;
        private int paintUnderOwnLineKernelID;
        private int paintUnderEverythingKernelID;
        private int paintOverEverythingKernelID;
        private int paintOverOwnLineKernelID;
        private int eraseKernelID;
        private Vector3 threadGroupSizeOut;
        private Vector3 threadGroupSize;
        private int imageWidth;
        private int imageHeight;

        public int GetNewID()
        {
            return Random.Range(1, 2147483647);
        }
        public Drawing(int _imageWidth, int _imageHeight)
        {
            paintShader = Resources.Load<ComputeShader>("DrawingShader");

            imageWidth = _imageWidth;
            imageHeight = _imageHeight;

            paintUnderOwnLineKernelID = paintShader.FindKernel("PaintUnderOwnLine");
            paintUnderEverythingKernelID = paintShader.FindKernel("PaintUnderEverything");
            paintOverEverythingKernelID = paintShader.FindKernel("PaintOverEverything");
            paintOverOwnLineKernelID = paintShader.FindKernel("PaintOverOwnLine");
            eraseKernelID = paintShader.FindKernel("Erase");
        
            paintShader.GetKernelThreadGroupSizes(paintUnderOwnLineKernelID, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
            threadGroupSizeOut.x = threadGroupSizeX;
            threadGroupSizeOut.y = threadGroupSizeY;
            
            threadGroupSize.x = Mathf.CeilToInt(_imageWidth / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt(_imageHeight / threadGroupSizeOut.y);

            rt = new CustomRenderTexture(_imageWidth, _imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                name = "rt",
            };
            rtID = new CustomRenderTexture(_imageWidth, _imageHeight, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                name = "rtID",
            };

            rt.Clear(false, true, Color.white);
            
            Color idColor = new Color(-1, -1, -1);
            rtID.Clear(false, true, idColor);
        }
    
        public void Draw(Vector2 _lastPos, Vector2 _currentPos, float _strokeBrushSize, PaintType _paintType, float _lastTime = 0, float _brushTime = 0, bool _firstStroke = false, int _strokeID = 0)
        {
            threadGroupSize.x = Mathf.CeilToInt((math.abs(_lastPos.x - _currentPos.x) + _strokeBrushSize * 2) / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt((math.abs(_lastPos.y - _currentPos.y) + _strokeBrushSize * 2) / threadGroupSizeOut.y);
        
            Vector2 startPos = GetStartPos(_lastPos, _currentPos, _strokeBrushSize);

            int kernelID = 0;
            switch (_paintType)
            {
                case PaintType.PaintUnderEverything:
                    kernelID = paintUnderEverythingKernelID;
                    Vector2 AtoB = _currentPos - _lastPos;
                    AtoB = AtoB.normalized;
                    AtoB.x *= 50;
                    AtoB.y *= 50;

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
                case PaintType.Erase:
                    kernelID = eraseKernelID;
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
            paintShader.SetTexture(kernelID, "_IdTex", rtID);
            paintShader.SetTexture(kernelID, "_ResultTex", rt);

            paintShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
        }

        private Vector2 GetStartPos(Vector2 a, Vector2 b, float _brushSize)
        {
            float lowestX = (a.x < b.x ? a.x : b.x) - _brushSize;
            float lowestY = (a.y < b.y ? a.y : b.y) - _brushSize;
            return new Vector2(lowestX, lowestY);
        }

        public void RedrawStroke(BrushStrokeID _brushstrokeID)
        {
           
            int newStrokeID = GetNewID();
            bool firstLoop = true;
            PaintType paintType = _brushstrokeID.paintType;

            foreach (var brushStroke in _brushstrokeID.brushStrokes)
            {
                Draw(brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, paintType, 
                     brushStroke.lastTime, brushStroke.currentTime, firstLoop, newStrokeID);

                firstLoop = false;
            }
        }

        private void RedrawStroke(BrushStrokeID _brushstrokeID, PaintType _newPaintType)
        {
            int newStrokeID = GetNewID();
            bool firstLoop = true;
            PaintType paintType = _newPaintType;
            
            foreach (var brushStroke in _brushstrokeID.brushStrokes)
            {
                Draw(brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, paintType, 
                     brushStroke.lastTime, brushStroke.currentTime, firstLoop, newStrokeID);

                firstLoop = false;
            }
        }

        public void RedrawStrokeOptimized(BrushStrokeID _brushstrokeID, Vector4 _collisionBox)
        {
            int newStrokeID = GetNewID();
            bool firstLoop = true;
            PaintType paintType = _brushstrokeID.paintType;

            if (!CheckCollision(_brushstrokeID.GetCollisionBox(), _collisionBox))
                return;

            foreach (var brushStroke in _brushstrokeID.brushStrokes)
            {
                Draw(brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, paintType, 
                     brushStroke.lastTime, brushStroke.currentTime, firstLoop, newStrokeID);
                
                firstLoop = false;
            }
        }

        public void RedrawStrokeInterpolation(BrushStrokeID _brushstrokeID)
        {
            int newStrokeID = GetNewID();
            PaintType paintType = _brushstrokeID.paintType;

            //First erase the stroke you want to redraw
            if (paintType is PaintType.PaintUnderEverything or PaintType.PaintOverOwnLine)
            {
                RedrawStroke(_brushstrokeID, PaintType.Erase);
            }

            if (_brushstrokeID.brushStrokes.Count == 1)
            {
                BrushStroke strokeStart = _brushstrokeID.brushStrokes[0];
                float lastTime = _brushstrokeID.lastTime;
                strokeStart.lastTime = lastTime;
                _brushstrokeID.brushStrokes[0] = strokeStart;
                
                Draw(strokeStart.GetLastPos(), strokeStart.GetCurrentPos(), strokeStart.strokeBrushSize, paintType, 
                     strokeStart.lastTime, strokeStart.currentTime, true, newStrokeID);
                return;
            }

            int amountStrokes = _brushstrokeID.brushStrokes.Count;
            float extraTime = (_brushstrokeID.currentTime - _brushstrokeID.lastTime) / amountStrokes;
            float timePadding;

            {
                float newTime = _brushstrokeID.lastTime + extraTime * (1 + extraTime + extraTime) * 1;
                float lastTime = _brushstrokeID.lastTime;
                
                BrushStroke strokeStartReference = _brushstrokeID.brushStrokes[1];
                Vector2 lineDir = (strokeStartReference.GetCurrentPos() - strokeStartReference.GetLastPos()).normalized * strokeStartReference.strokeBrushSize;
                Vector2 currentPos = strokeStartReference.GetCurrentPos() + lineDir;
                float distLine = Vector2.Distance(strokeStartReference.GetLastPos(), currentPos);
                timePadding = strokeStartReference.strokeBrushSize.Remap(0, distLine, 0, newTime - lastTime);

                BrushStroke strokeStart = _brushstrokeID.brushStrokes[0];
                strokeStart.currentTime = newTime;
                strokeStart.lastTime = lastTime;
                _brushstrokeID.brushStrokes[0] = strokeStart;
                
                Draw(strokeStart.GetLastPos(), strokeStart.GetCurrentPos(), strokeStart.strokeBrushSize, paintType, 
                     strokeStart.lastTime, strokeStart.currentTime, true, newStrokeID);
            }
            
            for (int i = 0; i < _brushstrokeID.brushStrokes.Count; i++)
            {
                var brushStroke = _brushstrokeID.brushStrokes[i];
                float newTime = _brushstrokeID.lastTime + extraTime * (1 + extraTime + extraTime) * (i + 1);
                float lastTime = _brushstrokeID.lastTime + extraTime * (1 + extraTime + extraTime) * (i);

                Vector2 lineDir = (brushStroke.GetCurrentPos() - brushStroke.GetLastPos()).normalized * brushStroke.strokeBrushSize;
                Vector2 currentPos = brushStroke.GetCurrentPos() + lineDir;
                float distLine = Vector2.Distance(brushStroke.GetLastPos(), currentPos);
                float brushSizeTime = brushStroke.strokeBrushSize.Remap(0, distLine, 0, newTime - lastTime);

                lastTime -= brushSizeTime;

                brushStroke.currentTime = newTime + timePadding;
                brushStroke.lastTime = lastTime + timePadding;
                _brushstrokeID.brushStrokes[i] = brushStroke;

                Draw(
                    brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, paintType,
                    brushStroke.lastTime, brushStroke.currentTime, false, newStrokeID);
            }
        }

        public bool IsMouseOverBrushStroke(BrushStrokeID _brushStrokeID, Vector2 _mousePos)
        {
            Vector4 collisionBox = _brushStrokeID.GetCollisionBox();
            if (CheckCollision(collisionBox, _mousePos))
            {
                foreach (var brushStroke in _brushStrokeID.brushStrokes)
                {
                    if (DistancePointToLine(brushStroke.GetStartPos(), brushStroke.GetEndPos(), _mousePos) < brushStroke.strokeBrushSize)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        public void RedrawAll()
        {
            rtID.Clear(false, true, new Color(-1, -1, -1));
            
            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                RedrawStroke(brushStrokesID[i]);
            }
        }

        //Redraws everything and if a stroke needs to be interpolated it does so automatically
        public void RedrawAllSafe(BrushStrokeID _brushStrokeID)
        {
            rtID.Clear(false, true, new Color(-1, -1, -1));

            Vector4 collisionBox = _brushStrokeID.GetCollisionBox();

            foreach (BrushStrokeID brushStrokeID in brushStrokesID)
            {
                float brushStart = brushStrokeID.brushStrokes[0].lastTime;
                float brushEnd = brushStrokeID.brushStrokes[^1].lastTime;
                float brushIDStart = brushStrokeID.lastTime;
                float brushIDEnd = brushStrokeID.currentTime;
                if (Math.Abs(brushStart - brushIDStart) > 0.01f || Math.Abs(brushEnd - brushIDEnd) > 0.01f)
                {
                    RedrawStrokeInterpolation(brushStrokeID);
                    continue;
                }

                RedrawStrokeOptimized(brushStrokeID, collisionBox);
            }
        }
        public void RedrawAllSafe(List<BrushStrokeID> _brushStrokeIDs)
        {
            rtID.Clear(false, true, new Color(-1, -1, -1));

            Vector4 collisionBox = CombineCollisionBox(
                _brushStrokeIDs.Select(_id => _id.GetCollisionBox()).ToArray());

            foreach (BrushStrokeID brushStrokeID in brushStrokesID)
            {
                float brushStart = brushStrokeID.brushStrokes[0].lastTime;
                float brushEnd = brushStrokeID.brushStrokes[^1].currentTime;
                float brushIDStart = brushStrokeID.lastTime;
                float brushIDEnd = brushStrokeID.currentTime;
                if (Math.Abs(brushStart - brushIDStart) > 0.01f || Math.Abs(brushEnd - brushIDEnd) > 0.01f)
                {
                    RedrawStrokeInterpolation(brushStrokeID);
                    continue;
                }

                RedrawStrokeOptimized(brushStrokeID, collisionBox);
            }
        }

        private Vector4 CombineCollisionBox(Vector4[] collisionBoxes)
        {
            Vector4 collisionBox = new Vector4(imageWidth, imageHeight, 0, 0);

            foreach (Vector4 tempCollisionBox in collisionBoxes)
            {
                if (collisionBox.x > tempCollisionBox.x) { collisionBox.x = tempCollisionBox.x; }
                if (collisionBox.y > tempCollisionBox.y) { collisionBox.y = tempCollisionBox.y; }
                if (collisionBox.z < tempCollisionBox.z) { collisionBox.z = tempCollisionBox.z; }
                if (collisionBox.w < tempCollisionBox.w) { collisionBox.w = tempCollisionBox.w; }
            }

            return collisionBox;
        }

        private bool CheckCollision(Vector4 _box1, Vector4 _box2)
        {
            return _box1.x <= _box2.z && _box1.z >= _box2.x && _box1.y <= _box2.w && _box1.w >= _box2.y;
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