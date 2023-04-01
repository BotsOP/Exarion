using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            collisionBox = resetBox;
            
            EventSystem.Subscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem.Subscribe(EventType.CLEAR_HIGHLIGHT, ClearHighlightStroke);
            EventSystem<Vector2>.Subscribe(EventType.DRAW, Draw);
            //Include the change brush size in the draw event
            EventSystem<float>.Subscribe(EventType.CHANGE_BRUSH_SIZE, SetBrushSize);
            EventSystem<float>.Subscribe(EventType.TIME, SetTime);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.HIGHLIGHT, HighlightStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.DELETE_CLIP, DeleteStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem<List<BrushStroke>, BrushStrokeID>.Subscribe(EventType.ADD_STROKE, AddStroke);
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
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.DELETE_CLIP, DeleteStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem<List<BrushStroke>, BrushStrokeID>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
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
            drawer.brushStrokes.Add(new BrushStroke(lastCursorPos, _mousePos, brushSize, time, cachedTime));

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
            BrushStrokeID brushStrokeID = new BrushStrokeID(
                drawer.lastBrushDrawID, drawer.brushDrawID, paintType, startBrushStrokeTime,
                time, collisionBox, drawer.brushStrokesID.Count);
            
            drawer.brushStrokesID.Add(brushStrokeID);
            drawer.lastDrawnStrokes.Add(brushStrokeID);
            
            int startID = drawer.brushStrokesID[^1].startID;
            int count = drawer.brushStrokesID[^1].endID - startID;
            List<BrushStroke> brushStrokes = drawer.brushStrokes.GetRange(startID, count);
            ICommand draw = new DrawCommand(brushStrokes, drawer.brushStrokesID[^1]);
            commandManager.AddCommand(draw);

            EventSystem<BrushStrokeID, float, float>.RaiseEvent(EventType.FINISHED_STROKE, drawer.brushStrokesID[^1], startBrushStrokeTime, time);
            collisionBox = resetBox;
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

        private void AddStroke(List<BrushStroke> _brushStrokes, BrushStrokeID _brushStrokeID)
        {
            drawer.brushStrokes.AddRange(_brushStrokes);
            _brushStrokeID.startID = drawer.lastBrushDrawID;
            _brushStrokeID.endID = drawer.brushDrawID;
            
            if (_brushStrokeID.indexWhenDrawn > drawer.brushStrokesID.Count)
            {
                drawer.brushStrokesID.Add(_brushStrokeID);
            }
            else
            {
                drawer.brushStrokesID.Insert(_brushStrokeID.indexWhenDrawn, _brushStrokeID);
            }
            drawer.lastDrawnStrokes.Add(_brushStrokeID);
            
            EventSystem<BrushStrokeID, float, float>.RaiseEvent(EventType.FINISHED_STROKE, _brushStrokeID, _brushStrokeID.lastTime, _brushStrokeID.currentTime);
            drawer.RedrawAllOptimized(_brushStrokeID);
        }
        private void DeleteStroke(BrushStrokeID _brushStrokeID)
        {
            int startID = _brushStrokeID.startID;
            int count = _brushStrokeID.endID - startID;
            List<BrushStroke> brushStrokes = drawer.brushStrokes.GetRange(startID, count);
            
            for (int i = startID; i < _brushStrokeID.endID; i++)
            {
                BrushStroke stroke = drawer.brushStrokes[i];
        
                drawer.Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, PaintType.Erase);
            }
            
            drawer.brushStrokes.RemoveRange(startID, count);

            foreach (BrushStrokeID clip in drawer.brushStrokesID.Where(clip => clip.startID >= _brushStrokeID.endID))
            {
                clip.startID -= count;
                clip.endID -= count;
            }
            
            drawer.brushStrokesID.Remove(_brushStrokeID);
            drawer.lastDrawnStrokes.Remove(_brushStrokeID);
            
            drawer.RedrawAllOptimized(_brushStrokeID);
            
            ICommand deleteStroke = new DeleteClipCommand(brushStrokes, _brushStrokeID);
            commandManager.AddCommand(deleteStroke);
        }
        private void RemoveStroke(BrushStrokeID _brushStrokeID)
        {
            int startID = _brushStrokeID.startID;
            int endID = _brushStrokeID.endID;
            
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = drawer.brushStrokes[i];
        
                drawer.Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, PaintType.Erase);
            }

            int amountToRemove = endID - startID;
            if (startID > 0)
            {
                drawer.brushStrokes.RemoveRange(startID, amountToRemove);
            }
            else
            {
                drawer.brushStrokes.Clear();
            }
            
            foreach (BrushStrokeID clip in drawer.brushStrokesID.Where(clip => clip.startID >= _brushStrokeID.endID))
            {
                clip.startID -= amountToRemove;
                clip.endID -= amountToRemove;
            }
            drawer.brushStrokesID.Remove(_brushStrokeID);
            drawer.lastDrawnStrokes.Remove(_brushStrokeID);
            
            drawer.RedrawAllOptimized(_brushStrokeID);
        }
        
        private void HighlightStroke(BrushStrokeID _brushstrokStartID)
        {
            int startID = _brushstrokStartID.startID;
            int endID = _brushstrokStartID.endID;
            
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = drawer.brushStrokes[i];
                float highlightBrushThickness = Mathf.Clamp(stroke.strokeBrushSize / 2, 5, 1024);

                drawer.DrawHighlight(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, HighlightType.Paint, highlightBrushThickness);
            }
            
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = drawer.brushStrokes[i];

                drawer.DrawHighlight(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, HighlightType.Erase, -5);
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
            drawer.brushStrokes = _data.brushStrokes;
            drawer.brushStrokesID = _data.brushStrokesID;
            drawer.lastDrawnStrokes = _data.lastDrawnStrokes;
            drawer.RedrawAll();
            
            for (int i = 0; i < drawer.brushStrokesID.Count; i++)
            {
                BrushStrokeID brushStrokeID = drawer.brushStrokesID[i];
                EventSystem<BrushStrokeID, float, float>.RaiseEvent(EventType.FINISHED_STROKE, drawer.brushStrokesID[i], brushStrokeID.lastTime, brushStrokeID.currentTime);
            }
        }

        public void SaveData(ToolData _data)
        {
            _data.brushStrokes = drawer.brushStrokes;
            _data.brushStrokesID = drawer.brushStrokesID;
            _data.lastDrawnStrokes = drawer.lastDrawnStrokes;
        }
    }

    
}
