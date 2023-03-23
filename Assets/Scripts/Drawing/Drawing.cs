using System.Collections;
using System.Collections.Generic;
using Managers;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;


public enum PaintType
{
    PaintUnderOwnLine,
    PaintUnderEverything,
    PaintOverOwnLine,
    PaintOverEverything,
    Erase
}
public enum HighlightType
{
    Paint,
    Erase
}

namespace Drawing
{
    public class Drawing
    {
        public readonly CustomRenderTexture rt;
        public readonly CustomRenderTexture rtID;
        public readonly CustomRenderTexture rtSelect;
        public List<BrushStrokeID> brushStrokesID = new List<BrushStrokeID>();
        public List<BrushStroke> brushStrokes = new List<BrushStroke>();
        public Transform ball1;
        public Transform ball2;

        public int brushDrawID => brushStrokes.Count;

        public int lastBrushDrawID
        {
            get {
                int lastID = 0;
                if (lastDrawnStrokes.Count > 0)
                {
                    lastID = lastDrawnStrokes[^1].endID;
                }
                return lastID;
            }
        }
        public List<BrushStrokeID> lastDrawnStrokes = new List<BrushStrokeID>();

        private ComputeShader paintShader;
        private int paintUnderOwnLineKernelID;
        private int paintUnderEverythingKernelID;
        private int paintOverEverythingKernelID;
        private int paintOverOwnLineKernelID;
        private int eraseKernelID;
        private int highlightKernelID;
        private int highlightEraseKernelID;
        private Vector3 threadGroupSizeOut;
        private Vector3 threadGroupSize;

        public int GetNewID()
        {
            return Random.Range(1, 2147483647);
        }
        public Drawing(int imageWidth, int imageHeight)
        {
            paintShader = Resources.Load<ComputeShader>("PaintShader");

            paintUnderOwnLineKernelID = paintShader.FindKernel("PaintUnderOwnLine");
            paintUnderEverythingKernelID = paintShader.FindKernel("PaintUnderEverything");
            paintOverEverythingKernelID = paintShader.FindKernel("PaintOverEverything");
            paintOverOwnLineKernelID = paintShader.FindKernel("PaintOverOwnLine");
            eraseKernelID = paintShader.FindKernel("Erase");
            highlightKernelID = paintShader.FindKernel("HighlightSelection");
            highlightEraseKernelID = paintShader.FindKernel("EraseHighlight");
        
            paintShader.GetKernelThreadGroupSizes(paintUnderOwnLineKernelID, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
            threadGroupSizeOut.x = threadGroupSizeX;
            threadGroupSizeOut.y = threadGroupSizeY;

            rt = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                name = "rt",
            };
            rtID = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                name = "rtID",
            };
            rtSelect = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                name = "rtSelect",
            };

            Graphics.SetRenderTarget(rt);
            GL.Clear(false, true, Color.white);
            
            Graphics.SetRenderTarget(rtID);
            Color idColor = new Color(-1, -1, -1);
            GL.Clear(false, true, idColor);
            
            Graphics.SetRenderTarget(rtSelect);
            GL.Clear(false, true, Color.white);
            
            Graphics.SetRenderTarget(null);

            threadGroupSize.x = Mathf.CeilToInt(imageWidth / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt(imageHeight / threadGroupSizeOut.y);
        }
    
