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
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.HIGHLIGHT, HighlightStroke);
            EventSystem<List<TimelineClip>>.Subscribe(EventType.DELETE_STROKE, DeleteStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem<List<BrushStroke>, BrushStrokeID>.Subscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<List<BrushStroke>>, List<BrushStrokeID>, List<TimelineClip>>.Subscribe(EventType.ADD_STROKE, AddStroke);
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
            EventSystem<List<TimelineClip>>.Unsubscribe(EventType.DELETE_STROKE, DeleteStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem<List<BrushStroke>, BrushStrokeID>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<List<BrushStroke>>, List<BrushStrokeID>, List<TimelineClip>>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
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

            EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, brushStrokeID);
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
            
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, _brushStrokeID);
            drawer.RedrawAllSafe(_brushStrokeID);
        }
        
        private void AddStroke(List<List<BrushStroke>> _brushStrokesList, List<BrushStrokeID> _brushStrokeIDs, List<TimelineClip> _timelineClips)
        {
            for (int i = 0; i < _brushStrokeIDs.Count; i++)
            {
                drawer.brushStrokes.AddRange(_brushStrokesList[i]);
                _brushStrokeIDs[i].startID = drawer.lastBrushDrawID;
                _brushStrokeIDs[i].endID = drawer.brushDrawID;
            
                if (_brushStrokeIDs[i].indexWhenDrawn > drawer.brushStrokesID.Count)
                {
                    drawer.brushStrokesID.Add(_brushStrokeIDs[i]);
                }
                else
                {
                    drawer.brushStrokesID.Insert(_brushStrokeIDs[i].indexWhenDrawn, _brushStrokeIDs[i]);
                }
                drawer.lastDrawnStrokes.Add(_brushStrokeIDs[i]);
            
                EventSystem<BrushStrokeID, TimelineClip>.RaiseEvent(EventType.FINISHED_STROKE, _brushStrokeIDs[i], _timelineClips[i]);
            }
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }

        private void DeleteStroke(List<TimelineClip> _timelineClips)
        {
            List<List<BrushStroke>> brushStrokesList = new List<List<BrushStroke>>();
            List<BrushStrokeID> brushStrokeIDs = _timelineClips.Select(_clip => _clip.brushStrokeID).ToList();

            foreach (var brushStrokeID in brushStrokeIDs)
            {
                int startID = brushStrokeID.startID;
                int count = brushStrokeID.endID - startID;
                List<BrushStroke> brushStrokes = drawer.brushStrokes.GetRange(startID, count);
                brushStrokesList.Add(brushStrokes);
            
                for (int i = startID; i < brushStrokeID.endID; i++)
                {
                    BrushStroke stroke = drawer.brushStrokes[i];
        
                    drawer.Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, PaintType.Erase);
                }
            
                drawer.brushStrokes.RemoveRange(startID, count);

                foreach (BrushStrokeID clip in drawer.brushStrokesID.Where(clip => clip.startID >= brushStrokeID.endID))
                {
                    clip.startID -= count;
                    clip.endID -= count;
                }
            
                drawer.brushStrokesID.Remove(brushStrokeID);
                drawer.lastDrawnStrokes.Remove(brushStrokeID);
            }
            
            drawer.RedrawAllSafe(brushStrokeIDs);
            commandManager.AddCommand(new DeleteClipMultipleCommand(brushStrokesList, _timelineClips));
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
            drawer.brushStrokes.RemoveRange(startID, amountToRemove);

            foreach (BrushStrokeID clip in drawer.brushStrokesID.Where(clip => clip.startID >= _brushStrokeID.endID))
            {
                clip.startID -= amountToRemove;
                clip.endID -= amountToRemove;
            }
            drawer.brushStrokesID.Remove(_brushStrokeID);
            drawer.lastDrawnStrokes.Remove(_brushStrokeID);
            
            drawer.RedrawAllSafe(_brushStrokeID);
        }
        
        private void RemoveStroke(List<BrushStrokeID> _brushStrokeIDs)
        {
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                int startID = brushStrokeID.startID;
                int endID = brushStrokeID.endID;
            
                for (int i = startID; i < endID; i++)
                {
                    BrushStroke stroke = drawer.brushStrokes[i];
        
                    drawer.Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, PaintType.Erase);
                }

                int amountToRemove = endID - startID;
                drawer.brushStrokes.RemoveRange(startID, amountToRemove);

                foreach (BrushStrokeID clip in drawer.brushStrokesID.Where(_strokeID => _strokeID.startID >= brushStrokeID.endID))
                {
                    clip.startID -= amountToRemove;
                    clip.endID -= amountToRemove;
                }
                drawer.brushStrokesID.Remove(brushStrokeID);
                drawer.lastDrawnStrokes.Remove(brushStrokeID);
            }
            
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        
        private void HighlightStroke(BrushStrokeID _brushstrokeID)
        {
            int startID = _brushstrokeID.startID;
            int endID = _brushstrokeID.endID;
            
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
                EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, drawer.brushStrokesID[i]);
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
