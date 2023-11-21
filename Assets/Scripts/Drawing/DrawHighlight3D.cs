using System.Collections.Generic;
using Managers;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Drawing
{
    public class DrawHighlight3D
    {
        private const int TIME_TABLE_BUFFER_SIZE_INCREASE = 50;

        public Renderer rend;
        public List<CustomRenderTexture> rtHighlights = new List<CustomRenderTexture>();
        
        private ComputeShader textureHelperShader;
        private Vector3 threadGroupSizeOut;
        private Vector3 threadGroupSize;
        private int copyHighlightKernel;
        
        private int imageWidth;
        private int imageHeight;
        
        private ComputeBuffer highlightIndexBuffer;
        private int highlightIndexBufferSize;

        public DrawHighlight3D(int _imageWidth, int _imageHeight)
        {
            textureHelperShader = Resources.Load<ComputeShader>("TextureHelper");
            copyHighlightKernel = textureHelperShader.FindKernel("CopyHighlight");
            
            textureHelperShader.GetKernelThreadGroupSizes(copyHighlightKernel, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
            threadGroupSizeOut.x = threadGroupSizeX;
            threadGroupSizeOut.y = threadGroupSizeY;

            imageWidth = _imageWidth;
            imageHeight = _imageHeight;
            
            threadGroupSize.x = Mathf.CeilToInt(_imageWidth / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt(_imageHeight / threadGroupSizeOut.y);

            highlightIndexBufferSize += TIME_TABLE_BUFFER_SIZE_INCREASE;
            highlightIndexBuffer = new ComputeBuffer(highlightIndexBufferSize, sizeof(int), ComputeBufferType.Structured);
        }

        public CustomRenderTexture AddRT()
        {
            CustomRenderTexture rtHighlight = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                name = "rtSelect",
            };
            
            rtHighlight.Clear(false, true, Color.black);
            rtHighlights.Add(rtHighlight);
            return rtHighlight;
        }

        public void HighlightStroke(List<BrushStrokeID> _brushStrokeIDs, List<CustomRenderTexture> _rtIDs, int _totalBrushStrokes)
        {
            ClearHighlight();

            if (_brushStrokeIDs.Count == 0)
            {
                return;
            }
            
            CheckTimeTableBufferSize(_totalBrushStrokes);

            int[] highlightIndex = new int[highlightIndexBufferSize];
            for (int i = 0; i < _brushStrokeIDs.Count; i++)
            {
                highlightIndex[_brushStrokeIDs[i].indexWhenDrawn] = 1;
            }
            highlightIndexBuffer.SetData(highlightIndex);

            textureHelperShader.SetBuffer(copyHighlightKernel, "_HighlightIndex", highlightIndexBuffer);
            for (int i = 0; i < rtHighlights.Count; i++)
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
                if (threadGroupX == 1 && threadGroupY == 1)
                {
                    continue;
                }
                
                textureHelperShader.SetTexture(copyHighlightKernel, "_FinalTexID", _rtIDs[i]);
                textureHelperShader.SetTexture(copyHighlightKernel, "_FinalTexColor", rtHighlights[i]);
                textureHelperShader.SetInt("_StartPosX", (int)bounds[0]);
                textureHelperShader.SetInt("_StartPosY", (int)bounds[1]);
                
                textureHelperShader.Dispatch(copyHighlightKernel, threadGroupX, threadGroupY, 1);
            }
        }
        
        private void CheckTimeTableBufferSize(int _totalSize)
        {
            if (_totalSize >= highlightIndexBufferSize)
            {
                highlightIndexBuffer?.Release();
                highlightIndexBuffer = null;

                highlightIndexBufferSize += TIME_TABLE_BUFFER_SIZE_INCREASE;
                highlightIndexBuffer = new ComputeBuffer(highlightIndexBufferSize, sizeof(float) * 4, ComputeBufferType.Structured);
            }
        }

        public void ClearHighlight()
        {
            foreach (var rtHighlight in rtHighlights)
            {
                rtHighlight.Clear(false, true, Color.black);
            }
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
    }
}