        public void Draw(Vector2 _lastPos, Vector2 _currentPos, float _strokeBrushSize, PaintType _paintType, float _lastTime = 0, float _brushTime = 0, bool _firstStroke = false, int _strokeID = 0, bool _lastStroke = false)
        {
            threadGroupSize.x = Mathf.CeilToInt((math.abs(_lastPos.x - _currentPos.x) + _strokeBrushSize * 2) / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt((math.abs(_lastPos.y - _currentPos.y) + _strokeBrushSize * 2) / threadGroupSizeOut.y);
        
            Vector2 startPos = GetStartPos(_lastPos, _currentPos, _strokeBrushSize);

            int kernelID = 0;
            switch (_paintType)
            {
                case PaintType.PaintUnderEverything:
                    kernelID = paintUnderEverythingKernelID;
                    Vector2 AtoB = _currentPos - _lastPos;
                    AtoB = AtoB.normalized;
                    AtoB.x *= 50;
                    AtoB.y *= 50;

                    Vector2 lastPos = _lastPos - AtoB;
                    Vector2 currentPos = _currentPos + AtoB;
                    paintShader.SetVector("_CursorPosPlusBrushSize", currentPos);
                    paintShader.SetVector("_LastCursorPosPlusBrushSize", lastPos);
                    break;
                case PaintType.PaintOverEverything:
                    kernelID = paintOverEverythingKernelID;
                    break;
                case PaintType.PaintOverOwnLine:
                    kernelID = paintOverOwnLineKernelID;
                    break;
                case PaintType.PaintUnderOwnLine:
                    kernelID = paintUnderOwnLineKernelID;
                    break;
                case PaintType.Erase:
                    if (_firstStroke)
                    {
                        _strokeBrushSize += 1;
                    }
                    kernelID = eraseKernelID;
                    break;
            }

            paintShader.SetBool("_FirstStroke", _firstStroke);
            paintShader.SetBool("_LastStroke", _lastStroke);
            paintShader.SetVector("_CursorPos", _currentPos);
            paintShader.SetVector("_LastCursorPos", _lastPos);
            paintShader.SetVector("_StartPos", startPos);
            paintShader.SetFloat("_BrushSize", _strokeBrushSize);
            paintShader.SetFloat("_TimeColor", _brushTime);
            paintShader.SetFloat("_PreviousTimeColor", _lastTime);
            paintShader.SetInt("_StrokeID", _strokeID);
            paintShader.SetTexture(kernelID, "_IdTex", rtID);
            paintShader.SetTexture(kernelID, "_ResultTex", rt);

            paintShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
        }
        
        public void DrawHighlight(Vector2 _lastPos, Vector2 _currentPos, float _strokeBrushSize, HighlightType _highlightType, float _borderThickness = 0)
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

            paintShader.SetVector("_CursorPos", _currentPos);
            paintShader.SetVector("_LastCursorPos", _lastPos);
            paintShader.SetVector("_StartPos", startPos);
            paintShader.SetFloat("_BrushSize", _strokeBrushSize);
            paintShader.SetTexture(kernelID, "_SelectTex", rtSelect);

            paintShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
        }

        private Vector2 GetStartPos(Vector2 a, Vector2 b, float _brushSize)
        {
            float lowestX = (a.x < b.x ? a.x : b.x) - _brushSize;
            float lowestY = (a.y < b.y ? a.y : b.y) - _brushSize;
            return new Vector2(lowestX, lowestY);
        }

        public void RedrawStroke(BrushStrokeID _brushstrokStartID)
        {
            int startID = _brushstrokStartID.startID;
            int endID = _brushstrokStartID.endID;
            int newStrokeID = GetNewID();
            bool firstLoop = true;
            PaintType paintType = _brushstrokStartID.paintType;
            
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = brushStrokes[i];
        
                Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, paintType, stroke.lastTime, stroke.brushTime, firstLoop, newStrokeID);

