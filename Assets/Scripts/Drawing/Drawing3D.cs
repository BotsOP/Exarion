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
        private const int TIME_TABLE_BUFFER_SIZE_INCREASE = 50;
        public List<CustomRenderTexture> rts = new List<CustomRenderTexture>();
        public List<CustomRenderTexture> rtIDs = new List<CustomRenderTexture>();
        public List<CustomRenderTexture> rtWholeTemps = new List<CustomRenderTexture>();
        public List<CustomRenderTexture> rtWholeIDTemps = new List<CustomRenderTexture>();
        public List<BrushStrokeID> brushStrokesID = new List<BrushStrokeID>();
        public Transform sphere1;
        public Renderer rend;

        private int subMeshCount => rend.materials.Length;
        
        private readonly CustomRenderTexture rtIndividualTemp;
        private ComputeShader textureHelperShader;
        private Material paintMaterial;
        private Material simplePaintMaterial;
        private int copyKernelID;
        private int finalCopyKernelID;
        private int eraseKernelID;
        private int timeRemapKernel;
        private int updateIDTexKernel;
        private int drawBrushStrokeKernel;
        private Vector3 threadGroupSizeOut;
        private Vector3 threadGroupSize;
        private int imageWidth;
        private int imageHeight;
        private CommandBuffer commandBuffer;
        private ComputeBuffer counterBuffer;
        private ComputeBuffer brushStrokeBoundsBuffer;
        private ComputeBuffer brushStrokeTableBuffer;
        private ComputeBuffer brushStrokeToPixel;
        private int timeTableBufferSize;
        private uint[] brushStrokeBoundsReset;
        
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
            eraseKernelID = textureHelperShader.FindKernel("Erase");
            timeRemapKernel = textureHelperShader.FindKernel("TimeRemap");
            updateIDTexKernel = textureHelperShader.FindKernel("UpdateIDTex");
            drawBrushStrokeKernel = textureHelperShader.FindKernel("DrawBrushStroke");
            
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

            brushStrokeBoundsReset = new[] { uint.MaxValue, uint.MaxValue, (uint)0, (uint)0 };
            
            counterBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Structured);
            counterBuffer.SetData(new int[1]);
            
            brushStrokeBoundsBuffer = new ComputeBuffer(4, sizeof(int), ComputeBufferType.Structured);
            brushStrokeBoundsBuffer.SetData(brushStrokeBoundsReset);
            
            timeTableBufferSize = TIME_TABLE_BUFFER_SIZE_INCREASE;
            brushStrokeTableBuffer = new ComputeBuffer(timeTableBufferSize, sizeof(float) * 4, ComputeBufferType.Structured);

            brushStrokeToPixel = new ComputeBuffer(imageWidth * imageHeight, sizeof(int) + sizeof(float), ComputeBufferType.Append);
            
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
            brushStrokeToPixel?.Release();
            brushStrokeToPixel = null;
            
            counterBuffer?.Release();
            counterBuffer = null;
            
            brushStrokeTableBuffer?.Release();
            brushStrokeTableBuffer = null;
            
            brushStrokeBoundsBuffer?.Release();
            brushStrokeBoundsBuffer = null;
        }

        #region addRT

        public CustomRenderTexture addRT()
        {
            CustomRenderTexture rt = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false,
                name = "rt" + rts.Count,
            };
            
            rt.Clear(false, true, Color.black);
            rts.Add(rt);
            return rt;
        }
        
        public CustomRenderTexture addRTID()
        {
            CustomRenderTexture rtID = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false,
                name = "rtID" + rtIDs.Count,
            };
            
            Color idColor = new Color(-1, -1, -1);
            rtID.Clear(false, true, idColor);
            rtIDs.Add(rtID);

            return rtID;
        }
        
        public CustomRenderTexture addRTWholeTemp()
        {
            CustomRenderTexture rtWholeTemp = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false,
                name = "rtWholeTemp" + rtWholeTemps.Count,
            };
            
            rtWholeTemp.Clear(false, true, Color.black);
            rtWholeTemps.Add(rtWholeTemp);

            return rtWholeTemp;
        }
        
        public CustomRenderTexture addRTWholeIDTemp()
        {
            CustomRenderTexture rtWholeIDTemp = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false,
                name = "rtWholeIDTemp" + rtWholeIDTemps.Count,
            };
            
            rtWholeIDTemp.Clear(false, true, Color.black);
            rtWholeIDTemps.Add(rtWholeIDTemp);

            return rtWholeIDTemp;
        }

        #endregion

        public void Draw(Vector3 _startPos, Vector3 _endPos, float _strokeBrushSize, PaintType _paintType, float _startTime = 0, float _endTime = 0, bool _firstStroke = false, float _strokeID = 0)
        {
            switch (_paintType)
            {
                case PaintType.PaintUnderEverything:
                    paintMaterial.SetInt(FirstStroke, _firstStroke ? 1 : 0);
                    paintMaterial.SetVector(CursorPos, _endPos);
                    paintMaterial.SetVector(LastCursorPos, _startPos);
                    paintMaterial.SetFloat(BrushSize, _strokeBrushSize);
                    paintMaterial.SetFloat(TimeColor, _endTime);
                    paintMaterial.SetFloat(PreviousTimeColor, _startTime);
                    paintMaterial.SetFloat(StrokeID, _strokeID);
                    for (int i = 0; i < subMeshCount; i++)
                    {
                        paintMaterial.SetTexture(IDTex, rtIDs[i]);
                    
                        commandBuffer.SetRenderTarget(rtIndividualTemp);
                        commandBuffer.DrawRenderer(rend, paintMaterial, i);
                        
                        commandBuffer.SetComputeTextureParam(textureHelperShader, copyKernelID, "_OrgTexColor", rtIndividualTemp);
                        commandBuffer.SetComputeTextureParam(textureHelperShader, copyKernelID, "_FinalTexColor", rtWholeTemps[i]);
                        commandBuffer.SetComputeTextureParam(textureHelperShader, copyKernelID, "_TempTexID", rtWholeIDTemps[i]);
                        commandBuffer.SetComputeFloatParam(textureHelperShader, "_StrokeID", _strokeID);
                        commandBuffer.DispatchCompute(textureHelperShader, copyKernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
                        ExecuteBuffer();
                    }
                    break;
            }
            
        }

        public (List<BrushStrokePixel[]>, List<uint[]>) FinishDrawing()
        {
            List<BrushStrokePixel[]> allPos = new List<BrushStrokePixel[]>();
            List<uint[]> allBounds = new List<uint[]>();
            for (int i = 0; i < subMeshCount; i++)
            {
                textureHelperShader.SetBuffer(finalCopyKernelID, "_TexToBuffer", brushStrokeToPixel);
                textureHelperShader.SetBuffer(finalCopyKernelID, "_Counter", counterBuffer);
                
                textureHelperShader.SetBuffer(finalCopyKernelID, "_BrushStrokeBounds", brushStrokeBoundsBuffer);
                textureHelperShader.SetTexture(finalCopyKernelID, "_OrgTexColor", rtWholeTemps[i]);
                textureHelperShader.SetTexture(finalCopyKernelID, "_FinalTexColor", rts[i]);
                textureHelperShader.SetTexture(finalCopyKernelID, "_FinalTexID", rtIDs[i]);
                
                textureHelperShader.Dispatch(finalCopyKernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
                
                int amountPixels = counterBuffer.GetCounter();
                Debug.Log($"{amountPixels} mb: {amountPixels * 2 / 1000000f}");
                BrushStrokePixel[] pos = new BrushStrokePixel[amountPixels];
                brushStrokeToPixel.GetData(pos);
                brushStrokeToPixel.SetCounterValue(0);
                allPos.Add(pos);

                uint[] bounds = new uint[4];
                brushStrokeBoundsBuffer.GetData(bounds);
                allBounds.Add(bounds);
                Debug.Log($"width: {bounds[2] - bounds[0]}  height: {bounds[3] - bounds[1]}");
                Debug.Log($"corner1: {bounds[0]}, {bounds[1]}  corner2: {bounds[2]}, {bounds[3]}");
                brushStrokeBoundsBuffer.SetData(brushStrokeBoundsReset);
                
                rtWholeTemps[i].Clear(false, true, Color.black);
                rtWholeIDTemps[i].Clear(false, true, Color.black);
            }
            
            return (allPos, allBounds);
        }
        
        public void EraseBrushStroke(BrushStrokeID _brushStrokeID)
        {
            CheckTimeTableBufferSize();
            int[] shouldDeleteTable = new int[timeTableBufferSize * 4];

            _brushStrokeID.shouldDelete = true;
            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                shouldDeleteTable[i * 4] = brushStrokesID[i].shouldDelete ? 1 : 0;
            }
            _brushStrokeID.shouldDelete = false;
            
            brushStrokeTableBuffer.SetData(shouldDeleteTable);
            
            for (int i = 0; i < subMeshCount; i++)
            {
                uint[] bounds = _brushStrokeID.bounds[i];
                uint width = bounds[2] - bounds[0];
                uint height = bounds[3] - bounds[1];
                int threadGroupX = Mathf.CeilToInt(width / threadGroupSizeOut.x);
                int threadGroupY = Mathf.CeilToInt(height / threadGroupSizeOut.y);
                
                Debug.Log($"x: {threadGroupX}  y: {threadGroupY}");
                if (threadGroupX == 0 && threadGroupY == 0)
                {
                    continue;
                }
                
                textureHelperShader.SetBuffer(eraseKernelID, "_EraseRemap", brushStrokeTableBuffer);
                textureHelperShader.SetTexture(eraseKernelID, "_FinalTexColor", rts[i]);
                textureHelperShader.SetTexture(eraseKernelID, "_FinalTexID", rtIDs[i]);
                textureHelperShader.SetInt("_StartPosX", (int)bounds[0]);
                textureHelperShader.SetInt("_StartPosY", (int)bounds[1]);
                
                textureHelperShader.Dispatch(eraseKernelID, threadGroupX, threadGroupY, 1);
            }
        }

        public void EraseBrushStroke(List<BrushStrokeID> _brushStrokeIDs)
        {
            CheckTimeTableBufferSize();
            int[] shouldDeleteTable = new int[timeTableBufferSize * 4];

            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                brushStrokeID.shouldDelete = true;
            }
            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                shouldDeleteTable[i * 4] = brushStrokesID[i].shouldDelete ? 1 : 0;
            }
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                brushStrokeID.shouldDelete = false;
            }
            
            brushStrokeTableBuffer.SetData(shouldDeleteTable);
            
            for (int i = 0; i < subMeshCount; i++)
            {
                uint[] bounds = CombineBounds(_brushStrokeIDs, i);
                uint width = bounds[2] - bounds[0];
                uint height = bounds[3] - bounds[1];
                int threadGroupX = Mathf.CeilToInt(width / threadGroupSizeOut.x);
                int threadGroupY = Mathf.CeilToInt(height / threadGroupSizeOut.y);
                
                Debug.Log($"x: {threadGroupX}  y: {threadGroupY}");
                if (threadGroupX == 0 && threadGroupY == 0)
                {
                    continue;
                }
                
                textureHelperShader.SetBuffer(eraseKernelID, "_EraseRemap", brushStrokeTableBuffer);
                textureHelperShader.SetTexture(eraseKernelID, "_FinalTexColor", rts[i]);
                textureHelperShader.SetTexture(eraseKernelID, "_FinalTexID", rtIDs[i]);
                textureHelperShader.SetInt("_StartPosX", (int)bounds[0]);
                textureHelperShader.SetInt("_StartPosY", (int)bounds[1]);
                
                textureHelperShader.Dispatch(eraseKernelID, threadGroupX, threadGroupY, 1);
            }
        }

        public void DrawBrushStroke(BrushStrokeID _brushStrokeID)
        {
            for (int i = 0; i < subMeshCount; i++)
            {
                BrushStrokePixel[] pixels = _brushStrokeID.pixels[i];
                int threadGroupX = Mathf.CeilToInt(pixels.Length / 1024f);
                ComputeBuffer tempPixelsBuffer = new ComputeBuffer(pixels.Length, sizeof(int) + sizeof(float), ComputeBufferType.Structured);
                tempPixelsBuffer.SetData(pixels);
                
                Vector4 timeRemap = new Vector4(_brushStrokeID.startTimeWhenDrawn, _brushStrokeID.endTimeWhenDrawn, _brushStrokeID.startTime, _brushStrokeID.endTime);

                textureHelperShader.SetBuffer(drawBrushStrokeKernel, "_BufferToTex", tempPixelsBuffer);
                textureHelperShader.SetTexture(drawBrushStrokeKernel, "_FinalTexColor", rts[i]);
                textureHelperShader.SetTexture(drawBrushStrokeKernel, "_FinalTexID", rtIDs[i]);
                textureHelperShader.SetFloat("_StrokeID", _brushStrokeID.indexWhenDrawn);
                textureHelperShader.SetVector("_TimeRemapBrushStroke", timeRemap);

                textureHelperShader.Dispatch(drawBrushStrokeKernel, threadGroupX, 1, 1);
                tempPixelsBuffer.Release();
            }
            // float oldStartTime = _brushStrokeID.brushStrokes[0].startTime;
            // float oldEndTime = _brushStrokeID.brushStrokes[^1].startTime;
            //
            // bool firstStroke = true;
            // foreach (var brushStroke in _brushStrokeID.brushStrokes)
            // {
            //     float startTime = brushStroke.startTime.Remap(oldStartTime, oldEndTime, _brushStrokeID.startTime, _brushStrokeID.endTime);
            //     float endTime = brushStroke.startTime.Remap(oldStartTime, oldEndTime, _brushStrokeID.startTime, _brushStrokeID.endTime);
            //     
            //     Draw(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, _brushStrokeID.paintType, 
            //          startTime, endTime, firstStroke, _brushStrokeID.indexWhenDrawn);
            //     firstStroke = false;
            // }
            // FinishDrawing();
        }
        
        public void SetupDrawBrushStroke(BrushStrokeID _brushStrokeID)
        {
            float oldStartTime = _brushStrokeID.brushStrokes[0].startTime;
            float oldEndTime = _brushStrokeID.brushStrokes[^1].startTime;
            
            bool firstStroke = true;
            foreach (var brushStroke in _brushStrokeID.brushStrokes)
            {
                float startTime = brushStroke.startTime.Remap(oldStartTime, oldEndTime, _brushStrokeID.startTime, _brushStrokeID.endTime);
                float endTime = brushStroke.startTime.Remap(oldStartTime, oldEndTime, _brushStrokeID.startTime, _brushStrokeID.endTime);
                
                Draw(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, _brushStrokeID.paintType, 
                     startTime, endTime, firstStroke, _brushStrokeID.indexWhenDrawn);
                firstStroke = false;
            }
            
            (List<BrushStrokePixel[]>, List<uint[]>) result = FinishDrawing();
            _brushStrokeID.pixels = result.Item1;
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
        public void UpdateIDTex(List<BrushStrokeID> _brushStrokeIDs)
        {
            CheckTimeTableBufferSize();
            int[] newIDs = new int[timeTableBufferSize * 4];
            
            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                newIDs[brushStrokesID[i].indexWhenDrawn * 4] = i;
                brushStrokesID[i].indexWhenDrawn = i;
            }
            
            brushStrokeTableBuffer.SetData(newIDs);

            for (int i = 0; i < subMeshCount; i++)
            {
                uint[] bounds = CombineBounds(_brushStrokeIDs, i);
                uint width = bounds[2] - bounds[0];
                uint height = bounds[3] - bounds[1];
                int threadGroupX = Mathf.CeilToInt(width / threadGroupSizeOut.x);
                int threadGroupY = Mathf.CeilToInt(height / threadGroupSizeOut.y);
                
                textureHelperShader.SetBuffer(updateIDTexKernel, "_IndexRemap", brushStrokeTableBuffer);
                textureHelperShader.SetTexture(updateIDTexKernel, "_FinalTexID", rtIDs[i]);
                textureHelperShader.SetInt("_StartPosX", (int)bounds[0]);
                textureHelperShader.SetInt("_StartPosY", (int)bounds[1]);
                
                textureHelperShader.Dispatch(updateIDTexKernel, threadGroupX, threadGroupY, 1);
            }
        }

        public void RedrawBrushStrokes(List<BrushStrokeID> _brushStrokeIDs, List<Vector2> _newTimes)
        {
            if (_brushStrokeIDs.Count != _newTimes.Count)
            {
                Debug.LogError($"Trying to redraw but the amount of brushstroke and new times does not match {_brushStrokeIDs.Count} {_newTimes.Count}");
            }

            CheckTimeTableBufferSize();
            
            Vector4[] timeTable = new Vector4[timeTableBufferSize];
            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                Vector2 time = brushStrokesID[i].GetTime();
                timeTable[i] = new Vector4(time.x, time.y, time.x, time.y);
            }
            for (int i = 0; i < _brushStrokeIDs.Count; i++)
            {
                Vector2 time = _brushStrokeIDs[i].GetTime();
                timeTable[_brushStrokeIDs[i].indexWhenDrawn] = new Vector4(time.x, time.y, _newTimes[i].x, _newTimes[i].y);
                _brushStrokeIDs[i].startTime = _newTimes[i].x;
                _brushStrokeIDs[i].endTime = _newTimes[i].y;
            }
            
            brushStrokeTableBuffer.SetData(timeTable);

            for (int i = 0; i < subMeshCount; i++)
            {
                uint[] bounds = CombineBounds(_brushStrokeIDs, i);
                uint width = bounds[2] - bounds[0];
                uint height = bounds[3] - bounds[1];
                int threadGroupX = Mathf.CeilToInt(width / threadGroupSizeOut.x);
                int threadGroupY = Mathf.CeilToInt(height / threadGroupSizeOut.y);
                
                //Debug.Log($"x: {threadGroupX}  y: {threadGroupY}");
                if (threadGroupX == 0 && threadGroupY == 0)
                {
                    continue;
                }
                
                textureHelperShader.SetBuffer(timeRemapKernel, "_TimeRemap", brushStrokeTableBuffer);
                textureHelperShader.SetTexture(timeRemapKernel, "_FinalTexColor", rts[i]);
                textureHelperShader.SetTexture(timeRemapKernel, "_FinalTexID", rtIDs[i]);
                textureHelperShader.SetInt("_StartPosX", (int)bounds[0]);
                textureHelperShader.SetInt("_StartPosY", (int)bounds[1]);
                
                textureHelperShader.Dispatch(timeRemapKernel, threadGroupX, threadGroupY, 1);
            }
        }

        private void CheckTimeTableBufferSize()
        {
            if (brushStrokesID.Count >= timeTableBufferSize)
            {
                brushStrokeTableBuffer?.Release();
                brushStrokeTableBuffer = null;
                timeTableBufferSize += TIME_TABLE_BUFFER_SIZE_INCREASE;
                
                brushStrokeTableBuffer = new ComputeBuffer(timeTableBufferSize, sizeof(float) * 4, ComputeBufferType.Structured);
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

        public void UpdateIDTex(BrushStrokeID _brushStrokeID)
        {
            CheckTimeTableBufferSize();
            int[] newIDs = new int[timeTableBufferSize * 4];
            
            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                newIDs[i * 4] = i;
                brushStrokesID[i].indexWhenDrawn = i;
            }
            
            brushStrokeTableBuffer.SetData(newIDs);

            for (int i = 0; i < subMeshCount; i++)
            {
                uint[] bounds = _brushStrokeID.bounds[i];
                uint width = bounds[2] - bounds[0];
                uint height = bounds[3] - bounds[1];
                int threadGroupX = Mathf.CeilToInt(width / threadGroupSizeOut.x);
                int threadGroupY = Mathf.CeilToInt(height / threadGroupSizeOut.y);
                
                textureHelperShader.SetBuffer(updateIDTexKernel, "_IndexRemap", brushStrokeTableBuffer);
                textureHelperShader.SetTexture(updateIDTexKernel, "_FinalTexID", rtIDs[i]);
                textureHelperShader.SetInt("_StartPosX", (int)bounds[0]);
                textureHelperShader.SetInt("_StartPosY", (int)bounds[1]);
                
                textureHelperShader.Dispatch(updateIDTexKernel, threadGroupX, threadGroupY, 1);
            }
        }
        
        public List<BrushStrokeID> GetOverlappingBrushStrokeID(BrushStrokeID _brushStrokeIDs)
        {
            List<BrushStrokeID> affected = new List<BrushStrokeID>();
            foreach (var toCheck in brushStrokesID)
            {
                if (affected.Contains(toCheck) || _brushStrokeIDs == toCheck )
                {
                    continue;
                }
                    
                if (CheckCollision(
                        toCheck.GetMinCorner(), toCheck.GetMaxCorner(), 
                        _brushStrokeIDs.GetMinCorner(), _brushStrokeIDs.GetMaxCorner()))
                {
                    affected.Add(toCheck);
                }
            }

            return affected;
        }
        public List<BrushStrokeID> GetOverlappingBrushStrokeID(List<BrushStrokeID> _brushStrokeIDs)
        {
            List<BrushStrokeID> affected = new List<BrushStrokeID>();
            foreach (var checkAgainst in _brushStrokeIDs)
            {
                foreach (var toCheck in brushStrokesID)
                {
                    if (affected.Contains(toCheck) || checkAgainst == toCheck )
                    {
                        continue;
                    }
                    
                    if (CheckCollision(
                            toCheck.GetMinCorner(), toCheck.GetMaxCorner(), 
                            checkAgainst.GetMinCorner(), checkAgainst.GetMaxCorner()))
                    {
                        affected.Add(toCheck);
                    }
                }
            }
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                affected.Remove(brushStrokeID);
            }

            return affected;
        }

        private uint[] CombineBounds(List<BrushStrokeID> _brushStrokeIDs, int _subMeshIndex)
        {
            uint[] combinedBounds = new uint[4];
            combinedBounds[0] = uint.MaxValue;
            combinedBounds[1] = uint.MaxValue;
            
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                uint tempLowX = brushStrokeID.bounds[_subMeshIndex][0];
                uint lowestX = combinedBounds[0];
                combinedBounds[0] = lowestX > tempLowX ? tempLowX : lowestX;
                
                uint tempLowY = brushStrokeID.bounds[_subMeshIndex][1];
                uint lowestY = combinedBounds[1];
                combinedBounds[1] = lowestY > tempLowY ? tempLowY : lowestY;
                
                uint tempHighestX = brushStrokeID.bounds[_subMeshIndex][2];
                uint highestX = combinedBounds[2];
                combinedBounds[2] = highestX < tempHighestX ? tempHighestX : highestX;
                
                uint tempHighestY = brushStrokeID.bounds[_subMeshIndex][3];
                uint highestY = combinedBounds[3];
                combinedBounds[3] = highestY < tempHighestY ? tempHighestY : highestY;
            }

            return combinedBounds;
        }
        
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