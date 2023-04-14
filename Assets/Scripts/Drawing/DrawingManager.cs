using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using UI;
using Undo;
using UnityEngine;
using EventType = Managers.EventType;

namespace Drawing
{
    public class DrawingManager : MonoBehaviour, IDataPersistence
    {
        [Header("Materials")]
        [SerializeField] private Material drawingMat;
        [SerializeField] private Material displayMat;
        [SerializeField] private Material selectMat;
        [SerializeField] private Material previewMat;
        
        [Header("Canvas Settings")]
        [SerializeField] private int imageWidth = 2048;
        [SerializeField] private int imageHeight = 2048;
    
        [Header("Painttype")]
        [SerializeField] private PaintType paintType;
        
        public Drawing drawer;
    
        private RenderTexture drawingRenderTexture;
        private int kernelID;
        private Vector2 threadGroupSizeOut;
        private Vector2 threadGroupSize;
        private Vector2 lastCursorPos;
        private bool firstUse = true;
        private List<BrushStroke> tempBrushStrokes;
        private List<BrushStrokeID> selectedBrushStrokes;

        private DrawHighlight highlighter;
        private DrawPreview previewer;
        private float brushSize;
        private int newBrushStrokeID;
        private float cachedTime;
        private float startBrushStrokeTime;
        private Vector4 collisionBox;
        private Vector4 resetBox;
        private Vector2 tempAvgPos;
        private float time;

        void OnEnable()
        {
            drawer = new Drawing(imageWidth, imageHeight);
            highlighter = new DrawHighlight(imageWidth, imageHeight);
            previewer = new DrawPreview(imageWidth, imageHeight);
            
            drawingMat.SetTexture("_MainTex", drawer.rt);
            displayMat.SetTexture("_MainTex", drawer.rt);
            selectMat.SetTexture("_MainTex", highlighter.rtHighlight);
            previewMat.SetTexture("_MainTex", previewer.rtPreview);

            resetBox = new Vector4(imageWidth, imageHeight, 0, 0);
            tempBrushStrokes = new List<BrushStroke>();
            selectedBrushStrokes = new List<BrushStrokeID>();
            collisionBox = resetBox;
            
            EventSystem.Subscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem.Subscribe(EventType.CLEAR_HIGHLIGHT, ClearHighlightStroke);
            EventSystem.Subscribe(EventType.STOPPED_SETTING_BRUSH_SIZE, ClearPreview);
            EventSystem<Vector2>.Subscribe(EventType.DRAW, Draw);
            EventSystem<float>.Subscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Subscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Subscribe(EventType.SELECT_BRUSHSTROKE, SelectBrushStroke);
            EventSystem<float>.Subscribe(EventType.TIME, SetTime);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.HIGHLIGHT, HighlightStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem<BrushStrokeID>.Subscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<Vector2>.Subscribe(EventType.MOVE_STROKE, MoveStrokes);
        }

        private void OnDisable()
        {
            EventSystem.Unsubscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem.Unsubscribe(EventType.CLEAR_HIGHLIGHT, ClearHighlightStroke);
            EventSystem.Unsubscribe(EventType.STOPPED_SETTING_BRUSH_SIZE, ClearPreview);
            EventSystem<Vector2>.Unsubscribe(EventType.DRAW, Draw);
            EventSystem<float>.Unsubscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Unsubscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Unsubscribe(EventType.SELECT_BRUSHSTROKE, SelectBrushStroke);
            EventSystem<float>.Unsubscribe(EventType.TIME, SetTime);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.HIGHLIGHT, HighlightStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<Vector2>.Unsubscribe(EventType.MOVE_STROKE, MoveStrokes);
        }

        private void SetTime(float _time)
        {
            time = _time;
            displayMat.SetFloat("_CustomTime", time);
        }

        private void SetBrushSize(float _brushSize)
        {
            brushSize = _brushSize;
        }
        private void SetBrushSize(Vector2 _mousePos)
        {
            previewer.Preview(_mousePos, brushSize);
        }