                firstLoop = false;
            }
        }

        private void RedrawStroke(BrushStrokeID _brushstrokStartID, PaintType _newPaintType)
        {
            int startID = _brushstrokStartID.startID;
            int endID = _brushstrokStartID.endID;
            int newStrokeID = GetNewID();
            bool firstLoop = true;
            PaintType paintType = _newPaintType;
            
            //Add first removing the id
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = brushStrokes[i];
        
                Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, paintType, stroke.lastTime, stroke.brushTime, firstLoop, newStrokeID);

                firstLoop = false;
            }
        }

        public void RedrawStrokeOptimized(BrushStrokeID _brushstrokID, Vector4 _collisionBox)
        {
            int startID = _brushstrokID.startID;
            int endID = _brushstrokID.endID;
            int newStrokeID = GetNewID();
            bool firstLoop = true;
            PaintType paintType = _brushstrokID.paintType;

            if (!CheckCollision(_brushstrokID.GetCollisionBox(), _collisionBox))
                return;

            Vector4 collisionBoxReset = _collisionBox;
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = brushStrokes[i];

                if (!CheckCollision(_collisionBox, stroke.GetCollisionBox()))
                {
                    _collisionBox = collisionBoxReset;
                    continue;
                }
                
                Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, paintType, stroke.lastTime, stroke.brushTime, firstLoop, newStrokeID);
                firstLoop = false;
                _collisionBox = collisionBoxReset;
            }
        }

        public void RedrawStroke(BrushStrokeID _brushstrokID, float _lastTime, float _currentTime)
        {
            int startID = _brushstrokID.startID;
            int endID = _brushstrokID.endID;
            int newStrokeID = GetNewID();
            _brushstrokID.lastTime = _lastTime;
            _brushstrokID.currentTime = _currentTime;
            Debug.Log($"{_lastTime} {_currentTime}");
            bool firstLoop = true;
            PaintType paintType = _brushstrokID.paintType;


            //First erase the stroke you want to redraw
            if (paintType is PaintType.PaintUnderEverything or PaintType.PaintOverOwnLine)
            {
                RedrawStroke(_brushstrokID, PaintType.Erase);
            }

            Vector4 collisionBox = _brushstrokID.GetCollisionBox();

            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                //If stroke is not the stroke you want to redraw. Redraw it optimized
                if (brushStrokesID[i] != _brushstrokID)
                {
                    RedrawStrokeOptimized(brushStrokesID[i], collisionBox);
                    continue;
                }

                float previousTime = _lastTime;
                for (int j = startID; j < endID; j++)
                {
                    BrushStroke stroke = brushStrokes[j];
        
                    float newTime = stroke.brushTime;
                    if (_lastTime >= 0 && _currentTime >= 0)
                    {
                        float idPercentage = ExtensionMethods.Remap(j + 1, startID, endID + 1, 0, 1);
                        newTime = (_currentTime - _lastTime) * idPercentage + _lastTime;
                    }

                    //Debug.Log($"{newTime} ___ {previousTime}");
                    stroke.brushTime = newTime;
                    stroke.lastTime = previousTime;
                    
                    Vector2 AtoB = stroke.GetCurrentPos() - stroke.GetLastPos();
                    AtoB = AtoB.normalized;
                    AtoB.x *= 50;
                    AtoB.y *= 50;

                    ball1.position = new Vector3((stroke.GetCurrentPos().x + AtoB.x) / 2048 - 0.5f, (stroke.GetCurrentPos().y + AtoB.y) / 2048 - 0.5f, -0.1f);
                    ball2.position = new Vector3((stroke.GetLastPos().x - AtoB.x) / 2048 - 0.5f, (stroke.GetLastPos().y - AtoB.y) / 2048 - 0.5f, -0.1f);

                    brushStrokes[j] = stroke;
            
                    Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, paintType, 
                         stroke.lastTime, stroke.brushTime, firstLoop, newStrokeID, j == endID - 1);

                    if (j == endID - 1)
                    {
                        Debug.Log($"{newTime} ___ {previousTime}");
                        Debug.Log($"last stroke");
                    }

                    firstLoop = false;
                    previousTime = newTime;
                }
            }
        }

        public void RedrawAll()
        {
            
            Graphics.SetRenderTarget(rtID);
            Color idColor = new Color(-1, -1, -1);
            GL.Clear(false, true, idColor);
            Graphics.SetRenderTarget(null);

            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                RedrawStroke(brushStrokesID[i]);
            }
        }
        
        public void RedrawAllOptimized(BrushStrokeID _brushStrokeID)
        {
            
            Graphics.SetRenderTarget(rtID);
            Color idColor = new Color(-1, -1, -1);
            GL.Clear(false, true, idColor);
            Graphics.SetRenderTarget(null);

            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                if (brushStrokesID[i] == _brushStrokeID)
                {
                    RedrawStroke(brushStrokesID[i]);
                }
                RedrawStrokeOptimized(brushStrokesID[i], _brushStrokeID.GetCollisionBox());
            }
        }

        private bool CheckCollision(Vector4 _box1, Vector4 _box2)
        {
            return _box1.x <= _box2.z && _box1.z >= _box2.x && _box1.y <= _box2.w && _box1.w >= _box2.y;
        }
    }

    public struct BrushStroke
    {
        public float lastPosX;
        public float lastPosY;
        public float currentPosX;
        public float currentPosY;
        public float strokeBrushSize;
        public float brushTime;
        public float lastTime;

        public BrushStroke(Vector2 _lastPos, Vector2 _currentPos, float _strokeBrushSize, float _brushTime, float _lastTime)
        {
            lastPosX = _lastPos.x;
            lastPosY = _lastPos.y;
            currentPosX = _currentPos.x;
            currentPosY = _currentPos.y;
            strokeBrushSize = _strokeBrushSize;
            brushTime = _brushTime;
            lastTime = _lastTime;
        }

        public Vector2 GetLastPos()
        {
            return new Vector2(lastPosX, lastPosY);
        }
        public Vector2 GetCurrentPos()
        {
            return new Vector2(currentPosX, currentPosY);
        }
        public Vector4 GetCollisionBox()
        {
            return new Vector4(lastPosX, lastPosY, currentPosX, currentPosY);
        }
    }
    
}