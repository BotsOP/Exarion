using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI;
using Undo;
using UnityEngine;

namespace Drawing
{
    public class DrawingManager : MonoBehaviour, IDataPersistence
    {
        [SerializeField] private Camera cam;
        [SerializeField] private Material drawingMat;
        [SerializeField] private Material displayMat;
        [SerializeField] private Material selectMat;
        public int imageWidth = 2048;
        public int imageHeight = 2048;
    
        [SerializeField] private PaintType paintType;
        public Transform ball1;
        public Transform ball2;
        public Drawing drawer;
    
        private RenderTexture drawingRenderTexture;
        private int kernelID;
        private Vector2 threadGroupSizeOut;
        private Vector2 threadGroupSize;
        private Vector2 lastCursorPos;
        private bool firstUse = true;
        private List<BrushStroke> tempBrushStrokes;

        private float brushSize;
        private int newBrushStrokeID;
        private float cachedTime;
        private float startBrushStrokeTime;
        private Vector4 collisionBox;
        private Vector4 resetBox;
        private CommandManager commandManager;
        private float time;

        void OnEnable()
        {
            drawer = new Drawing(imageWidth, imageHeight);
            commandManager = FindObjectOfType<CommandManager>();

            drawingMat.SetTexture("_MainTex", drawer.rt);
            displayMat.SetTexture("_MainTex", drawer.rt);
            selectMat.SetTexture("_MainTex", drawer.rtSelect);

            resetBox = new Vector4(imageWidth, imageHeight, 0, 0);
            tempBrushStrokes = new List<BrushStroke>();
            collisionBox = resetBox;
            
            EventSystem.Subscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem.Subscribe(EventType.CLEAR_HIGHLIGHT, ClearHighlightStroke);
            EventSystem<Vector2>.Subscribe(EventType.DRAW, Draw);
            //Include the change brush size in the draw event
            EventSystem<float>.Subscribe(EventType.CHANGE_BRUSH_SIZE, SetBrushSize);
            EventSystem<float>.Subscribe(EventType.TIME, SetTime);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.HIGHLIGHT, HighlightStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem<BrushStrokeID>.Subscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_STROKE, AddStroke);
        }

        private void OnDisable()
        {
            EventSystem.Unsubscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem.Unsubscribe(EventType.CLEAR_HIGHLIGHT, ClearHighlightStroke);
            EventSystem<Vector2>.Unsubscribe(EventType.DRAW, Draw);
            EventSystem<float>.Unsubscribe(EventType.CHANGE_BRUSH_SIZE, SetBrushSize);
            EventSystem<float>.Unsubscribe(EventType.TIME, SetTime);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.HIGHLIGHT, HighlightStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
        }

        private void SetTime(float _time)
        {
            time = _time;
            displayMat.SetFloat("_CustomTime", time);
        }
        private void SetShowcaseTime(float _time)
        {
            displayMat.SetFloat("_TimeSpeed", _time);
        }

        private void SetBrushSize(float _brushSize)
        {
            brushSize = _brushSize;
        }


        private void Draw(Vector2 _mousePos)
        {
            bool firstDraw = firstUse;

            if (lastCursorPos == _mousePos)
            {
                return;
            }

            if (firstUse)
            {
                startBrushStrokeTime = time;
                newBrushStrokeID = drawer.GetNewID();
                lastCursorPos = _mousePos;
                firstUse = false;
            }

            drawer.Draw(lastCursorPos, _mousePos, brushSize, paintType, cachedTime, time, firstDraw, newBrushStrokeID);
            tempBrushStrokes.Add(new BrushStroke(lastCursorPos, _mousePos, brushSize, time, cachedTime));

            lastCursorPos = _mousePos;
                
            if (collisionBox.x > _mousePos.x) { collisionBox.x = _mousePos.x; }
            if (collisionBox.y > _mousePos.y) { collisionBox.y = _mousePos.y; }
            if (collisionBox.z < _mousePos.x) { collisionBox.z = _mousePos.x; }
            if (collisionBox.w < _mousePos.y) { collisionBox.w = _mousePos.y; }
            // ball1.position = new Vector3(collisionBox.x / imageWidth, collisionBox.y / imageHeight, 0);
            // ball2.position = new Vector3(collisionBox.z / imageWidth, collisionBox.w / imageHeight, 0);
        }
        
