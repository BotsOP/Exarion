using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using UI;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Drawing
{
    public class Drawing3D
    {
        public List<CustomRenderTexture> rts = new List<CustomRenderTexture>();
        public List<CustomRenderTexture> rtIDs = new List<CustomRenderTexture>();
        public List<CustomRenderTexture> rtWholeTemps = new List<CustomRenderTexture>();
        public List<ComputeBuffer> brushStrokeToPixel = new List<ComputeBuffer>();
        public List<BrushStrokeID> brushStrokesID = new List<BrushStrokeID>();
        public Transform sphere1;
        public Renderer rend;

        private readonly CustomRenderTexture rtIndividualTemp;
        private ComputeShader textureHelperShader;
        private Material paintMaterial;
        private Material simplePaintMaterial;
        private int copyKernelID;
        private int finalCopyKernelID;
        private int CopyHighlightKernelID;
        private int EraseKernelID;
        private Vector3 threadGroupSizeOut;
        private Vector3 threadGroupSize;
        private int imageWidth;
        private int imageHeight;
        private CommandBuffer commandBuffer;
        private ComputeBuffer counterBuffer;
        
        private static readonly int FirstStroke = Shader.PropertyToID("_FirstStroke");
        private static readonly int CursorPos = Shader.PropertyToID("_CursorPos");
        private static readonly int LastCursorPos = Shader.PropertyToID("_LastCursorPos");
        private static readonly int BrushSize = Shader.PropertyToID("_BrushSize");
        private static readonly int TimeColor = Shader.PropertyToID("_TimeColor");
        private static readonly int PreviousTimeColor = Shader.PropertyToID("_PreviousTimeColor");
        private static readonly int StrokeID = Shader.PropertyToID("_StrokeID");
        private static readonly int IDTex = Shader.PropertyToID("_IDTex");

        
        public Drawing3D(int _imageWidth, int _imageHeight, Transform _sphere1)
        {
            textureHelperShader = Resources.Load<ComputeShader>("TextureHelper");
            paintMaterial = new Material(Resources.Load<Shader>("DrawPainter"));
            simplePaintMaterial = new Material(Resources.Load<Shader>("SimplePainter"));

            copyKernelID = textureHelperShader.FindKernel("Copy");
            finalCopyKernelID = textureHelperShader.FindKernel("FinalCopy");
            CopyHighlightKernelID = textureHelperShader.FindKernel("CopyHighlight");
            EraseKernelID = textureHelperShader.FindKernel("Erase");

            commandBuffer = new CommandBuffer();
            commandBuffer.name = "3DUVTimePainter";

            imageWidth = _imageWidth;
            imageHeight = _imageHeight;
            sphere1 = _sphere1;

            textureHelperShader.GetKernelThreadGroupSizes(copyKernelID, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
            threadGroupSizeOut.x = threadGroupSizeX;
            threadGroupSizeOut.y = threadGroupSizeY;
            
            threadGroupSize.x = Mathf.CeilToInt(_imageWidth / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt(_imageHeight / threadGroupSizeOut.y);

            counterBuffer = new ComputeBuffer(100, sizeof(int), ComputeBufferType.Structured);
            
            rtIndividualTemp = new CustomRenderTexture(_imageWidth, _imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false,
                name = "rtIndividualTemp",
            };
            
            
        }

        public void OnDestroy()
        {
            for (int i = 0; i < brushStrokeToPixel.Count; i++)
            {
                brushStrokeToPixel[i]?.Release();
                brushStrokeToPixel[i] = null;
            }
            counterBuffer?.Release();
            counterBuffer = null;
        }
        
        public CustomRenderTexture addRT()
        {
            CustomRenderTexture rtID = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false,
                name = "rtID",
            };
            
            Color idColor = new Color(-1, -1, -1);
            rtID.Clear(false, true, idColor);
            rtIDs.Add(rtID);
            
            CustomRenderTexture rtWholeTemp = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false,
                name = "rtWholeTemp",
            };
            
            rtWholeTemp.Clear(false, true, Color.black);
            
            rtWholeTemps.Add(rtWholeTemp);
            
            ComputeBuffer brushStrokeBuffer = new ComputeBuffer(imageWidth * imageHeight, sizeof(int), ComputeBufferType.Append);
            brushStrokeToPixel.Add(brushStrokeBuffer);
            
            CustomRenderTexture rt = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false,
                name = "rt",
            };
            
            rt.Clear(false, true, Color.black);
            rts.Add(rt);
            return rt;
        }

        public void Draw(Vector3 _lastPos, Vector3 _currentPos, float _strokeBrushSize, PaintType _paintType, float _lastTime = 0, float _brushTime = 0, bool _firstStroke = false, float _strokeID = 0)
        {
            switch (_paintType)
            {
                case PaintType.PaintUnderEverything:
                    paintMaterial.SetInt(FirstStroke, _firstStroke ? 1 : 0);
                    paintMaterial.SetVector(CursorPos, _currentPos);
                    paintMaterial.SetVector(LastCursorPos, _lastPos);
                    paintMaterial.SetFloat(BrushSize, _strokeBrushSize);
                    paintMaterial.SetFloat(TimeColor, _brushTime);
                    paintMaterial.SetFloat(PreviousTimeColor, _lastTime);
                    paintMaterial.SetFloat(StrokeID, _strokeID);
                    for (int i = 0; i < rts.Count; i++)
                    {
                        paintMaterial.SetTexture(IDTex, rtIDs[i]);
                    
                        commandBuffer.SetRenderTarget(rtIndividualTemp);
                        commandBuffer.DrawRenderer(rend, paintMaterial, i);
                        
                        commandBuffer.SetComputeTextureParam(textureHelperShader, 0, "_OrgTexColor", rtIndividualTemp);
                        commandBuffer.SetComputeTextureParam(textureHelperShader, 0, "_FinalTexColor", rtWholeTemps[i]);
                        commandBuffer.SetComputeFloatParam(textureHelperShader, "_StrokeID", _strokeID);
                        commandBuffer.DispatchCompute(textureHelperShader, 0, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
                        ExecuteBuffer();

                        // int amountPixels = counterBuffer.GetCounter();
                        // UInt16[] pos = new ushort[amountPixels * 2];
                        // brushStrokeBuffer.GetData(pos);
                    }
                    break;
                case PaintType.Erase:
                    simplePaintMaterial.SetInt(FirstStroke, _firstStroke ? 1 : 0);
                    simplePaintMaterial.SetVector(CursorPos, _currentPos);
                    simplePaintMaterial.SetVector(LastCursorPos, _lastPos);
                    simplePaintMaterial.SetFloat(BrushSize, _strokeBrushSize);

                    for (int i = 0; i < rts.Count; i++)
                    {
                        commandBuffer.SetRenderTarget(rtIndividualTemp);
                        commandBuffer.DrawRenderer(rend, simplePaintMaterial, i);
                    
                        commandBuffer.SetComputeTextureParam(textureHelperShader, 1, "_OrgTexColor", rtIndividualTemp);
                        commandBuffer.SetComputeTextureParam(textureHelperShader, 1, "_FinalTexColor", rts[i]);
                        commandBuffer.SetComputeTextureParam(textureHelperShader, 1, "_FinalTexID", rtIDs[i]);
                        commandBuffer.DispatchCompute(textureHelperShader, 1, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
                        ExecuteBuffer();
                    }
                    break;
            }
            
        }

        public void FinishDrawing()
        {
            List<UInt16[]> allPos = new List<ushort[]>();
            for (int i = 0; i < rts.Count; i++)
            {
                textureHelperShader.SetInt("_CurrentSubMesh", i);
                textureHelperShader.SetBuffer(finalCopyKernelID, "_TexToBuffer", brushStrokeToPixel[i]);
                textureHelperShader.SetBuffer(finalCopyKernelID, "_Counter", counterBuffer);
                textureHelperShader.SetTexture(finalCopyKernelID, "_OrgTexColor", rtWholeTemps[i]);
                textureHelperShader.SetTexture(finalCopyKernelID, "_FinalTexColor", rts[i]);
                textureHelperShader.SetTexture(finalCopyKernelID, "_FinalTexID", rtIDs[i]);
                
                int amountPixels = counterBuffer.GetCounter(100, i);
                UInt16[] pos = new ushort[amountPixels * 2];
                brushStrokeToPixel[i].GetData(pos);
                allPos.Add(pos);
            }
            
            counterBuffer.SetData(new int[100]);
        }

        // private void RedrawStroke(BrushStrokeID _brushstrokeID)
        // {
        //     float newStrokeID = GetNewID();
        //     bool firstLoop = true;
        //     PaintType paintType = _brushstrokeID.paintType;
        //
        //     foreach (var brushStroke in _brushstrokeID.brushStrokes)
        //     {
        //         Draw(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, paintType, 
        //              brushStroke.startTime, brushStroke.endTime, firstLoop, newStrokeID);
        //
        //         firstLoop = false;
        //     }
        // }

        public void RedrawStroke(BrushStrokeID _brushstrokeID, int _strokeID, PaintType _newPaintType)
        {
            int newStrokeID = _strokeID;
            bool firstLoop = true;
            PaintType paintType = _newPaintType;
            
            foreach (var brushStroke in _brushstrokeID.brushStrokes)
            {
                Draw(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, paintType, 
                     brushStroke.startTime, brushStroke.endTime, firstLoop, newStrokeID);

                firstLoop = false;
            }
        }

        private void RedrawStrokeOptimized(BrushStrokeID _brushstrokeID, int _strokeID, Vector3 _minCorner, Vector3 _maxCorner)
        {
            int newStrokeID = _strokeID;
            bool firstLoop = true;
            PaintType paintType = _brushstrokeID.paintType;

            if (!CheckCollision(_brushstrokeID.GetMinCorner(), _brushstrokeID.GetMaxCorner(), _minCorner, _maxCorner))
                return;
            
            RedrawStroke(_brushstrokeID, _strokeID, PaintType.Erase);
            
            foreach (var brushStroke in _brushstrokeID.brushStrokes)
            {
                Draw(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, paintType, 
                     brushStroke.startTime, brushStroke.endTime, firstLoop, newStrokeID);
                
                firstLoop = false;
            }
            
        }

        public void ReverseBrushStroke(BrushStrokeID _brushStrokeID, int _strokeID)
        {
            RedrawStroke(_brushStrokeID, _strokeID, PaintType.Erase);
            _brushStrokeID.Reverse();
            RedrawStrokeInterpolation(_brushStrokeID, _strokeID);
        }
        
        public void RedrawStrokeInterpolation(BrushStrokeID _brushstrokeID, int _strokeID)
        {
            if (_brushstrokeID.brushStrokes.Count == 0)
            {
                Debug.LogError("Brush stroke holds no draw data");
                return;
            }
            int newStrokeID = _strokeID;
            PaintType paintType = _brushstrokeID.paintType;

            //First erase the stroke you want to redraw
            if (paintType is PaintType.PaintUnderEverything or PaintType.PaintOverOwnLine)
            {
                RedrawStroke(_brushstrokeID, _strokeID, PaintType.Erase);
            }

            if (_brushstrokeID.brushStrokes.Count == 1)
            {
                BrushStroke strokeStart = _brushstrokeID.brushStrokes[0];
                float lastTime = _brushstrokeID.startTime;
                strokeStart.startTime = lastTime;
                _brushstrokeID.brushStrokes[0] = strokeStart;
                
                Draw(strokeStart.GetStartPos(), strokeStart.GetEndPos(), strokeStart.brushSize, paintType, 
                     strokeStart.startTime, strokeStart.endTime, true, newStrokeID);
                return;
            }

            int amountStrokes = _brushstrokeID.brushStrokes.Count;
            float extraTime = (_brushstrokeID.endTime - _brushstrokeID.startTime) / amountStrokes;
            float timePadding;

            //First brushStroke
            {
                float endTime = _brushstrokeID.startTime + extraTime * (1 + extraTime + extraTime) * 1;
                float startTime = _brushstrokeID.startTime;
                
                BrushStroke strokeStartReference = _brushstrokeID.brushStrokes[1];
                Vector3 lineDir = (strokeStartReference.GetEndPos() - strokeStartReference.GetStartPos()).normalized * strokeStartReference.brushSize;
                Vector3 currentPos = strokeStartReference.GetEndPos() + lineDir;
                float distLine = Vector3.Distance(strokeStartReference.GetStartPos(), currentPos);
                timePadding = strokeStartReference.brushSize.Remap(0, distLine, 0, endTime - startTime);

                BrushStroke strokeStart = _brushstrokeID.brushStrokes[0];
                strokeStart.endTime = endTime;
                strokeStart.startTime = startTime;
                _brushstrokeID.brushStrokes[0] = strokeStart;
                
                Draw(strokeStart.GetStartPos(), strokeStart.GetEndPos(), strokeStart.brushSize, paintType, 
                     strokeStart.startTime, strokeStart.endTime, true, newStrokeID);
            }
            
            //Rest of the brushStorkes
            for (int i = 1; i < _brushstrokeID.brushStrokes.Count; i++)
            {
                var brushStroke = _brushstrokeID.brushStrokes[i];
                float endTime = _brushstrokeID.startTime + extraTime * (1 + extraTime + extraTime) * (i + 1);
                float startTime = _brushstrokeID.startTime + extraTime * (1 + extraTime + extraTime) * (i);

                Vector3 lineDir = (brushStroke.GetEndPos() - brushStroke.GetStartPos()).normalized * brushStroke.brushSize;
                Vector3 currentPos = brushStroke.GetEndPos() + lineDir;
                float distLine = Vector3.Distance(brushStroke.GetStartPos(), currentPos);
                float brushSizeTime = brushStroke.brushSize.Remap(0, distLine, 0, endTime - startTime);

                startTime -= brushSizeTime;

                brushStroke.endTime = endTime + timePadding;
                brushStroke.startTime = startTime + timePadding;
                _brushstrokeID.brushStrokes[i] = brushStroke;

                Draw(
                    brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, paintType,
                    brushStroke.startTime, brushStroke.endTime, false, newStrokeID);
            }
        }

        public bool IsMouseOverBrushStroke(BrushStrokeID _brushStrokeID, Vector3 _worldPos)
        {
            Vector3 minCorner = _brushStrokeID.GetMinCorner();
            Vector3 maxCorner = _brushStrokeID.GetMaxCorner();
            if (CheckCollision(minCorner, maxCorner, _worldPos))
            {
                foreach (var brushStroke in _brushStrokeID.brushStrokes)
                {
                    float distLine = DistancePointToLine(brushStroke.GetStartPos(), brushStroke.GetEndPos(), _worldPos);
                    if (distLine < brushStroke.brushSize)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        
        public void RedrawAll()
        {
            foreach (var rtID in rtIDs)
            {
                rtID.Clear(false, true, new Color(-1, -1, -1));
            }

            for (int i = brushStrokesID.Count - 1; i >= 0; i--)
            {
                BrushStrokeID brushStrokeID = brushStrokesID[i];
                RedrawStrokeInterpolation(brushStrokeID, i);
            }
        }

        //Redraws everything and if a stroke needs to be interpolated it does so automatically
        public void RedrawAllSafe(BrushStrokeID _brushStrokeID)
        {
            for (var i = 0; i < brushStrokesID.Count; i++)
            {
                var brushStrokeID = brushStrokesID[i];
                float brushStart = brushStrokeID.brushStrokes[0].startTime;
                float brushEnd = brushStrokeID.brushStrokes[^1].startTime;
                float brushIDStart = brushStrokeID.startTime;
                float brushIDEnd = brushStrokeID.endTime;
                if (Math.Abs(brushStart - brushIDStart) > 0.000001f || Math.Abs(brushEnd - brushIDEnd) > 0.000001f)
                {
                    RedrawStrokeInterpolation(brushStrokeID, i);
                    continue;
                }

                RedrawStrokeOptimized(brushStrokeID, i, _brushStrokeID.GetMinCorner(), _brushStrokeID.GetMaxCorner());
            }
        }
        public void RedrawAllSafe(List<BrushStrokeID> _brushStrokeIDs)
        {
            Vector3 minCorner = CombineMinCorner(_brushStrokeIDs.Select(_id => _id.GetMinCorner()).ToArray());
            Vector3 maxCorner = CombineMinCorner(_brushStrokeIDs.Select(_id => _id.GetMaxCorner()).ToArray());

            for (var i = brushStrokesID.Count - 1; i >= 0; i--)
            {
                BrushStrokeID brushStrokeID = brushStrokesID[i];
                if (brushStrokeID.brushStrokes.Count == 0)
                {
                    Debug.LogError("Brush stroke holds no draw data");
                    return;
                }
                float brushStart = brushStrokeID.brushStrokes[0].startTime;
                float brushEnd = brushStrokeID.brushStrokes[^1].endTime;
                float brushIDStart = brushStrokeID.startTime;
                float brushIDEnd = brushStrokeID.endTime;
                if (Math.Abs(brushStart - brushIDStart) > 0.001f || Math.Abs(brushEnd - brushIDEnd) > 0.001f)
                {
                    RedrawStrokeInterpolation(brushStrokeID, i);
                    continue;
                }

                RedrawStrokeOptimized(brushStrokeID, i, minCorner, maxCorner);
            }
        }
        
        public void RedrawAllDirect(List<BrushStrokeID> _brushStrokeIDs)
        {
            Vector3 minCorner = CombineMinCorner(_brushStrokeIDs.Select(_id => _id.GetMinCorner()).ToArray());
            Vector3 maxCorner = CombineMinCorner(_brushStrokeIDs.Select(_id => _id.GetMaxCorner()).ToArray());

            for (var i = brushStrokesID.Count - 1; i >= 0; i--)
            {
                BrushStrokeID brushStrokeID = brushStrokesID[i];
                RedrawStrokeOptimized(brushStrokeID, i, minCorner, maxCorner);
            }
        }

        public void ExecuteBuffer()
        {
            Graphics.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        // public CustomRenderTexture ReverseRtoB()
        // {
        //     CustomRenderTexture tempRT = new CustomRenderTexture(UIManager.imageWidth, UIManager.imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
        //     {
        //         filterMode = FilterMode.Point,
        //         enableRandomWrite = true,
        //         name = "rtReverse",
        //     };
        //
        //     int kernelReverse = textureHelperShader.FindKernel("WriteToReverse");
        //     
        //     textureHelperShader.SetTexture(kernelReverse, "_ResultTexReverse", tempRT);
        //     textureHelperShader.SetTexture(kernelReverse, "_ResultTex", rt);
        //     textureHelperShader.SetBool("_WriteToG", false);
        //     textureHelperShader.Dispatch(kernelReverse, Mathf.CeilToInt(UIManager.imageWidth / 32f), Mathf.CeilToInt(UIManager.imageHeight / 32f), 1);
        //
        //     for (var i = brushStrokesID.Count - 1; i >= 0; i--)
        //     {
        //         var brushStrokeID = brushStrokesID[i];
        //         RedrawStroke(brushStrokeID, PaintType.Erase);
        //         brushStrokeID.Reverse();
        //         float newStartTime = brushStrokeID.endTime;
        //         float newEndTime = brushStrokeID.startTime;
        //         brushStrokeID.startTime = newStartTime;
        //         brushStrokeID.endTime = newEndTime;
        //         RedrawStrokeInterpolation(brushStrokeID);
        //     }
        //
        //     textureHelperShader.SetBool("_WriteToG", true);
        //     textureHelperShader.Dispatch(kernelReverse, Mathf.CeilToInt(UIManager.imageWidth / 32f), Mathf.CeilToInt(UIManager.imageHeight / 32f), 1);
        //     
        //     for (var i = brushStrokesID.Count - 1; i >= 0; i--)
        //     {
        //         var brushStrokeID = brushStrokesID[i];
        //         RedrawStroke(brushStrokeID, PaintType.Erase);
        //         brushStrokeID.Reverse();
        //         float newStartTime = brushStrokeID.endTime;
        //         float newEndTime = brushStrokeID.startTime;
        //         brushStrokeID.startTime = newStartTime;
        //         brushStrokeID.endTime = newEndTime;
        //         RedrawStrokeInterpolation(brushStrokeID);
        //     }
        //     
        //     ExecuteBuffer();
        //     
        //     return tempRT;
        // }

        private Vector3 CombineMinCorner(Vector3[] _collisionBoxes)
        {
            Vector3 collisionBox = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            foreach (Vector3 tempCollisionBox in _collisionBoxes)
            {
                if (collisionBox.x > tempCollisionBox.x) { collisionBox.x = tempCollisionBox.x; }
                if (collisionBox.y > tempCollisionBox.y) { collisionBox.y = tempCollisionBox.y; }
                if (collisionBox.z > tempCollisionBox.z) { collisionBox.z = tempCollisionBox.z; }
            }

            return collisionBox;
        }
        
        private Vector3 CombineMaxCorner(Vector3[] _collisionBoxes)
        {
            Vector3 collisionBox = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

            foreach (Vector3 tempCollisionBox in _collisionBoxes)
            {
                if (collisionBox.x < tempCollisionBox.x) { collisionBox.x = tempCollisionBox.x; }
                if (collisionBox.y < tempCollisionBox.y) { collisionBox.y = tempCollisionBox.y; }
                if (collisionBox.z < tempCollisionBox.z) { collisionBox.z = tempCollisionBox.z; }
            }

            return collisionBox;
        }

        private bool CheckCollision(Vector3 _minCorner1, Vector3 _maxCorner1, Vector3 _minCorner2, Vector3 _maxCorner2)
        {
            return (_minCorner1.x <= _maxCorner2.x && _maxCorner1.x >= _minCorner2.x) &&
                   (_minCorner1.y <= _maxCorner2.y && _maxCorner1.y >= _minCorner2.y) &&
                   (_minCorner1.z <= _maxCorner2.z && _maxCorner1.z >= _minCorner2.z);
        }
        private bool CheckCollision(Vector3 _minCorner, Vector3 _maxCorner, Vector3 _point)
        {
            return (_point.x > _minCorner.x && _point.x < _maxCorner.x) &&
                   (_point.y > _minCorner.y && _point.y < _maxCorner.y) &&
                   (_point.z > _minCorner.z && _point.z < _maxCorner.z);
        }

        private float DistancePointToLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
        {
            //Get heading
            Vector3 heading = (lineEnd - lineStart);
            float magnitudeMax = heading.magnitude;
            heading.Normalize();

            //Do projection from the point but clamp it
            Vector3 lhs = point - lineStart;
            float dotP = Vector3.Dot(lhs, heading);
            dotP = Mathf.Clamp(dotP, 0f, magnitudeMax);
            return Vector3.Distance(lineStart + heading * dotP, point);
        }
    }
}