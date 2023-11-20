using System.Collections.Generic;
using Managers;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Drawing
{
    public class DrawPreview3D
    {
        private readonly int imageWidth;
        private readonly int imageHeight;
        public List<CustomRenderTexture> rtPreviews;
        public Renderer rend;
        private bool clearedTexture;
        private readonly Material simplePaintMaterial;
        private CommandBuffer commandBuffer;
        
        private static readonly int CursorPos = Shader.PropertyToID("_CursorPos");
        private static readonly int LastCursorPos = Shader.PropertyToID("_LastCursorPos");
        private static readonly int BrushSize = Shader.PropertyToID("_BrushSize");
        private static readonly int TimeColor = Shader.PropertyToID("_TimeColor");

        public DrawPreview3D(int _imageWidth, int _imageHeight)
        {
            imageWidth = _imageWidth;
            imageHeight = _imageHeight;
            simplePaintMaterial = new Material(Resources.Load<Shader>("SimplePainter"));

            rtPreviews = new List<CustomRenderTexture>();
            commandBuffer = new CommandBuffer();
        }

        public CustomRenderTexture AddRTPReview()
        {
            var rtPreview = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.RFloat,
                RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                name = "rtPreview" + rtPreviews.Count,
            };
            
            rtPreview.Clear(false, true, Color.black);
            rtPreviews.Add(rtPreview);
            
            return rtPreview;
        }
        
        public void DrawPreview(Vector3 _worldPos, float _strokeBrushSize, float _time)
        {
            simplePaintMaterial.SetVector(LastCursorPos, _worldPos);
            simplePaintMaterial.SetVector(CursorPos, _worldPos);
            simplePaintMaterial.SetFloat(BrushSize, _strokeBrushSize);
            simplePaintMaterial.SetFloat(TimeColor, _time);
            for (int i = 0; i < rtPreviews.Count; i++)
            {
                commandBuffer.SetRenderTarget(rtPreviews[i]);
                commandBuffer.DrawRenderer(rend, simplePaintMaterial, i);
                
                Graphics.ExecuteCommandBuffer(commandBuffer);
                commandBuffer.Clear();
            }

            clearedTexture = false;
        }
        
        public void ClearPreview()
        {
            if (!clearedTexture)
            {
                foreach (var rtPreview in rtPreviews)
                {
                    rtPreview.Clear(false, true, Color.black);
                    clearedTexture = true;
                }
            }
        }
    }
}