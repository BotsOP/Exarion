using System.Collections;
using System.Collections.Generic;
using Managers;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Drawing
{
    public class Drawing
    {
        public CustomRenderTexture rt;
        public CustomRenderTexture rtID;
        public CustomRenderTexture rtSelect;
        public List<BrushStrokeID> brushStrokesID = new List<BrushStrokeID>();
        public List<BrushStroke> brushStrokes = new List<BrushStroke>();

        private int brushDrawID => brushStrokes.Count;

        private int lastBrushDrawID
        {
            get {
                int lastID = 0;
                if (brushStrokesID.Count > 0)
                {
                    lastID = brushStrokesID[^1].endID + 1;
                }
                return lastID;
            }
        }

        private ComputeShader paintShader;
        private int paintUnderOwnLineKernelID;
        private int paintUnderEverythingKernelID;
        private int paintOverEverythingKernelID;
        private int paintOverOwnLineKernelID;
        private int eraseKernelID;
        private int highlightKernelID;
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
                enableRandomWrite = true,
                name = "rtID",
            };
            rtSelect = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear)
            {
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
    
        public void Draw(Vector2 _lastPos, Vector2 _currentPos, float _strokeBrushSize, PaintType _paintType, float _lastTime = 0, float _brushTime = 0, bool _firstStroke = false, int _strokeID = 0)
        {
            threadGroupSize.x = Mathf.CeilToInt((math.abs(_lastPos.x - _currentPos.x) + _strokeBrushSize * 2) / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt((math.abs(_lastPos.y - _currentPos.y) + _strokeBrushSize * 2) / threadGroupSizeOut.y);
        
            Vector2 startPos = GetStartPos(_lastPos, _currentPos, _strokeBrushSize);

            int kernelID = 0;
            switch (_paintType)
            {
                case PaintType.PaintUnderEverything:
                    kernelID = paintUnderEverythingKernelID;
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
                    kernelID = eraseKernelID;
                    break;
            }

            paintShader.SetBool("_FirstStroke", _firstStroke);
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
        
        public void DrawHighlight(Vector2 _lastPos, Vector2 _currentPos, float _strokeBrushSize, float _borderThickness)
        {
            _strokeBrushSize += _borderThickness;
            threadGroupSize.x = Mathf.CeilToInt((math.abs(_lastPos.x - _currentPos.x) + _strokeBrushSize * 2) / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt((math.abs(_lastPos.y - _currentPos.y) + _strokeBrushSize * 2) / threadGroupSizeOut.y);
        
            Vector2 startPos = GetStartPos(_lastPos, _currentPos, _strokeBrushSize);

            paintShader.SetVector("_CursorPos", _currentPos);
            paintShader.SetVector("_LastCursorPos", _lastPos);
            paintShader.SetVector("_StartPos", startPos);
            paintShader.SetFloat("_BrushSize", _strokeBrushSize);
            paintShader.SetTexture(highlightKernelID, "_SelectTex", rtSelect);

            paintShader.Dispatch(highlightKernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
        }

        private Vector2 GetStartPos(Vector2 a, Vector2 b, float _brushSize)
        {
            float lowestX = (a.x < b.x ? a.x : b.x) - _brushSize;
            float lowestY = (a.y < b.y ? a.y : b.y) - _brushSize;
            return new Vector2(lowestX, lowestY);
        }

        public void RedrawStroke(int _brushstrokStartID)
        {
            BrushStrokeID brushStrokeID = brushStrokesID[_brushstrokStartID];
            int startID = brushStrokeID.startID;
            int endID = brushStrokeID.endID;
            int newStrokeID = GetNewID();
            bool firstLoop = true;
            PaintType paintType = brushStrokeID.paintType;
            
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = brushStrokes[i];
        
                Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, paintType, stroke.lastTime, stroke.brushTime, firstLoop, newStrokeID);

                firstLoop = false;
            }
        }

        private void RedrawStroke(int _brushstrokStartID, PaintType _newPaintType)
        {
            BrushStrokeID brushStrokeID = brushStrokesID[_brushstrokStartID];
            int startID = brushStrokeID.startID;
            int endID = brushStrokeID.endID;
            int newStrokeID = GetNewID();
            bool firstLoop = true;
            PaintType paintType = _newPaintType;
            
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = brushStrokes[i];
        
                Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, paintType, stroke.lastTime, stroke.brushTime, firstLoop, newStrokeID);

                firstLoop = false;
            }
        }

        private void RedrawStrokeOptimized(int _brushstrokStartID, Vector4 _collisionBox)
        {
            BrushStrokeID brushStrokeID = brushStrokesID[_brushstrokStartID];
            int startID = brushStrokeID.startID;
            int endID = brushStrokeID.endID;
            int newStrokeID = GetNewID();
            bool firstLoop = true;
            PaintType paintType = brushStrokeID.paintType;
            
            Vector4 collisionBoxReset = _collisionBox;
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = brushStrokes[i];
                
                _collisionBox.x -= stroke.strokeBrushSize;
                _collisionBox.y -= stroke.strokeBrushSize;
                _collisionBox.z += stroke.strokeBrushSize;
                _collisionBox.w += stroke.strokeBrushSize;

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

        public void RedrawStroke(int _brushstrokStartID, float _lastTime, float _currentTime)
        {
            BrushStrokeID brushStrokeID = brushStrokesID[_brushstrokStartID];
            int startID = brushStrokeID.startID;
            int endID = brushStrokeID.endID;
            int newStrokeID = GetNewID();
            brushStrokeID.lastTime = _lastTime;
            brushStrokeID.currentTime = _currentTime;
            bool firstLoop = true;
            PaintType paintType = brushStrokeID.paintType;

            brushStrokesID[_brushstrokStartID] = brushStrokeID;

            //First erase the stroke you want to redraw
            if (paintType is PaintType.PaintUnderEverything or PaintType.PaintOverOwnLine)
            {
                RedrawStroke(_brushstrokStartID, PaintType.Erase);
            }

            Vector4 collisionBox = brushStrokesID[_brushstrokStartID].GetCollisionBox();

            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                //If stroke is not the stroke you want to redraw. Redraw it optimized
                if (i != _brushstrokStartID)
                {
                    RedrawStrokeOptimized(i, collisionBox);
                    continue;
                }
                //If stroke is the one you want to redraw then redo it using the new time variables
                float previousTime = _lastTime;
                for (int j = startID; j < endID; j++)
                {
                    BrushStroke stroke = brushStrokes[j];
        
                    float newTime = stroke.brushTime;
                    if (_lastTime >= 0 && _currentTime >= 0)
                    {
                        float idPercentage = ExtensionMethods.Remap(j, startID, endID, 0, 1);
                        newTime = (_currentTime - _lastTime) * idPercentage + _lastTime;
                    }

                    stroke.brushTime = newTime;
                    stroke.lastTime = previousTime;

                    brushStrokes[j] = stroke;
            
                    Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, paintType, stroke.lastTime, stroke.brushTime, firstLoop, newStrokeID);

                    firstLoop = false;
                    previousTime = newTime;
                }
            }
        }

        public void RemoveStroke(int _brushstrokStartID)
        {
            BrushStrokeID brushStrokeID = brushStrokesID[_brushstrokStartID];
            int startID = brushStrokeID.startID;
            int endID = brushStrokeID.endID;
            
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = brushStrokes[i];
        
                Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, PaintType.Erase);
            }

            int amountToRemove = endID - startID;
            if (_brushstrokStartID + 1 < brushStrokesID.Count)
            {
                AdjustBrushStrokeIDs(_brushstrokStartID, amountToRemove);
            }

            if (startID > 0)
            {
                brushStrokes.RemoveRange(startID - 1, amountToRemove + 1);
            }
            else
            {
                brushStrokes.Clear();
            }
            brushStrokesID.RemoveAt(_brushstrokStartID);
            RedrawAll();
        }
        
        private void AdjustBrushStrokeIDs(int _brushstrokStartID, int _amountToRemove)
        {
            Debug.Log($"Adjusting brushstroke start and end IDs");
            for (int i = _brushstrokStartID + 1; i < brushStrokesID.Count; i++)
            {
                BrushStrokeID brushStrokeID = brushStrokesID[_brushstrokStartID];
                brushStrokeID.startID -= _amountToRemove;
                brushStrokeID.endID -= _amountToRemove;
            }
        }

        public void RedrawAll()
        {
            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                RedrawStroke(i);
            }
        }

        public void FinishedStroke(Vector4 _collisionBox, PaintType _paintType, float _lastTime, float _currentTime)
        {
            brushStrokesID.Add(new BrushStrokeID(lastBrushDrawID, brushDrawID, _paintType, _lastTime, _currentTime, _collisionBox));
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

    public enum PaintType
    {
        PaintUnderOwnLine,
        PaintUnderEverything,
        PaintOverOwnLine,
        PaintOverEverything,
        Erase
    }

    public struct BrushStrokeID
    {
        public int startID;
        public int endID;
        public float collisionBoxX;
        public float collisionBoxY;
        public float collisionBoxZ;
        public float collisionBoxW;
        public PaintType paintType;
        public float lastTime;
        public float currentTime;

        public BrushStrokeID(int _startID, int _endID, PaintType _paintType, float _lastTime, float _currentTime, Vector4 _collisionBox) : this()
        {
            startID = _startID;
            endID = _endID;
            paintType = _paintType;
            lastTime = _lastTime;
            currentTime = _currentTime;
            collisionBoxX = _collisionBox.x;
            collisionBoxY = _collisionBox.y;
            collisionBoxZ = _collisionBox.z;
            collisionBoxW = _collisionBox.w;
        }

        public Vector4 GetCollisionBox()
        {
            return new Vector4(collisionBoxX, collisionBoxY, collisionBoxZ, collisionBoxW);
        }
    }
}