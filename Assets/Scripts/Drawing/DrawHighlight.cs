using System.Collections.Generic;
using Managers;
using Unity.Mathematics;
using UnityEngine;

namespace Drawing
{
    public enum HighlightType
    {
        Paint,
        Erase
    }
    
    public class DrawHighlight
    {
        public readonly CustomRenderTexture rtHighlight;
        private ComputeShader highlightShader;
        private int highlightKernelID;
        private int highlightEraseKernelID;
        private Vector3 threadGroupSizeOut;
        private Vector3 threadGroupSize;


        public DrawHighlight(int _imageWidth, int _imageHeight)
        {
            highlightShader = Resources.Load<ComputeShader>("HighlightShader");
            
            highlightKernelID = highlightShader.FindKernel("HighlightSelection");
            highlightEraseKernelID = highlightShader.FindKernel("EraseHighlight");
            
            highlightShader.GetKernelThreadGroupSizes(highlightKernelID, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
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
            
            Graphics.SetRenderTarget(rtHighlight);
            GL.Clear(false, true, Color.white);
            Graphics.SetRenderTarget(null);
        }

        private void Highlight(Vector2 _lastPos, Vector2 _currentPos, float _strokeBrushSize, HighlightType _highlightType, float _borderThickness = 0)
        {
            _strokeBrushSize += _borderThickness;
            _strokeBrushSize = Mathf.Clamp(_strokeBrushSize, 1, 1024);
            threadGroupSize.x = Mathf.CeilToInt((math.abs(_lastPos.x - _currentPos.x) + _strokeBrushSize * 2) / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt((math.abs(_lastPos.y - _currentPos.y) + _strokeBrushSize * 2) / threadGroupSizeOut.y);
        
            Vector2 startPos = GetStartPos(_lastPos, _currentPos, _strokeBrushSize);
            
            int kernelID = 0;
            switch (_highlightType)
            {
                case HighlightType.Paint:
                    kernelID = highlightKernelID;
                    break;
                case HighlightType.Erase:
                    kernelID = highlightEraseKernelID;
                    break;
            }

            highlightShader.SetVector("_CursorPos", _currentPos);
            highlightShader.SetVector("_LastCursorPos", _lastPos);
            highlightShader.SetVector("_StartPos", startPos);
            highlightShader.SetFloat("_BrushSize", _strokeBrushSize);
            highlightShader.SetTexture(kernelID, "_SelectTex", rtHighlight);

            highlightShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
        }

        public void HighlightStroke(BrushStrokeID _brushStrokeID)
        {
            ClearHighlight();
            foreach (var brushStroke in _brushStrokeID.brushStrokes)
            {
                float highlightBrushThickness = Mathf.Clamp(brushStroke.strokeBrushSize / 2, 5, 1024);

                Highlight(brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, HighlightType.Paint, highlightBrushThickness);
            }
            
            foreach (var brushStroke in _brushStrokeID.brushStrokes)
            {
                Highlight(brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, HighlightType.Erase, -5);
            }
        }
        
        public void HighlightStroke(List<BrushStrokeID> _brushStrokeIDs)
        {
            ClearHighlight();
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                foreach (var brushStroke in brushStrokeID.brushStrokes)
                {
                    float highlightBrushThickness = Mathf.Clamp(brushStroke.strokeBrushSize / 2, 5, 1024);

                    Highlight(brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, HighlightType.Paint, highlightBrushThickness);
                }
            }
            
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                foreach (var brushStroke in brushStrokeID.brushStrokes)
                {
                    Highlight(brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, HighlightType.Erase, -5);
                }
            }
        }

        public void ClearHighlight()
        {
            rtHighlight.Clear(false, true, Color.white);
        }
        
        private Vector2 GetStartPos(Vector2 a, Vector2 b, float _brushSize)
        {
            float lowestX = (a.x < b.x ? a.x : b.x) - _brushSize;
            float lowestY = (a.y < b.y ? a.y : b.y) - _brushSize;
            return new Vector2(lowestX, lowestY);
        }
    }
}
