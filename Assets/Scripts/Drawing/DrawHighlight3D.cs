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
            Debug.Log($"multi select");
            ClearHighlight();
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
                textureHelperShader.SetTexture(copyHighlightKernel, "_FinalTexID", _rtIDs[i]);
                textureHelperShader.SetTexture(copyHighlightKernel, "_FinalTexColor", rtHighlights[i]);
                
                textureHelperShader.Dispatch(copyHighlightKernel, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
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
    }
}
