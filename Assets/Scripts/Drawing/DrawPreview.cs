using Managers;
using Unity.Mathematics;
using UnityEngine;

namespace Drawing
{
    public class DrawPreview
    {
        public readonly CustomRenderTexture rtPreview;
        private ComputeShader previewShader;
        private int previewKernelID;
        private Vector3 threadGroupSizeOut;
        private Vector3 threadGroupSize;
        private bool clearedTexture;

        public DrawPreview(int _imageWidth, int _imageHeight)
        {
            previewShader = Resources.Load<ComputeShader>("PreviewShader");
            
            previewKernelID = previewShader.FindKernel("Preview");
            
            previewShader.GetKernelThreadGroupSizes(previewKernelID, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
            threadGroupSizeOut.x = threadGroupSizeX;
            threadGroupSizeOut.y = threadGroupSizeY;
            
            threadGroupSize.x = Mathf.CeilToInt(_imageWidth / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt(_imageHeight / threadGroupSizeOut.y);
            
            rtPreview = new CustomRenderTexture(_imageWidth, _imageHeight, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                name = "rtPreview",
            };
        }

        public void Preview(Vector2 _currentPos, float _strokeBrushSize)
        {
            rtPreview.Clear(false, true, Color.black);
            
            clearedTexture = false;
            threadGroupSize.x = Mathf.CeilToInt(_strokeBrushSize * 2 / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt(_strokeBrushSize * 2 / threadGroupSizeOut.y);
        
            Vector2 startPos = new Vector2(_currentPos.x - _strokeBrushSize, _currentPos.y - _strokeBrushSize);

            previewShader.SetVector("_CursorPos", _currentPos);
            previewShader.SetVector("_StartPos", startPos);
            previewShader.SetFloat("_BrushSize", _strokeBrushSize);
            previewShader.SetTexture(previewKernelID, "_PreviewTex", rtPreview);

            previewShader.Dispatch(previewKernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
        }

        public void ClearPreview()
        {
            if (!clearedTexture)
            {
                Debug.Log($"clear");
                rtPreview.Clear(false, true, Color.black);
                clearedTexture = true;
            }
        }
    }
}