        private void ClearPreview()
        {
            previewer.ClearPreview();
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
                cachedTime = cachedTime > 1 ? 0 : cachedTime;
                newBrushStrokeID = drawer.GetNewID();
                lastCursorPos = _mousePos;
                firstUse = false;
            }

            drawer.Draw(lastCursorPos, _mousePos, brushSize, paintType, cachedTime, time, firstDraw, newBrushStrokeID);
            tempBrushStrokes.Add(new BrushStroke(lastCursorPos, _mousePos, brushSize, time, cachedTime));

            tempAvgPos += (lastCursorPos + _mousePos) / 2;

            lastCursorPos = _mousePos;
                
            if (collisionBox.x > _mousePos.x - brushSize) { collisionBox.x = _mousePos.x - brushSize; }
            if (collisionBox.y > _mousePos.y - brushSize) { collisionBox.y = _mousePos.y - brushSize; }
            if (collisionBox.z < _mousePos.x + brushSize) { collisionBox.z = _mousePos.x + brushSize; }
            if (collisionBox.w < _mousePos.y + brushSize) { collisionBox.w = _mousePos.y + brushSize; }
        }
        
        void Update()
        {
            cachedTime = time;
        }

        private void StoppedDrawing()
        {
            tempAvgPos /= tempBrushStrokes.Count;
            List<BrushStroke> brushStrokes = new List<BrushStroke>(tempBrushStrokes);
            
            BrushStrokeID brushStrokeID = new BrushStrokeID(
                brushStrokes, paintType, startBrushStrokeTime, time, collisionBox, drawer.brushStrokesID.Count, tempAvgPos);
            
            drawer.brushStrokesID.Add(brushStrokeID);
            
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, brushStrokeID);
            
            tempAvgPos = Vector2.zero;
            collisionBox = resetBox;
            tempBrushStrokes.Clear();
            firstUse = true;
        }

        private void MoveStrokes(Vector2 _dir)
        {
            foreach (var brushStrokeID in selectedBrushStrokes)
            {
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
                for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[i];
                    brushStroke.lastPosX += _dir.x;
                    brushStroke.lastPosY += _dir.y;
                    brushStroke.currentPosX += _dir.x;
                    brushStroke.currentPosY += _dir.y;
                    brushStrokeID.brushStrokes[i] = brushStroke;
                }
                brushStrokeID.collisionBoxX += _dir.x;
                brushStrokeID.collisionBoxY += _dir.y;
                brushStrokeID.collisionBoxZ += _dir.x;
                brushStrokeID.collisionBoxW += _dir.y;
            }
            drawer.RedrawAllSafe(selectedBrushStrokes);
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

            selectedBrushStrokes.Remove(_brushStrokeID);
            highlighter.ClearHighlight();
            HighlightStroke(selectedBrushStrokes);
            
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

                selectedBrushStrokes.Remove(brushStrokeID);
                drawer.brushStrokesID.Remove(brushStrokeID);
            }
            
            highlighter.ClearHighlight();
            HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        
        private void HighlightStroke(List<BrushStrokeID> _brushStrokeIDs)
        {
            highlighter.HighlightStroke(_brushStrokeIDs);
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                if (selectedBrushStrokes.Contains(brushStrokeID))
                {
                    selectedBrushStrokes.Remove(brushStrokeID);
                    continue;
                }
                selectedBrushStrokes.Add(brushStrokeID);
            }
        }
        private void ClearHighlightStroke()
        {
            selectedBrushStrokes.Clear();
            highlighter.ClearHighlight();
        }
        
        private void SelectBrushStroke(Vector2 _mousePos)
        {
            foreach (BrushStrokeID brushStrokeID in drawer.brushStrokesID)
            {
                if (drawer.IsMouseOverBrushStroke(brushStrokeID, _mousePos))
                {
                    selectedBrushStrokes.Add(brushStrokeID);
                    highlighter.HighlightStroke(brushStrokeID);
                    //EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.SELECT_TIMELINECLIP, selectedBrushStrokes);
                    return;
                }
            }
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
