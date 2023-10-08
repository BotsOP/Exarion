using System.Collections.Generic;
using Managers;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Drawing
{
    public class DrawHighlight3D
    {
        public Renderer rend;
        public List<CustomRenderTexture> rtHighlights = new List<CustomRenderTexture>();
        private readonly CustomRenderTexture rtHighlightTemp;
        private ComputeShader textureHelperShader;
        private Vector3 threadGroupSizeOut;
        private Vector3 threadGroupSize;
        private Material simplePaintMaterial;
        private CommandBuffer commandBuffer;
        private int imageWidth;
        private int imageHeight;
        private static readonly int CursorPos = Shader.PropertyToID("_CursorPos");
        private static readonly int LastCursorPos = Shader.PropertyToID("_LastCursorPos");
        private static readonly int BrushSize = Shader.PropertyToID("_BrushSize");
        
        public DrawHighlight3D(int _imageWidth, int _imageHeight)
        {
            textureHelperShader = Resources.Load<ComputeShader>("TextureHelper");
            simplePaintMaterial = new Material(Resources.Load<Shader>("SimplePainter"));
            
            textureHelperShader.GetKernelThreadGroupSizes(0, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
            threadGroupSizeOut.x = threadGroupSizeX;
            threadGroupSizeOut.y = threadGroupSizeY;

            imageWidth = _imageWidth;
            imageHeight = _imageHeight;
            
            threadGroupSize.x = Mathf.CeilToInt(_imageWidth / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt(_imageHeight / threadGroupSizeOut.y);
            
            rtHighlightTemp = new CustomRenderTexture(_imageWidth, _imageHeight, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                name = "rtSelectTemp",
            };
            
            commandBuffer = new CommandBuffer();
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
        
        private void Highlight(Vector3 _lastPos, Vector3 _currentPos, float _strokeBrushSize, HighlightType _highlightType, float _borderThickness = 0)
        {
            simplePaintMaterial.SetVector(CursorPos, _currentPos);
            simplePaintMaterial.SetVector(LastCursorPos, _lastPos);
            simplePaintMaterial.SetFloat(BrushSize, _strokeBrushSize);

            for (int i = 0; i < rtHighlights.Count; i++)
            {
                commandBuffer.SetRenderTarget(rtHighlightTemp);
                commandBuffer.DrawRenderer(rend, simplePaintMaterial, i);
            
                commandBuffer.SetComputeTextureParam(textureHelperShader, 2, "_OrgTex4", rtHighlightTemp);
                commandBuffer.SetComputeTextureParam(textureHelperShader, 2, "_FinalTex4", rtHighlights[i]);
                commandBuffer.DispatchCompute(textureHelperShader, 2, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
                
                Graphics.ExecuteCommandBuffer(commandBuffer);
                commandBuffer.Clear();
            }
        }

        public void HighlightStroke(BrushStrokeID _brushStrokeID)
        {
            ClearHighlight();
            foreach (var brushStroke in _brushStrokeID.brushStrokes)
            {
                float highlightBrushThickness = Mathf.Clamp(brushStroke.brushSize / 2, 5, 1024);

                Highlight(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, HighlightType.Paint, highlightBrushThickness);
            }
            
            foreach (var brushStroke in _brushStrokeID.brushStrokes)
            {
                Highlight(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, HighlightType.Erase, -5);
            }
        }
        
        public void HighlightStroke(List<BrushStrokeID> _brushStrokeIDs)
        {
            ClearHighlight();
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                foreach (var brushStroke in brushStrokeID.brushStrokes)
                {
                    float highlightBrushThickness = Mathf.Clamp(brushStroke.brushSize / 2, 5, 1024);
                    highlightBrushThickness = 0;

                    Highlight(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, HighlightType.Paint, highlightBrushThickness);
                }
            }
            
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                foreach (var brushStroke in brushStrokeID.brushStrokes)
                {
                    float highlightBrushThickness = Mathf.Clamp(brushStroke.brushSize / 2, 5, 1024);
                    
                    Highlight(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, HighlightType.Erase, -highlightBrushThickness);
                }
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
