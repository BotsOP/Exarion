using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace Drawing
{
    public class DrawingLib
    {
        protected const int TIME_TABLE_BUFFER_SIZE_INCREASE = 50;
        public List<CustomRenderTexture> rts = new List<CustomRenderTexture>();
        public List<CustomRenderTexture> rtIDs = new List<CustomRenderTexture>();
        public List<CustomRenderTexture> rtWholeTemps = new List<CustomRenderTexture>();
        public List<CustomRenderTexture> rtWholeIDTemps = new List<CustomRenderTexture>();
        public List<BrushStrokeID> brushStrokesID = new List<BrushStrokeID>();
        protected int subMeshCount => rts.Count;
        
        protected ComputeShader textureHelperShader;
        protected int copyKernelID;
        protected int finalCopyKernelID;
        protected int eraseKernelID;
        protected int timeRemapKernel;
        protected int updateIDTexKernel;
        protected int drawBrushStrokeKernel;
        protected Vector3 threadGroupSizeOut;
        protected Vector3 threadGroupSize;
        protected int imageWidth;
        protected int imageHeight;
        protected ComputeBuffer counterBuffer;
        protected ComputeBuffer brushStrokeBoundsBuffer;
        protected ComputeBuffer brushStrokeTableBuffer;
        protected ComputeBuffer brushStrokeToPixel;
        protected int timeTableBufferSize;
        protected uint[] brushStrokeBoundsReset;
        protected List<ComputeBuffer> tempPixels;

        protected DrawingLib(int _imageWidth, int _imageHeight)
        {
            textureHelperShader = Resources.Load<ComputeShader>("TextureHelper");
            copyKernelID = textureHelperShader.FindKernel("Copy");
            finalCopyKernelID = textureHelperShader.FindKernel("FinalCopy");
            eraseKernelID = textureHelperShader.FindKernel("Erase");
            timeRemapKernel = textureHelperShader.FindKernel("TimeRemap");
            updateIDTexKernel = textureHelperShader.FindKernel("UpdateIDTex");
            drawBrushStrokeKernel = textureHelperShader.FindKernel("DrawBrushStroke");
            
            imageWidth = _imageWidth;
            imageHeight = _imageHeight;
            
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
            brushStrokeToPixel.SetCounterValue(0);

            tempPixels = new List<ComputeBuffer>();
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

        private void Draw(Vector3 _startPos, Vector3 _endPos, float _strokeBrushSize, PaintType _paintType, float _startTime = 0, float _endTime = 0, bool _firstStroke = false, float _strokeID = 0)
        {
            Debug.LogError($"Implement draw method!");
        }

        public (List<BrushStrokePixel[]>, List<uint[]>) FinishDrawing(int _strokeID, bool _getPixels = true)
        {
            List<BrushStrokePixel[]> allPos = new List<BrushStrokePixel[]>();
            List<uint[]> allBounds = new List<uint[]>();
            
            textureHelperShader.SetFloat("_StrokeID", _strokeID);
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
                brushStrokeToPixel.SetCounterValue(0);
                if (_getPixels)
                {
                    //Debug.Log($"mb: {amountPixels * 2 / 1000000f}");
                    BrushStrokePixel[] pos = new BrushStrokePixel[amountPixels];
                    brushStrokeToPixel.GetData(pos);
                    allPos.Add(pos);
                }

                uint[] bounds = new uint[4];
                brushStrokeBoundsBuffer.GetData(bounds);
                allBounds.Add(bounds);
                brushStrokeBoundsBuffer.SetData(brushStrokeBoundsReset);
                
                rtWholeTemps[i].Clear(false, true, Color.black);
                rtWholeIDTemps[i].Clear(false, true, Color.black);
            }
            
            return (allPos, allBounds);
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

        public void CachePixelBuffer(List<BrushStrokeID> _brushStrokeIDs)
        {
            tempPixels.Clear();
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                for (int i = 0; i < brushStrokeID.pixels.Count; i++)
                {
                    BrushStrokePixel[] pixels = brushStrokeID.pixels[i];
                    ComputeBuffer tempPixelsBuffer = new ComputeBuffer(pixels.Length, sizeof(int) + sizeof(float), ComputeBufferType.Structured);
                    tempPixelsBuffer.SetData(pixels);
                    tempPixels.Add(tempPixelsBuffer);
                }
            }
        }

        public void ClearCachePixelBuffer()
        {
            foreach (var computeBuffer in tempPixels)
            {
                computeBuffer?.Release();
            }
            tempPixels.Clear();
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
        }
        public void DrawBrushStroke(List<BrushStrokeID> _brushStrokeIDs, bool _useCachedPixelBuffer = false)
        {
            for (var i = 0; i < _brushStrokeIDs.Count; i++)
            {
                var brushStrokeID = _brushStrokeIDs[i];
                for (int j = 0; j < subMeshCount; j++)
                {
                    if (_useCachedPixelBuffer)
                    {
                        ComputeBuffer tempPixelsBuffer = tempPixels[i + j];
                        int threadGroupX = Mathf.CeilToInt(tempPixelsBuffer.count / 1024f);

                        Vector4 timeRemap = new Vector4(brushStrokeID.startTimeWhenDrawn,
                            brushStrokeID.endTimeWhenDrawn, brushStrokeID.startTime, brushStrokeID.endTime);

                        textureHelperShader.SetBuffer(drawBrushStrokeKernel, "_BufferToTex", tempPixelsBuffer);
                        textureHelperShader.SetTexture(drawBrushStrokeKernel, "_FinalTexColor", rts[j]);
                        textureHelperShader.SetTexture(drawBrushStrokeKernel, "_FinalTexID", rtIDs[j]);
                        textureHelperShader.SetFloat("_StrokeID", brushStrokeID.indexWhenDrawn);
                        textureHelperShader.SetVector("_TimeRemapBrushStroke", timeRemap);

                        textureHelperShader.Dispatch(drawBrushStrokeKernel, threadGroupX, 1, 1);
                    }
                    else
                    {
                        BrushStrokePixel[] pixels = brushStrokeID.pixels[j];
                        int threadGroupX = Mathf.CeilToInt(pixels.Length / 1024f);
                        ComputeBuffer tempPixelsBuffer = new ComputeBuffer(pixels.Length, sizeof(int) + sizeof(float),
                            ComputeBufferType.Structured);
                        tempPixelsBuffer.SetData(pixels);

                        Vector4 timeRemap = new Vector4(brushStrokeID.startTimeWhenDrawn,
                            brushStrokeID.endTimeWhenDrawn, brushStrokeID.startTime, brushStrokeID.endTime);

                        textureHelperShader.SetBuffer(drawBrushStrokeKernel, "_BufferToTex", tempPixelsBuffer);
                        textureHelperShader.SetTexture(drawBrushStrokeKernel, "_FinalTexColor", rts[j]);
                        textureHelperShader.SetTexture(drawBrushStrokeKernel, "_FinalTexID", rtIDs[j]);
                        textureHelperShader.SetFloat("_StrokeID", brushStrokeID.indexWhenDrawn);
                        textureHelperShader.SetVector("_TimeRemapBrushStroke", timeRemap);

                        textureHelperShader.Dispatch(drawBrushStrokeKernel, threadGroupX, 1, 1);
                        tempPixelsBuffer.Release();
                    }
                }
            }
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

        public void RedrawBrushStrokes(List<BrushStrokeID> _brushStrokeIDs)
        {
            CheckTimeTableBufferSize();
            
            Vector4[] timeTable = new Vector4[timeTableBufferSize];
            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                Vector2 time = brushStrokesID[i].GetTime();
                timeTable[i] = new Vector4(time.x, time.y, time.x, time.y);
            }
            for (int i = 0; i < _brushStrokeIDs.Count; i++)
            {
                BrushStrokeID brushStrokeID = _brushStrokeIDs[i];
                
                float startTimeOld = brushStrokeID.startTimeOld;
                float endTimeOld = brushStrokeID.endTimeOld;
                float startTime = brushStrokeID.startTime;
                float endTime = brushStrokeID.endTime;
                
                timeTable[brushStrokeID.indexWhenDrawn] = new Vector4(startTimeOld, endTimeOld, startTime, endTime);
                brushStrokeID.startTimeOld = startTime;
                brushStrokeID.endTimeOld = endTime;
            }
            
            brushStrokeTableBuffer.SetData(timeTable);

            for (int i = 0; i < subMeshCount; i++)
            {
                uint[] bounds = CombineBounds(_brushStrokeIDs, i);
                uint width = bounds[2] - bounds[0];
                uint height = bounds[3] - bounds[1];
                int threadGroupX = Mathf.CeilToInt(width / threadGroupSizeOut.x);
                int threadGroupY = Mathf.CeilToInt(height / threadGroupSizeOut.y);
                
                if (threadGroupX == 0 && threadGroupY == 0)
                {
                    Debug.Log($"Skipped rendering brushstroke");
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
                if (affected.Contains(toCheck) || _brushStrokeIDs.indexWhenDrawn >= toCheck.indexWhenDrawn)
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
                    if (affected.Contains(toCheck) || checkAgainst.indexWhenDrawn >= toCheck.indexWhenDrawn)
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

        private bool CheckCollision(Vector3 _minCorner1, Vector3 _maxCorner1, Vector3 _minCorner2, Vector3 _maxCorner2)
        {
            if (_minCorner1.x > _maxCorner2.x || _minCorner2.x >_maxCorner1.x)
                return false;
            if (_minCorner1.y > _maxCorner2.y || _minCorner2.y > _maxCorner1.y)
                return false;
            if (_minCorner1.z > _maxCorner2.z || _minCorner2.z > _maxCorner1.z)
                return false;

            return true;
        }
    }
}
