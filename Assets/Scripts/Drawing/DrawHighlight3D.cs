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
        public readonly CustomRenderTexture rtHighlight;
        private readonly CustomRenderTexture rtHighlightTemp;
        private ComputeShader textureHelperShader;
        private Vector3 threadGroupSizeOut;
        private Vector3 threadGroupSize;
        private Material simplePaintMaterial;
        private CommandBuffer commandBuffer;
        

        public DrawHighlight3D(int _imageWidth, int _imageHeight)
        {
            textureHelperShader = Resources.Load<ComputeShader>("TextureHelper");
            simplePaintMaterial = new Material(Resources.Load<Shader>("SimplePainter"));
            
            textureHelperShader.GetKernelThreadGroupSizes(0, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
            threadGroupSizeOut.x = threadGroupSizeX;
            threadGroupSizeOut.y = threadGroupSizeY;
            
            threadGroupSize.x = Mathf.CeilToInt(_imageWidth / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt(_imageHeight / threadGroupSizeOut.y);
            
            rtHighlight = new CustomRenderTexture(_imageWidth, _imageHeight, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                name = "rtSelect",
            };
            rtHighlightTemp = new CustomRenderTexture(_imageWidth, _imageHeight, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                name = "rtSelectTemp",
            };
            
            rtHighlight.Clear(false, true, Color.black);

            commandBuffer = new CommandBuffer();
        }
        
        private void Highlight(Vector3 _lastPos, Vector3 _currentPos, float _strokeBrushSize, HighlightType _highlightType, float _borderThickness = 0)
        {
            simplePaintMaterial.SetVector("_CursorPos", _currentPos);
            simplePaintMaterial.SetVector("_LastCursorPos", _lastPos);
            simplePaintMaterial.SetFloat("_BrushSize", _strokeBrushSize);

            commandBuffer.SetRenderTarget(rtHighlightTemp);
            commandBuffer.DrawRenderer(rend, simplePaintMaterial, 0);
            
            commandBuffer.SetComputeTextureParam(textureHelperShader, 2, "_OrgTex4", rtHighlightTemp);
            commandBuffer.SetComputeTextureParam(textureHelperShader, 2, "_FinalTex4", rtHighlight);
            commandBuffer.DispatchCompute(textureHelperShader, 2, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
            Graphics.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }
        
        // private void Highlight(Vector2 _lastPos, Vector2 _currentPos, float _strokeBrushSize, HighlightType _highlightType, float _borderThickness = 0)
        // {
        //     _strokeBrushSize += _borderThickness;
        //     _strokeBrushSize = Mathf.Clamp(_strokeBrushSize, 1, 1024);
        //     threadGroupSize.x = Mathf.CeilToInt((math.abs(_lastPos.x - _currentPos.x) + _strokeBrushSize * 2) / threadGroupSizeOut.x);
        //     threadGroupSize.y = Mathf.CeilToInt((math.abs(_lastPos.y - _currentPos.y) + _strokeBrushSize * 2) / threadGroupSizeOut.y);
        //
        //     Vector2 startPos = GetStartPos(_lastPos, _currentPos, _strokeBrushSize);
        //     
        //     int kernelID = 0;
        //     switch (_highlightType)
        //     {
        //         case HighlightType.Paint:
        //             kernelID = highlightKernelID;
        //             break;
        //         case HighlightType.Erase:
        //             kernelID = highlightEraseKernelID;
        //             break;
        //     }
        //
        //     textureHelperShader.SetVector("_CursorPos", _currentPos);
        //     textureHelperShader.SetVector("_LastCursorPos", _lastPos);
        //     textureHelperShader.SetVector("_StartPos", startPos);
        //     textureHelperShader.SetFloat("_BrushSize", _strokeBrushSize);
        //     textureHelperShader.SetTexture(kernelID, "_SelectTex", rtHighlight);
        //
        //     textureHelperShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
        // }

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
            rtHighlight.Clear(false, true, Color.black);
        }
        
        private Vector2 GetStartPos(Vector2 a, Vector2 b, float _brushSize)
        {
            float lowestX = (a.x < b.x ? a.x : b.x) - _brushSize;
            float lowestY = (a.y < b.y ? a.y : b.y) - _brushSize;
            return new Vector2(lowestX, lowestY);
        }
    }
}
