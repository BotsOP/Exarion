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
        private DrawStamp drawStamp;
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
            drawStamp = new DrawStamp();
            
            drawingMat.SetTexture("_MainTex", drawer.rt);
            displayMat.SetTexture("_MainTex", drawer.rt);
            selectMat.SetTexture("_MainTex", highlighter.rtHighlight);
            previewMat.SetTexture("_MainTex", previewer.rtPreview);

            resetBox = new Vector4(imageWidth, imageHeight, 0, 0);
            tempBrushStrokes = new List<BrushStroke>();
            selectedBrushStrokes = new List<BrushStrokeID>();
            collisionBox = resetBox;
            
            EventSystem.Subscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem.Subscribe(EventType.CLEAR_SELECT, ClearHighlightStroke);
            EventSystem.Subscribe(EventType.STOPPED_SETTING_BRUSH_SIZE, ClearPreview);
            EventSystem<Vector2>.Subscribe(EventType.DRAW, Draw);
            EventSystem<float>.Subscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Subscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Subscribe(EventType.SELECT_BRUSHSTROKE, SelectBrushStroke);
            EventSystem<float>.Subscribe(EventType.TIME, SetTime);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem<BrushStrokeID>.Subscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<Vector2>.Subscribe(EventType.MOVE_STROKE, MoveStrokes);
            EventSystem<float>.Subscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            EventSystem<float>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<Vector2, string>.Subscribe(EventType.SPAWN_STAMP, DrawStamp);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem.Subscribe(EventType.MOVE_STROKE, StoppedMovingStroke);
            EventSystem.Subscribe(EventType.RESIZE_STROKE, StoppedResizing);
            EventSystem.Subscribe(EventType.ROTATE_STROKE, StoppedRotating);
            EventSystem<Vector2, List<BrushStrokeID>>.Subscribe(EventType.MOVE_STROKE, MoveStrokes);
            EventSystem<float, List<BrushStrokeID>>.Subscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            EventSystem<float, List<BrushStrokeID>>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem.Subscribe(EventType.DUPLICATE_STROKE, DuplicateBrushStrokes);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.SELECT_BRUSHSTROKE, HighlightStroke);
        }

        private void OnDisable()
        {
            EventSystem.Unsubscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem.Unsubscribe(EventType.CLEAR_SELECT, ClearHighlightStroke);
            EventSystem.Unsubscribe(EventType.STOPPED_SETTING_BRUSH_SIZE, ClearPreview);
            EventSystem<Vector2>.Unsubscribe(EventType.DRAW, Draw);
            EventSystem<float>.Unsubscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Unsubscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Unsubscribe(EventType.SELECT_BRUSHSTROKE, SelectBrushStroke);
            EventSystem<float>.Unsubscribe(EventType.TIME, SetTime);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<Vector2>.Unsubscribe(EventType.MOVE_STROKE, MoveStrokes);
            EventSystem<float>.Unsubscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            EventSystem<float>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<Vector2, string>.Unsubscribe(EventType.SPAWN_STAMP, DrawStamp);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem.Unsubscribe(EventType.MOVE_STROKE, StoppedMovingStroke);
            EventSystem.Unsubscribe(EventType.RESIZE_STROKE, StoppedResizing);
            EventSystem.Unsubscribe(EventType.ROTATE_STROKE, StoppedRotating);
            EventSystem<Vector2, List<BrushStrokeID>>.Unsubscribe(EventType.MOVE_STROKE, MoveStrokes);
            EventSystem<float, List<BrushStrokeID>>.Unsubscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            EventSystem<float, List<BrushStrokeID>>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem.Unsubscribe(EventType.DUPLICATE_STROKE, DuplicateBrushStrokes);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.SELECT_BRUSHSTROKE, HighlightStroke);
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
            foreach (BrushStroke brushStroke in _brushStrokeID.brushStrokes)
            {
                drawer.Draw(brushStroke.GetLastPos(), brushStroke.GetCurrentPos(), brushStroke.strokeBrushSize, PaintType.Erase);
            }

            selectedBrushStrokes.Remove(_brushStrokeID);
            highlighter.HighlightStroke(selectedBrushStrokes);
            
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
            
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        
        private void HighlightStroke(BrushStrokeID _brushStrokeID)
        {
            if (selectedBrushStrokes.Remove(_brushStrokeID))
            {
                highlighter.HighlightStroke(selectedBrushStrokes);
                return;
            }

            selectedBrushStrokes.Add(_brushStrokeID);
            highlighter.HighlightStroke(selectedBrushStrokes);
        }
        private void HighlightStroke(List<BrushStrokeID> _brushStrokeIDs)
        {
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                if (selectedBrushStrokes.Remove(brushStrokeID))
                {
                    highlighter.HighlightStroke(selectedBrushStrokes);
                    return;
                }

                selectedBrushStrokes.Add(brushStrokeID);
            }
            highlighter.HighlightStroke(selectedBrushStrokes);
        }
        private void RemoveHighlight(BrushStrokeID _brushStrokeID)
        {
            selectedBrushStrokes.Remove(_brushStrokeID);
            highlighter.HighlightStroke(selectedBrushStrokes);
        }
        private void RemoveHighlight(List<BrushStrokeID> _brushStrokeIDs)
        {
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                selectedBrushStrokes.Remove(brushStrokeID);
            }
            highlighter.HighlightStroke(selectedBrushStrokes);
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
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        if (selectedBrushStrokes.Remove(brushStrokeID))
                        {
                            highlighter.HighlightStroke(selectedBrushStrokes);
                            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REMOVE_SELECT, brushStrokeID);
                            return;
                        }
                        selectedBrushStrokes.Add(brushStrokeID);
                        highlighter.HighlightStroke(selectedBrushStrokes);
                        EventSystem<BrushStrokeID>.RaiseEvent(EventType.SELECT_TIMELINECLIP, brushStrokeID);
                        return;
                    }
                    EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
                    selectedBrushStrokes.Add(brushStrokeID);
                    highlighter.HighlightStroke(selectedBrushStrokes);
                    EventSystem<BrushStrokeID>.RaiseEvent(EventType.SELECT_TIMELINECLIP, brushStrokeID);
                    
                    return;
                }
            }
        }

        private void DuplicateBrushStrokes()
        {
            List<BrushStrokeID> duplicateBrushStrokeIDs = new List<BrushStrokeID>();
            foreach (var brushStrokeID in selectedBrushStrokes)
            {
                BrushStrokeID duplicateBrushStrokeID = new BrushStrokeID(brushStrokeID, drawer.brushStrokesID.Count);
                
                drawer.brushStrokesID.Add(duplicateBrushStrokeID);
                EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, duplicateBrushStrokeID);
                
                duplicateBrushStrokeIDs.Add(duplicateBrushStrokeID);
            }
            
            MoveStrokes(new Vector2(10, 10), duplicateBrushStrokeIDs);
            EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
            selectedBrushStrokes = duplicateBrushStrokeIDs;
            EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.SELECT_TIMELINECLIP, selectedBrushStrokes);
        }

        private Vector2 lastMovePos;
        private void MoveStrokes(Vector2 _dir)
        {
            if (_dir == Vector2.zero)
                return;

            lastMovePos += _dir;
            
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
                brushStrokeID.avgPosX += _dir.x;
                brushStrokeID.avgPosY += _dir.y;
                brushStrokeID.collisionBoxX += _dir.x;
                brushStrokeID.collisionBoxY += _dir.y;
                brushStrokeID.collisionBoxZ += _dir.x;
                brushStrokeID.collisionBoxW += _dir.y;
                brushStrokeID.RecalculateAvgPos();
            }
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(selectedBrushStrokes);
        }
        
        private void MoveStrokes(Vector2 _dir, List<BrushStrokeID> _brushStrokeIDs)
        {
            if (_dir == Vector2.zero)
                return;
            
            foreach (var brushStrokeID in _brushStrokeIDs)
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
                brushStrokeID.avgPosX += _dir.x;
                brushStrokeID.avgPosY += _dir.y;
                brushStrokeID.collisionBoxX += _dir.x;
                brushStrokeID.collisionBoxY += _dir.y;
                brushStrokeID.collisionBoxZ += _dir.x;
                brushStrokeID.collisionBoxW += _dir.y;
                brushStrokeID.RecalculateAvgPos();
            }
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        private void StoppedMovingStroke()
        {
            ICommand moveCommand = new MoveCommand(lastMovePos, selectedBrushStrokes);
            EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, moveCommand);
            lastMovePos = Vector2.zero;
        }
        
        private float resizeAmount = 1;
        private void ResizeStrokes(float _sizeIncrease)
        {
            if (_sizeIncrease == 1)
                return;

            Debug.Log(_sizeIncrease);
            resizeAmount -= 1 - _sizeIncrease;
            
            Vector2 allAvgPos = Vector2.zero;
            foreach (var brushStrokeID in selectedBrushStrokes)
            {
                allAvgPos += brushStrokeID.GetAvgPos();
            }
            allAvgPos /= selectedBrushStrokes.Count;
            
            foreach (var brushStrokeID in selectedBrushStrokes)
            {
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
                
                for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[i];
                    Vector2 lastPos = brushStroke.GetLastPos();
                    Vector2 currentPos = brushStroke.GetCurrentPos();
                    Vector2 lastPosDir = (lastPos - allAvgPos);
                    Vector2 currentPosDir = (currentPos - allAvgPos);
                    lastPos = allAvgPos + lastPosDir * _sizeIncrease;
                    currentPos = allAvgPos + currentPosDir * _sizeIncrease;
                    
                    brushStroke.lastPosX = lastPos.x;
                    brushStroke.lastPosY = lastPos.y;
                    brushStroke.currentPosX = currentPos.x;
                    brushStroke.currentPosY = currentPos.y;
                    brushStrokeID.brushStrokes[i] = brushStroke;
                }
                brushStrokeID.RecalculateCollisionBoxAndAvgPos();
            }
            
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(selectedBrushStrokes);
        }
        private void ResizeStrokes(float _sizeIncrease, List<BrushStrokeID> _brushStrokeIDs)
        {
            Debug.Log(_sizeIncrease);
            if (_sizeIncrease == 1)
                return;

            _sizeIncrease = 1 / _sizeIncrease;
            Debug.Log(_sizeIncrease);
            
            Vector2 allAvgPos = Vector2.zero;
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                allAvgPos += brushStrokeID.GetAvgPos();
            }
            allAvgPos /= _brushStrokeIDs.Count;
            
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
                
                for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[i];
                    Vector2 lastPos = brushStroke.GetLastPos();
                    Vector2 currentPos = brushStroke.GetCurrentPos();
                    Vector2 lastPosDir = (lastPos - allAvgPos);
                    Vector2 currentPosDir = (currentPos - allAvgPos);
                    lastPos = allAvgPos + lastPosDir * _sizeIncrease;
                    currentPos = allAvgPos + currentPosDir * _sizeIncrease;
                    
                    brushStroke.lastPosX = lastPos.x;
                    brushStroke.lastPosY = lastPos.y;
                    brushStroke.currentPosX = currentPos.x;
                    brushStroke.currentPosY = currentPos.y;
                    brushStrokeID.brushStrokes[i] = brushStroke;
                }
                brushStrokeID.RecalculateCollisionBoxAndAvgPos();
            }
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        private void StoppedResizing()
        {
            ICommand resizeCommand = new ResizeCommand(resizeAmount, selectedBrushStrokes);
            EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, resizeCommand);
            resizeAmount = 1;
        }

        private float rotateAmount;
        private void RotateStroke(float _angle)
        {
            if (_angle == 0)
                return;

            rotateAmount += _angle;
            
            Vector2 allAvgPos = Vector2.zero;
            foreach (var brushStrokeID in selectedBrushStrokes)
            {
                allAvgPos += brushStrokeID.GetAvgPos();
            }
            allAvgPos /= selectedBrushStrokes.Count;

            for (int i = 0; i < selectedBrushStrokes.Count; i++)
            {
                selectedBrushStrokes[i].angle += _angle;
                var brushStrokeID = selectedBrushStrokes[i];
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);

                for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[j];
                    Vector2 lastPos = brushStroke.GetLastPos();
                    Vector2 currentPos = brushStroke.GetCurrentPos();
                    Vector2 lastPosDir = (lastPos - allAvgPos);
                    Vector2 currentPosDir = (currentPos - allAvgPos);

                    float cosTheta = Mathf.Cos(_angle);
                    float sinTheta = Mathf.Sin(_angle);

                    float lastPosRotatedX = lastPosDir.x * cosTheta - lastPosDir.y * sinTheta;
                    float lastPosRotatedY = lastPosDir.x * sinTheta + lastPosDir.y * cosTheta;
                    float currentPosRotatedX = currentPosDir.x * cosTheta - currentPosDir.y * sinTheta;
                    float currentPosRotatedY = currentPosDir.x * sinTheta + currentPosDir.y * cosTheta;


                    Vector2 lastPosRotated = new Vector2(lastPosRotatedX, lastPosRotatedY) + allAvgPos;
                    Vector2 currentPosRotated = new Vector2(currentPosRotatedX, currentPosRotatedY) + allAvgPos;

                    brushStroke.lastPosX = lastPosRotated.x;
                    brushStroke.lastPosY = lastPosRotated.y;
                    brushStroke.currentPosX = currentPosRotated.x;
                    brushStroke.currentPosY = currentPosRotated.y;
                    brushStrokeID.brushStrokes[j] = brushStroke;
                }
                brushStrokeID.RecalculateCollisionBoxAndAvgPos();
            }
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(selectedBrushStrokes);
        }
        
        private void RotateStroke(float _angle, List<BrushStrokeID> _brushStrokeIDs)
        {
            if (_angle == 0)
                return;

            Vector2 allAvgPos = Vector2.zero;
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                allAvgPos += brushStrokeID.GetAvgPos();
            }
            allAvgPos /= _brushStrokeIDs.Count;

            for (int i = 0; i < _brushStrokeIDs.Count; i++)
            {
                _brushStrokeIDs[i].angle += _angle;
                var brushStrokeID = _brushStrokeIDs[i];
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);

                for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[j];
                    Vector2 lastPos = brushStroke.GetLastPos();
                    Vector2 currentPos = brushStroke.GetCurrentPos();
                    Vector2 lastPosDir = (lastPos - allAvgPos);
                    Vector2 currentPosDir = (currentPos - allAvgPos);

                    float cosTheta = Mathf.Cos(_angle);
                    float sinTheta = Mathf.Sin(_angle);

                    float lastPosRotatedX = lastPosDir.x * cosTheta - lastPosDir.y * sinTheta;
                    float lastPosRotatedY = lastPosDir.x * sinTheta + lastPosDir.y * cosTheta;
                    float currentPosRotatedX = currentPosDir.x * cosTheta - currentPosDir.y * sinTheta;
                    float currentPosRotatedY = currentPosDir.x * sinTheta + currentPosDir.y * cosTheta;


                    Vector2 lastPosRotated = new Vector2(lastPosRotatedX, lastPosRotatedY) + allAvgPos;
                    Vector2 currentPosRotated = new Vector2(currentPosRotatedX, currentPosRotatedY) + allAvgPos;

                    brushStroke.lastPosX = lastPosRotated.x;
                    brushStroke.lastPosY = lastPosRotated.y;
                    brushStroke.currentPosX = currentPosRotated.x;
                    brushStroke.currentPosY = currentPosRotated.y;
                    brushStrokeID.brushStrokes[j] = brushStroke;
                }
                brushStrokeID.RecalculateCollisionBoxAndAvgPos();
            }
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }

        private void StoppedRotating()
        {
            ICommand rotateCommand = new RotateCommand(rotateAmount, selectedBrushStrokes);
            EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, rotateCommand);
            rotateAmount = 0;
        }

        private void DrawStamp(Vector2 _mousePos, string _key)
        {
            BrushStrokeID brushStrokeID = drawStamp.GetStamp(_key, _mousePos, 256, drawer.brushStrokesID.Count, 
                brushSize, time, Mathf.Clamp01(time + 0.05f));
            EventSystem<float>.RaiseEvent(EventType.ADD_TIME, 0.05f);
            drawer.RedrawStrokeInterpolation(brushStrokeID);
            
            drawer.brushStrokesID.Add(brushStrokeID);
            
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, brushStrokeID);
        }
        public void LoadData(ToolData _data)
        {
            
        }
        public void SaveData(ToolData _data)
        {
            _data.displayImg = drawer.rt.ToBytesPNG(1024, 1024);
        }
    }
}