        private void StoppedDrawing()
        {
            List<BrushStroke> brushStrokes = new List<BrushStroke>(tempBrushStrokes);
            BrushStrokeID brushStrokeID = new BrushStrokeID(
                brushStrokes, paintType, startBrushStrokeTime,
                time, collisionBox, drawer.brushStrokesID.Count);
            
            drawer.brushStrokesID.Add(brushStrokeID);

            ICommand draw = new DrawCommand(brushStrokeID);
            commandManager.AddCommand(draw);

            EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, brushStrokeID);
            
            collisionBox = resetBox;
            tempBrushStrokes.Clear();
            firstUse = true;
        }
        
        private void RedrawStroke(BrushStrokeID _brushStrokeID)
        {
            drawer.RedrawAllSafe(_brushStrokeID);
        }
        
        private void RedrawStrokes(List<BrushStrokeID> _brushStrokeIDs)
        {
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }

        private void AddStroke(BrushStrokeID _brushStrokeID)
        {
            if (_brushStrokeID.indexWhenDrawn > drawer.brushStrokesID.Count)
            {
                drawer.brushStrokesID.Add(_brushStrokeID);
            }
            else
            {
                drawer.brushStrokesID.Insert(_brushStrokeID.indexWhenDrawn, _brushStrokeID);
            }
            
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, _brushStrokeID);
            drawer.RedrawAllSafe(_brushStrokeID);
        }
        
        private void AddStroke(List<BrushStrokeID> _brushStrokeIDs)
        {
            foreach (BrushStrokeID brushStrokeID in _brushStrokeIDs)
            {
                if (brushStrokeID.indexWhenDrawn > drawer.brushStrokesID.Count)
                {
                    drawer.brushStrokesID.Add(brushStrokeID);
                }
                else
                {
                    drawer.brushStrokesID.Insert(brushStrokeID.indexWhenDrawn, brushStrokeID);
                }
            }
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }

        private void RemoveStroke(BrushStrokeID _brushStrokeID)
        {
            foreach (var brushStroke in _brushStrokeID.brushStrokes)
            {
                drawer.Draw(brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, PaintType.Erase);
            }

            drawer.brushStrokesID.Remove(_brushStrokeID);
            drawer.RedrawAllSafe(_brushStrokeID);
        }
        
        private void RemoveStroke(List<BrushStrokeID> _brushStrokeIDs)
        {
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                foreach (var brushStroke in brushStrokeID.brushStrokes)
                {
                    drawer.Draw(brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, PaintType.Erase);
                }
                
                drawer.brushStrokesID.Remove(brushStrokeID);
            }
            
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        
        private void HighlightStroke(BrushStrokeID _brushstrokeID)
        {
            foreach (var brushStroke in _brushstrokeID.brushStrokes)
            {
                float highlightBrushThickness = Mathf.Clamp(brushStroke.strokeBrushSize / 2, 5, 1024);

                drawer.DrawHighlight(brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, HighlightType.Paint, highlightBrushThickness);
            }
            
            foreach (var brushStroke in _brushstrokeID.brushStrokes)
            {
                drawer.DrawHighlight(brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, HighlightType.Erase, -5);
            }
        }
        private void ClearHighlightStroke()
        {
            Graphics.SetRenderTarget(drawer.rtSelect);
            GL.Clear(false, true, Color.white);
        }

        void Update()
        {
            cachedTime = time;
        }

        public void LoadData(ToolData _data)
        {
            drawer.brushStrokesID = _data.brushStrokesID;
            drawer.RedrawAll();
            
            for (int i = 0; i < drawer.brushStrokesID.Count; i++)
            {
                EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, drawer.brushStrokesID[i]);
            }
        }

        public void SaveData(ToolData _data)
        {
            _data.brushStrokesID = drawer.brushStrokesID;
        }
    }

    
}
