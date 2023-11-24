using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DataPersistence.Data;
using Managers;
using UI;
using Undo;
using Unity.VisualScripting;
using UnityEngine;
using Debug = UnityEngine.Debug;
using EventType = Managers.EventType;

namespace Drawing
{
    public class DrawingManager2D : MonoBehaviour, IDataPersistence
    {
        [Header("Materials")]
        [SerializeField] private Material drawingMat;
        [SerializeField] private Material displayMat;
        
        [Header("Canvas Settings")]
        [SerializeField] private int imageWidth = 2048;
        [SerializeField] private int imageHeight = 2048;
    
        [Header("Paint Type")]
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

        private DrawHighlight3D highlighter;
        private DrawPreview3D previewer;
        private DrawStamp drawStamp;
        private float brushSize;
        private int newBrushStrokeID;
        private float cachedTime;
        private float startBrushStrokeTime;
        private Vector3 collisionBoxMin;
        private Vector3 collisionBoxMax;
        private Vector4 resetBox;
        private Vector2 tempAvgPos;
        private float time;

        private void Start()
        {
            drawer = new Drawing(imageWidth, imageHeight);
            highlighter = new DrawHighlight3D(imageWidth, imageHeight);
            previewer = new DrawPreview3D(imageWidth, imageHeight);
            drawStamp = new DrawStamp();
            
            drawingMat.SetTexture("_IDTex", drawer.rtIDs[0]);
            drawingMat.SetTexture("_MainTex", drawer.rts[0]);
            drawingMat.SetTexture("_TempBrushStroke", drawer.rtWholeTemps[0]);
            displayMat.SetTexture("_MainTex", drawer.rts[0]);
            
            drawingMat.SetTexture("_PreviewTex", previewer.AddRTPReview());
            drawingMat.SetTexture("_SelectTex", highlighter.AddRT());

            resetBox = new Vector4(imageWidth, imageHeight, 0, 0);
            tempBrushStrokes = new List<BrushStroke>();
            selectedBrushStrokes = new List<BrushStrokeID>();
            collisionBoxMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            collisionBoxMax = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
        }

        void OnEnable()
        {
            EventSystem.Subscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem.Subscribe(EventType.CLEAR_SELECT, ClearHighlightStroke);
            EventSystem.Subscribe(EventType.STOPPED_SETTING_BRUSH_SIZE, ClearPreview);
            EventSystem<int>.Subscribe(EventType.CHANGE_PAINTTYPE, SetPaintType);
            EventSystem<Vector2>.Subscribe(EventType.DRAW, Draw);
            EventSystem<float>.Subscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Subscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Subscribe(EventType.SELECT_BRUSHSTROKE, SelectBrushStroke);
            EventSystem<float>.Subscribe(EventType.TIME, SetTime);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem.Subscribe(EventType.REDRAW_ALL, RedrawAllStrokes);
            EventSystem<BrushStrokeID>.Subscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<Vector2, int>.Subscribe(EventType.SPAWN_STAMP, DrawStamp);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.SETUP_BRUSHSTROKES, SetupBrushStrokes);
            EventSystem<Vector2>.Subscribe(EventType.MOVE_STROKE, MoveDirStrokes);
            EventSystem.Subscribe(EventType.MOVE_STROKE, StoppedMovingStroke);
            EventSystem<Vector2, List<BrushStrokeID>>.Subscribe(EventType.MOVE_STROKE, MoveDirStrokes);
            EventSystem<List<BrushStrokeID>, Vector2>.Subscribe(EventType.MOVE_STROKE, MovePosStrokes);
            EventSystem<List<BrushStrokeID>, List<Vector2>>.Subscribe(EventType.MOVE_STROKE, MovePosStrokes);
            EventSystem<float, bool>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem.Subscribe(EventType.ROTATE_STROKE, StoppedRotating);
            EventSystem<float, bool, List<BrushStrokeID>>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<List<BrushStrokeID>, float>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<List<BrushStrokeID>, List<float>>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<float, bool>.Subscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            EventSystem.Subscribe(EventType.RESIZE_STROKE, StoppedResizing);
            EventSystem<float, bool, List<BrushStrokeID>>.Subscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            EventSystem<List<BrushStrokeID>, float>.Subscribe(EventType.RESIZE_STROKE, ResizeSetStrokes);
            EventSystem<List<BrushStrokeID>, List<float>>.Subscribe(EventType.RESIZE_STROKE, ResizeSetStrokes);
            EventSystem.Subscribe(EventType.DUPLICATE_STROKE, DuplicateBrushStrokes);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.SELECT_BRUSHSTROKE, HighlightStroke);
            EventSystem<List<BrushStrokeID>, int>.Subscribe(EventType.CHANGE_DRAW_ORDER, ChangeDrawOrder);
            EventSystem<List<BrushStrokeID>, float>.Subscribe(EventType.CHANGE_BRUSH_SIZE, ChangeBrushStrokeBrushSize);
            EventSystem<List<BrushStrokeID>, List<float>>.Subscribe(EventType.CHANGE_BRUSH_SIZE, ChangeBrushStrokeBrushSize);
        }

        private void OnDisable()
        {
            EventSystem.Unsubscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem.Unsubscribe(EventType.CLEAR_SELECT, ClearHighlightStroke);
            EventSystem.Unsubscribe(EventType.STOPPED_SETTING_BRUSH_SIZE, ClearPreview);
            EventSystem<int>.Unsubscribe(EventType.CHANGE_PAINTTYPE, SetPaintType);
            EventSystem<Vector2>.Unsubscribe(EventType.DRAW, Draw);
            EventSystem<float>.Unsubscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Unsubscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Unsubscribe(EventType.SELECT_BRUSHSTROKE, SelectBrushStroke);
            EventSystem<float>.Unsubscribe(EventType.TIME, SetTime);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem.Unsubscribe(EventType.REDRAW_ALL, RedrawAllStrokes);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<Vector2, int>.Unsubscribe(EventType.SPAWN_STAMP, DrawStamp);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.SETUP_BRUSHSTROKES, SetupBrushStrokes);
            EventSystem<Vector2>.Unsubscribe(EventType.MOVE_STROKE, MoveDirStrokes);
            EventSystem<Vector2, List<BrushStrokeID>>.Unsubscribe(EventType.MOVE_STROKE, MoveDirStrokes);
            EventSystem<List<BrushStrokeID>, Vector2>.Unsubscribe(EventType.MOVE_STROKE, MovePosStrokes);
            EventSystem<List<BrushStrokeID>, List<Vector2>>.Unsubscribe(EventType.MOVE_STROKE, MovePosStrokes);
            EventSystem.Unsubscribe(EventType.MOVE_STROKE, StoppedMovingStroke);
            EventSystem<float, bool>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem.Unsubscribe(EventType.ROTATE_STROKE, StoppedRotating);
            EventSystem<float, bool, List<BrushStrokeID>>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<List<BrushStrokeID>, float>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<List<BrushStrokeID>, List<float>>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<float, bool>.Unsubscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            EventSystem.Unsubscribe(EventType.RESIZE_STROKE, StoppedResizing);
            EventSystem<float, bool, List<BrushStrokeID>>.Unsubscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            EventSystem<List<BrushStrokeID>, float>.Unsubscribe(EventType.RESIZE_STROKE, ResizeSetStrokes);
            EventSystem<List<BrushStrokeID>, List<float>>.Unsubscribe(EventType.RESIZE_STROKE, ResizeSetStrokes);
            EventSystem.Unsubscribe(EventType.DUPLICATE_STROKE, DuplicateBrushStrokes);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.SELECT_BRUSHSTROKE, HighlightStroke);
            EventSystem<List<BrushStrokeID>, int>.Unsubscribe(EventType.CHANGE_DRAW_ORDER, ChangeDrawOrder);
            EventSystem<List<BrushStrokeID>, float>.Unsubscribe(EventType.CHANGE_BRUSH_SIZE, ChangeBrushStrokeBrushSize);
            EventSystem<List<BrushStrokeID>, List<float>>.Unsubscribe(EventType.CHANGE_BRUSH_SIZE, ChangeBrushStrokeBrushSize);
        }

        private void SetPaintType(int index)
        {
            paintType = (PaintType)index;
        }

        private void SetTime(float _time)
        {
            time = _time;
            displayMat.SetFloat("_CustomTime", Mathf.Clamp(_time, 0, 0.99f));
        }

        private void SetBrushSize(float _brushSize)
        {
            brushSize = _brushSize;
        }
        private void SetBrushSize(Vector2 _mousePos)
        {
            previewer.DrawPreview(_mousePos, brushSize, time);
        }

        private void ClearPreview()
        {
            previewer.ClearPreview();
        }

        private void Draw(Vector2 _worldPos)
        {
            bool firstDraw = firstUse;

            if (lastCursorPos == _worldPos)
            {
                return;
            }

            if (firstUse)
            {
                startBrushStrokeTime = time;
                //cachedTime = cachedTime > 1 ? 0 : time;
                lastCursorPos = _worldPos;
                firstUse = false;
                newBrushStrokeID = drawer.brushStrokesID.Count;
            }

            collisionBoxMin.x = collisionBoxMin.x > _worldPos.x ? _worldPos.x - brushSize: collisionBoxMin.x;
            collisionBoxMin.y = collisionBoxMin.y > _worldPos.y ? _worldPos.y - brushSize: collisionBoxMin.y;
            collisionBoxMin.z = -float.MaxValue;
            collisionBoxMax.x = collisionBoxMax.x < _worldPos.x ? _worldPos.x + brushSize: collisionBoxMax.x;
            collisionBoxMax.y = collisionBoxMax.y < _worldPos.y ? _worldPos.y + brushSize: collisionBoxMax.y;
            collisionBoxMax.z = float.MaxValue;

            drawer.Draw(lastCursorPos, _worldPos, brushSize, paintType, cachedTime, time, firstDraw, newBrushStrokeID);
            tempBrushStrokes.Add(new BrushStroke(lastCursorPos, _worldPos, brushSize, time, cachedTime));

            tempAvgPos += _worldPos;
            lastCursorPos = _worldPos;
        }
        
        void Update()
        {
            cachedTime = time;

            if (Input.GetKeyDown(KeyCode.J) && selectedBrushStrokes.Count > 0)
            {
                foreach (var brushStrokeID in selectedBrushStrokes)
                {
                    drawer.ReverseBrushStroke(brushStrokeID);
                }
            }
        }

        private void StoppedDrawing()
        {
            (List<BrushStrokePixel[]>, List<uint[]>) result = drawer.FinishDrawing(newBrushStrokeID);
            List<BrushStrokePixel[]> pixels = result.Item1;
            List<uint[]> bounds = result.Item2;
            
            tempAvgPos /= tempBrushStrokes.Count;
            List<BrushStroke> brushStrokes = new List<BrushStroke>(tempBrushStrokes);

            BrushStrokeID brushStrokeID = new BrushStrokeID(
                pixels, brushStrokes, bounds, paintType, brushStrokes[0].startTime, brushStrokes[^1].endTime, collisionBoxMin, collisionBoxMax, drawer.brushStrokesID.Count, tempAvgPos);

            drawer.brushStrokesID.Add(brushStrokeID);
            
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, brushStrokeID);
            
            tempBrushStrokes.Clear();
            tempAvgPos = Vector2.zero;
            collisionBoxMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            collisionBoxMax = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
            firstUse = true;
        }

        private void SetupBrushStrokes(List<BrushStrokeID> _brushStrokeIDs)
        {
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                drawer.brushStrokesID.Add(brushStrokeID);
                drawer.SetupDrawBrushStroke(brushStrokeID);
            }
        }
        private void RedrawStrokes(List<BrushStrokeID> _brushStrokeIDs)
        {
            drawer.RedrawBrushStrokes(_brushStrokeIDs);
        }
        private void RedrawAllStrokes()
        {
            drawer.RedrawBrushStrokes(drawer.brushStrokesID);
        }

        private void AddStroke(BrushStrokeID _brushStrokeID)
        {
            if (_brushStrokeID.indexWhenDrawn >= drawer.brushStrokesID.Count)
            {
                _brushStrokeID.indexWhenDrawn = drawer.brushStrokesID.Count;
                drawer.brushStrokesID.Add(_brushStrokeID);
                drawer.DrawBrushStroke(_brushStrokeID);
            }
            else
            {
                drawer.brushStrokesID.Insert(_brushStrokeID.indexWhenDrawn, _brushStrokeID);
                int count = drawer.brushStrokesID.Count - _brushStrokeID.indexWhenDrawn;
                List<BrushStrokeID> changedIDBrushStrokes = drawer.brushStrokesID.GetRange(_brushStrokeID.indexWhenDrawn, count);
                drawer.UpdateIDTex(changedIDBrushStrokes);
                drawer.DrawBrushStroke(_brushStrokeID);
            }
        }
        
        private void AddStroke(List<BrushStrokeID> _brushStrokeIDs)
        {
            bool ifInserted = false;
            int lowestIndex = int.MaxValue;
            foreach (BrushStrokeID brushStrokeID in _brushStrokeIDs)
            {
                if (brushStrokeID.indexWhenDrawn >= drawer.brushStrokesID.Count)
                {
                    brushStrokeID.indexWhenDrawn = drawer.brushStrokesID.Count;
                    drawer.brushStrokesID.Add(brushStrokeID);
                    drawer.DrawBrushStroke(brushStrokeID);
                }
                else
                {
                    lowestIndex = lowestIndex > brushStrokeID.indexWhenDrawn ? brushStrokeID.indexWhenDrawn : lowestIndex;
                    drawer.brushStrokesID.Insert(brushStrokeID.indexWhenDrawn, brushStrokeID);
                    ifInserted = true;
                }
            }
            
            if (!ifInserted)
            {
                drawer.UpdateIDTex(_brushStrokeIDs);
                return;
            }
            
            int count = drawer.brushStrokesID.Count - lowestIndex;
            List<BrushStrokeID> changedIDBrushStrokes = drawer.brushStrokesID.GetRange(lowestIndex, count);
            drawer.UpdateIDTex(changedIDBrushStrokes);
            
            drawer.DrawBrushStroke(_brushStrokeIDs);
        }

        private void RemoveStroke(BrushStrokeID _brushStrokeID)
        {
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeID);
            List<BrushStrokeID> eraseBrushStrokeIDs = new List<BrushStrokeID>(affected) { _brushStrokeID };
            drawer.EraseBrushStroke(eraseBrushStrokeIDs);
            
            drawer.brushStrokesID.Remove(_brushStrokeID);
            int count = drawer.brushStrokesID.Count - _brushStrokeID.indexWhenDrawn;

            List<BrushStrokeID> changedIDBrushStrokes = drawer.brushStrokesID.GetRange(_brushStrokeID.indexWhenDrawn, count);
            drawer.UpdateIDTex(changedIDBrushStrokes);

            drawer.DrawBrushStroke(affected);
        }
        
        private void RemoveStroke(List<BrushStrokeID> _brushStrokeIDs)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeIDs);

            List<BrushStrokeID> toErase = new List<BrushStrokeID>(_brushStrokeIDs);
            toErase.AddRange(affected);
            drawer.EraseBrushStroke(toErase);
            
            int lowestIndex = int.MaxValue;
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                lowestIndex = lowestIndex > brushStrokeID.indexWhenDrawn ? brushStrokeID.indexWhenDrawn : lowestIndex;
                
                drawer.brushStrokesID.Remove(brushStrokeID);
            }
            
            int count = drawer.brushStrokesID.Count - lowestIndex;

            List<BrushStrokeID> changedIDBrushStrokes = drawer.brushStrokesID.GetRange(lowestIndex, count);
            drawer.UpdateIDTex(changedIDBrushStrokes);

            
            drawer.DrawBrushStroke(affected);
            
            stopwatch.Stop();
            Debug.Log($"time: {stopwatch.ElapsedMilliseconds}");
        }
        
        private void HighlightStroke(BrushStrokeID _brushStrokeID)
        {
            int amountBrushStrokes = drawer.brushStrokesID.Count;
            if (selectedBrushStrokes.Remove(_brushStrokeID))
            {
                highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, amountBrushStrokes);
                return;
            }

            selectedBrushStrokes.Add(_brushStrokeID);
            highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, amountBrushStrokes);
        }
        private void HighlightStroke(List<BrushStrokeID> _brushStrokeIDs)
        {
            int amountBrushStrokes = drawer.brushStrokesID.Count;
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                selectedBrushStrokes.Add(brushStrokeID);
            }
            highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, amountBrushStrokes);
        }
        private void RemoveHighlight(BrushStrokeID _brushStrokeID)
        {
            int amountBrushStrokes = drawer.brushStrokesID.Count;
            selectedBrushStrokes.Remove(_brushStrokeID);
            highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, amountBrushStrokes);
        }
        private void RemoveHighlight(List<BrushStrokeID> _brushStrokeIDs)
        {
            int amountBrushStrokes = drawer.brushStrokesID.Count;
            for (var i = 0; i < _brushStrokeIDs.Count; i++)
            {
                selectedBrushStrokes.Remove(_brushStrokeIDs[i]);
            }

            highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, amountBrushStrokes);
        }
        private void ClearHighlightStroke()
        {
            selectedBrushStrokes.Clear();
            highlighter.ClearHighlight();
        }
        
        private void SelectBrushStroke(Vector2 _mousePos)
        {
            int amountBrushStrokes = drawer.brushStrokesID.Count;
            
            foreach (BrushStrokeID brushStrokeID in drawer.brushStrokesID)
            {
                if (drawer.IsMouseOverBrushStroke(brushStrokeID, _mousePos))
                {
                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        if (selectedBrushStrokes.Remove(brushStrokeID))
                        {
                            highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, amountBrushStrokes);
                            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REMOVE_SELECT, brushStrokeID);
                            return;
                        }
                        selectedBrushStrokes.Add(brushStrokeID);
                        highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, amountBrushStrokes);
                        EventSystem<BrushStrokeID>.RaiseEvent(EventType.SELECT_TIMELINECLIP, brushStrokeID);
                        return;
                    }
                    EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
                    selectedBrushStrokes.Add(brushStrokeID);
                    highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, amountBrushStrokes);
                    EventSystem<BrushStrokeID>.RaiseEvent(EventType.SELECT_TIMELINECLIP, brushStrokeID);
                    
                    return;
                }
            }
            EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
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
            drawer.SetupDrawBrushStroke(duplicateBrushStrokeIDs);
            MoveDirStrokes(new Vector2(10, 10), duplicateBrushStrokeIDs);
            StartCoroutine(HighlightWith1FrameDelay(duplicateBrushStrokeIDs));
            
            EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
            selectedBrushStrokes = duplicateBrushStrokeIDs;
            EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.SELECT_TIMELINECLIP, selectedBrushStrokes);
        }
        
        private IEnumerator HighlightWith1FrameDelay(List<BrushStrokeID> _brushStrokeIDs)
        {
            yield return new WaitForEndOfFrameUnit();
            highlighter.HighlightStroke(_brushStrokeIDs, drawer.rtIDs, drawer.brushStrokesID.Count);
        }

        private Vector2 lastMovePos;
        private bool firstTimeMove;
        private void MoveDirStrokes(Vector2 _dir)
        {
            if (_dir == Vector2.zero)
                return;

            if (firstTimeMove)
            {
                drawer.CachePixelBuffer(selectedBrushStrokes);
                firstTimeMove = false;
            }
        
            EventSystem.RaiseEvent(EventType.UPDATE_CLIP_INFO);
            lastMovePos += _dir;

            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(selectedBrushStrokes);
            List<BrushStrokeID> affectedAll = new List<BrushStrokeID>(affected);
            affectedAll.AddRange(selectedBrushStrokes);
            drawer.EraseBrushStroke(affectedAll);
            
            foreach (var brushStrokeID in selectedBrushStrokes)
            {
                for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[i];
                    brushStroke.startPosX += _dir.x;
                    brushStroke.startPosY += _dir.y;
                    brushStroke.endPosX += _dir.x;
                    brushStroke.endPosY += _dir.y;
                    brushStrokeID.brushStrokes[i] = brushStroke;
                }
                brushStrokeID.avgPosX += _dir.x;
                brushStrokeID.avgPosY += _dir.y;
                brushStrokeID.collisionBoxMinX += _dir.x;
                brushStrokeID.collisionBoxMinY += _dir.y;
                brushStrokeID.collisionBoxMaxX += _dir.x;
                brushStrokeID.collisionBoxMaxY += _dir.y;
                brushStrokeID.RecalculateAvgPos();
            }
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(selectedBrushStrokes, false);
            highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, drawer.brushStrokesID.Count);
        }
        
        private void MoveDirStrokes(Vector2 _dir, List<BrushStrokeID> _brushStrokeIDs)
        {
            if (_dir == Vector2.zero)
                return;
            
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeIDs);
            List<BrushStrokeID> affectedAll = new List<BrushStrokeID>(affected);
            affectedAll.AddRange(_brushStrokeIDs);
            drawer.EraseBrushStroke(affectedAll);
            
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[i];
                    brushStroke.startPosX += _dir.x;
                    brushStroke.startPosY += _dir.y;
                    brushStroke.endPosX += _dir.x;
                    brushStroke.endPosY += _dir.y;
                    brushStrokeID.brushStrokes[i] = brushStroke;
                }
                brushStrokeID.avgPosX += _dir.x;
                brushStrokeID.avgPosY += _dir.y;
                brushStrokeID.collisionBoxMinX += _dir.x;
                brushStrokeID.collisionBoxMinY += _dir.y;
                brushStrokeID.collisionBoxMaxX += _dir.x;
                brushStrokeID.collisionBoxMaxY += _dir.y;
                brushStrokeID.RecalculateAvgPos();
            }
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(_brushStrokeIDs);
        }
        
        private void MovePosStrokes(List<BrushStrokeID> _brushStrokeIDs, Vector2 _pos)
        {
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeIDs);
            List<BrushStrokeID> affectedAll = new List<BrushStrokeID>(affected);
            affectedAll.AddRange(_brushStrokeIDs);
            drawer.EraseBrushStroke(affectedAll);
            
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                if (_pos.x < 0)
                {
                    _pos.x = brushStrokeID.avgPosX;
                }
                if (_pos.y < 0)
                {
                    _pos.y = brushStrokeID.avgPosY;
                }
                Vector2 dir = _pos - brushStrokeID.GetAvgPos();
                
                for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[i];
                    brushStroke.startPosX += dir.x;
                    brushStroke.startPosY += dir.y;
                    brushStroke.endPosX += dir.x;
                    brushStroke.endPosY += dir.y;
                    brushStrokeID.brushStrokes[i] = brushStroke;
                }
                
                brushStrokeID.avgPosX += dir.x;
                brushStrokeID.avgPosY += dir.y;
                brushStrokeID.collisionBoxMinX += dir.x;
                brushStrokeID.collisionBoxMinY += dir.y;
                brushStrokeID.collisionBoxMaxX += dir.x;
                brushStrokeID.collisionBoxMaxY += dir.y;
                brushStrokeID.RecalculateAvgPos();
            }
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(_brushStrokeIDs);
        }
        private void MovePosStrokes(List<BrushStrokeID> _brushStrokeIDs, List<Vector2> _newPositions)
        {
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeIDs);
            List<BrushStrokeID> affectedAll = new List<BrushStrokeID>(affected);
            affectedAll.AddRange(_brushStrokeIDs);
            drawer.EraseBrushStroke(affectedAll);
            
            for (int i = 0; i < _brushStrokeIDs.Count; i++)
            {
                var brushStrokeID = _brushStrokeIDs[i];
                Vector2 pos = _newPositions[i];
                if (pos.x < 0)
                {
                    pos.x = brushStrokeID.avgPosX;
                }
                if (pos.y < 0)
                {
                    pos.y = brushStrokeID.avgPosY;
                }
                Vector2 dir = pos - brushStrokeID.GetAvgPos();
        
                for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[j];
                    brushStroke.startPosX += dir.x;
                    brushStroke.startPosY += dir.y;
                    brushStroke.endPosX += dir.x;
                    brushStroke.endPosY += dir.y;
                    brushStrokeID.brushStrokes[j] = brushStroke;
                }
        
                brushStrokeID.avgPosX += dir.x;
                brushStrokeID.avgPosY += dir.y;
                brushStrokeID.collisionBoxMinX += dir.x;
                brushStrokeID.collisionBoxMinY += dir.y;
                brushStrokeID.collisionBoxMaxX += dir.x;
                brushStrokeID.collisionBoxMaxY += dir.y;
                brushStrokeID.RecalculateAvgPos();
            }
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(_brushStrokeIDs);
        }
        private void StoppedMovingStroke()
        {
            drawer.SetupDrawBrushStroke(selectedBrushStrokes);
            drawer.ClearCachePixelBuffer();
            firstTimeMove = true;
            ICommand moveCommand = new MoveCommand(lastMovePos, new List<BrushStrokeID>(selectedBrushStrokes));
            EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, moveCommand);
            lastMovePos = Vector2.zero;
        }
        
        private float rotateAmount;
        private void RotateStroke(float _angle, bool _center)
        {
            if (_angle == 0)
                return;
            
            if (firstTimeMove)
            {
                drawer.CachePixelBuffer(selectedBrushStrokes);
                firstTimeMove = false;
            }
        
            EventSystem.RaiseEvent(EventType.UPDATE_CLIP_INFO);
            rotateAmount += _angle;
            
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(selectedBrushStrokes);
            List<BrushStrokeID> affectedAll = new List<BrushStrokeID>(affected);
            affectedAll.AddRange(selectedBrushStrokes);
            drawer.EraseBrushStroke(affectedAll);
            
            Vector2 allAvgPos = Vector2.zero;
        
            if (_center)
            {
                foreach (var brushStrokeID in selectedBrushStrokes)
                {
                    allAvgPos += brushStrokeID.GetAvgPos();
                }
                allAvgPos /= selectedBrushStrokes.Count;
            }
        
            foreach (var brushStrokeID in selectedBrushStrokes)
            {
                brushStrokeID.angle += _angle;

                if (!_center)
                {
                    allAvgPos = brushStrokeID.GetAvgPos();
                }
        
                for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[j];
                    Vector2 lastPos = brushStroke.GetStartPos();
                    Vector2 currentPos = brushStroke.GetEndPos();
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
        
                    brushStroke.startPosX = lastPosRotated.x;
                    brushStroke.startPosY = lastPosRotated.y;
                    brushStroke.endPosX = currentPosRotated.x;
                    brushStroke.endPosY = currentPosRotated.y;
                    brushStrokeID.brushStrokes[j] = brushStroke;
                }
                brushStrokeID.RecalculateCollisionBoxAndAvgPos();
            }
            
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(selectedBrushStrokes, false);
            highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, drawer.brushStrokesID.Count);
        }
        
        private void RotateStroke(float _angle, bool _center, List<BrushStrokeID> _brushStrokeIDs)
        {
            if (_angle == 0)
                return;
            
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeIDs);
            List<BrushStrokeID> affectedAll = new List<BrushStrokeID>(affected);
            affectedAll.AddRange(_brushStrokeIDs);
            drawer.EraseBrushStroke(affectedAll);
        
            Vector2 allAvgPos = Vector2.zero;
            if (_center)
            {
                foreach (var brushStrokeID in _brushStrokeIDs)
                {
                    allAvgPos += brushStrokeID.GetAvgPos();
                }
                allAvgPos /= _brushStrokeIDs.Count;
            }
        
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                brushStrokeID.angle += _angle;
                
                if (!_center)
                {
                    allAvgPos = brushStrokeID.GetAvgPos();
                }
        
                for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[j];
                    Vector2 lastPos = brushStroke.GetStartPos();
                    Vector2 currentPos = brushStroke.GetEndPos();
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
        
                    brushStroke.startPosX = lastPosRotated.x;
                    brushStroke.startPosY = lastPosRotated.y;
                    brushStroke.endPosX = currentPosRotated.x;
                    brushStroke.endPosY = currentPosRotated.y;
                    brushStrokeID.brushStrokes[j] = brushStroke;
                }
                brushStrokeID.RecalculateCollisionBoxAndAvgPos();
            }
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(_brushStrokeIDs);
        }
        
        private void RotateStroke(List<BrushStrokeID> _brushStrokeIDs, float _angle)
        {
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeIDs);
            List<BrushStrokeID> affectedAll = new List<BrushStrokeID>(affected);
            affectedAll.AddRange(_brushStrokeIDs);
            drawer.EraseBrushStroke(affectedAll);
            
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                float angleDelta = _angle - brushStrokeID.angle;
                brushStrokeID.angle = _angle;
                Vector2 allAvgPos = brushStrokeID.GetAvgPos();
        
                for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[j];
                    Vector2 lastPos = brushStroke.GetStartPos();
                    Vector2 currentPos = brushStroke.GetEndPos();
                    Vector2 lastPosDir = (lastPos - allAvgPos);
                    Vector2 currentPosDir = (currentPos - allAvgPos);
        
                    float cosTheta = Mathf.Cos(angleDelta);
                    float sinTheta = Mathf.Sin(angleDelta);
        
                    float lastPosRotatedX = lastPosDir.x * cosTheta - lastPosDir.y * sinTheta;
                    float lastPosRotatedY = lastPosDir.x * sinTheta + lastPosDir.y * cosTheta;
                    float currentPosRotatedX = currentPosDir.x * cosTheta - currentPosDir.y * sinTheta;
                    float currentPosRotatedY = currentPosDir.x * sinTheta + currentPosDir.y * cosTheta;
                    
                    Vector2 lastPosRotated = new Vector2(lastPosRotatedX, lastPosRotatedY) + allAvgPos;
                    Vector2 currentPosRotated = new Vector2(currentPosRotatedX, currentPosRotatedY) + allAvgPos;
        
                    brushStroke.startPosX = lastPosRotated.x;
                    brushStroke.startPosY = lastPosRotated.y;
                    brushStroke.endPosX = currentPosRotated.x;
                    brushStroke.endPosY = currentPosRotated.y;
                    brushStrokeID.brushStrokes[j] = brushStroke;
                }
                brushStrokeID.RecalculateCollisionBoxAndAvgPos();
            }
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(_brushStrokeIDs);
        }
        
        private void RotateStroke(List<BrushStrokeID> _brushStrokeIDs, List<float> _angles)
        {
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeIDs);
            List<BrushStrokeID> affectedAll = new List<BrushStrokeID>(affected);
            affectedAll.AddRange(_brushStrokeIDs);
            drawer.EraseBrushStroke(affectedAll);
            
            for (int i = 0; i < _brushStrokeIDs.Count; i++)
            {
                var brushStrokeID = _brushStrokeIDs[i];
                float angleDelta = _angles[i] - brushStrokeID.angle;
                brushStrokeID.angle = _angles[i];
                Vector2 allAvgPos = brushStrokeID.GetAvgPos();
        
                for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[j];
                    Vector2 lastPos = brushStroke.GetStartPos();
                    Vector2 currentPos = brushStroke.GetEndPos();
                    Vector2 lastPosDir = (lastPos - allAvgPos);
                    Vector2 currentPosDir = (currentPos - allAvgPos);
        
                    float cosTheta = Mathf.Cos(angleDelta);
                    float sinTheta = Mathf.Sin(angleDelta);
        
                    float lastPosRotatedX = lastPosDir.x * cosTheta - lastPosDir.y * sinTheta;
                    float lastPosRotatedY = lastPosDir.x * sinTheta + lastPosDir.y * cosTheta;
                    float currentPosRotatedX = currentPosDir.x * cosTheta - currentPosDir.y * sinTheta;
                    float currentPosRotatedY = currentPosDir.x * sinTheta + currentPosDir.y * cosTheta;
        
        
                    Vector2 lastPosRotated = new Vector2(lastPosRotatedX, lastPosRotatedY) + allAvgPos;
                    Vector2 currentPosRotated = new Vector2(currentPosRotatedX, currentPosRotatedY) + allAvgPos;
        
                    brushStroke.startPosX = lastPosRotated.x;
                    brushStroke.startPosY = lastPosRotated.y;
                    brushStroke.endPosX = currentPosRotated.x;
                    brushStroke.endPosY = currentPosRotated.y;
                    brushStrokeID.brushStrokes[j] = brushStroke;
                }
                brushStrokeID.RecalculateCollisionBoxAndAvgPos();
            }
            
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(_brushStrokeIDs);
        }
        
        private void StoppedRotating()
        {
            drawer.SetupDrawBrushStroke(selectedBrushStrokes);
            drawer.ClearCachePixelBuffer();
            firstTimeMove = true;
            ICommand rotateCommand = new RotateCommand(rotateAmount, UIManager.center, new List<BrushStrokeID>(selectedBrushStrokes));
            EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, rotateCommand);
            rotateAmount = 0;
        }
        
        private float resizeAmount = 1;
        private void ResizeStrokes(float _sizeIncrease, bool _center)
        {
            if (Math.Abs(_sizeIncrease - 1) < 0.001f)
                return;
        
            EventSystem.RaiseEvent(EventType.UPDATE_CLIP_INFO);
            resizeAmount -= 1 - _sizeIncrease;
            
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(selectedBrushStrokes);
            List<BrushStrokeID> affectedAll = new List<BrushStrokeID>(affected);
            affectedAll.AddRange(selectedBrushStrokes);
            drawer.EraseBrushStroke(affectedAll);
            
            Vector2 allAvgPos = Vector2.zero;
            if (_center)
            {
                foreach (var brushStrokeID in selectedBrushStrokes)
                {
                    allAvgPos += brushStrokeID.GetAvgPos();
                }
                allAvgPos /= selectedBrushStrokes.Count;
            }
            
            foreach (var brushStrokeID in selectedBrushStrokes)
            {
                brushStrokeID.scale *= _sizeIncrease;
                if (!_center)
                {
                    allAvgPos = brushStrokeID.GetAvgPos();
                }
                
                for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[i];
                    Vector2 lastPos = brushStroke.GetStartPos();
                    Vector2 currentPos = brushStroke.GetEndPos();
                    Vector2 lastPosDir = (lastPos - allAvgPos);
                    Vector2 currentPosDir = (currentPos - allAvgPos);
                    lastPos = allAvgPos + lastPosDir * _sizeIncrease;
                    currentPos = allAvgPos + currentPosDir * _sizeIncrease;
                    
                    brushStroke.startPosX = lastPos.x;
                    brushStroke.startPosY = lastPos.y;
                    brushStroke.endPosX = currentPos.x;
                    brushStroke.endPosY = currentPos.y;
                    brushStrokeID.brushStrokes[i] = brushStroke;
                }
                brushStrokeID.RecalculateCollisionBoxAndAvgPos();
            }
            
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(selectedBrushStrokes, false);
            highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, drawer.brushStrokesID.Count);
        }
        private void ResizeStrokes(float _sizeIncrease, bool _center, List<BrushStrokeID> _brushStrokeIDs)
        {
            if (Math.Abs(_sizeIncrease - 1) < 0.001f)
                return;
            
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeIDs);
            List<BrushStrokeID> affectedAll = new List<BrushStrokeID>(affected);
            affectedAll.AddRange(_brushStrokeIDs);
            drawer.EraseBrushStroke(affectedAll);
        
            Vector2 allAvgPos = Vector2.zero;
            if (_center)
            {
                foreach (var brushStrokeID in _brushStrokeIDs)
                {
                    allAvgPos += brushStrokeID.GetAvgPos();
                }
                allAvgPos /= _brushStrokeIDs.Count;
            }
            
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                brushStrokeID.scale *= _sizeIncrease;
                if (!_center)
                {
                    allAvgPos = brushStrokeID.GetAvgPos();
                }
                
                for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[i];
                    Vector2 lastPos = brushStroke.GetStartPos();
                    Vector2 currentPos = brushStroke.GetEndPos();
                    Vector2 lastPosDir = (lastPos - allAvgPos);
                    Vector2 currentPosDir = (currentPos - allAvgPos);
                    lastPos = allAvgPos + lastPosDir * _sizeIncrease;
                    currentPos = allAvgPos + currentPosDir * _sizeIncrease;
                    
                    brushStroke.startPosX = lastPos.x;
                    brushStroke.startPosY = lastPos.y;
                    brushStroke.endPosX = currentPos.x;
                    brushStroke.endPosY = currentPos.y;
                    brushStrokeID.brushStrokes[i] = brushStroke;
                }
                brushStrokeID.RecalculateCollisionBoxAndAvgPos();
            }
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(_brushStrokeIDs);
        }
        private void ResizeSetStrokes(List<BrushStrokeID> _brushStrokeIDs, float _newSize)
        {
            Vector2 allAvgPos;
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeIDs);
            List<BrushStrokeID> affectedAll = new List<BrushStrokeID>(affected);
            affectedAll.AddRange(_brushStrokeIDs);
            drawer.EraseBrushStroke(affectedAll);
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                float sizeIncrease = _newSize / brushStrokeID.scale;
                brushStrokeID.scale = _newSize;
                allAvgPos = brushStrokeID.GetAvgPos();
                
                for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[i];
                    Vector2 lastPos = brushStroke.GetStartPos();
                    Vector2 currentPos = brushStroke.GetEndPos();
                    Vector2 lastPosDir = (lastPos - allAvgPos);
                    Vector2 currentPosDir = (currentPos - allAvgPos);
                    lastPos = allAvgPos + lastPosDir * sizeIncrease;
                    currentPos = allAvgPos + currentPosDir * sizeIncrease;
                    
                    brushStroke.startPosX = lastPos.x;
                    brushStroke.startPosY = lastPos.y;
                    brushStroke.endPosX = currentPos.x;
                    brushStroke.endPosY = currentPos.y;
                    brushStrokeID.brushStrokes[i] = brushStroke;
                }
                brushStrokeID.RecalculateCollisionBoxAndAvgPos();
            }
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(_brushStrokeIDs);
        }
        private void ResizeSetStrokes(List<BrushStrokeID> _brushStrokeIDs, List<float> _newSize)
        {
            Vector2 allAvgPos;
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeIDs);
            List<BrushStrokeID> affectedAll = new List<BrushStrokeID>(affected);
            affectedAll.AddRange(_brushStrokeIDs);
            drawer.EraseBrushStroke(affectedAll);
            
            for (int i = 0; i < _brushStrokeIDs.Count; i++)
            {
                var brushStrokeID = _brushStrokeIDs[i];
                float sizeIncrease = _newSize[i] / brushStrokeID.scale;
                brushStrokeID.scale = _newSize[i];
                allAvgPos = brushStrokeID.GetAvgPos();
        
                for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
                {
                    var brushStroke = brushStrokeID.brushStrokes[j];
                    Vector2 lastPos = brushStroke.GetStartPos();
                    Vector2 currentPos = brushStroke.GetEndPos();
                    Vector2 lastPosDir = (lastPos - allAvgPos);
                    Vector2 currentPosDir = (currentPos - allAvgPos);
                    lastPos = allAvgPos + lastPosDir * sizeIncrease;
                    currentPos = allAvgPos + currentPosDir * sizeIncrease;
        
                    brushStroke.startPosX = lastPos.x;
                    brushStroke.startPosY = lastPos.y;
                    brushStroke.endPosX = currentPos.x;
                    brushStroke.endPosY = currentPos.y;
                    brushStrokeID.brushStrokes[j] = brushStroke;
                }
                brushStrokeID.RecalculateCollisionBoxAndAvgPos();
            }
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(_brushStrokeIDs);
        }
        private void StoppedResizing()
        {
            drawer.SetupDrawBrushStroke(selectedBrushStrokes);
            drawer.ClearCachePixelBuffer();
            firstTimeMove = true;
            ICommand resizeCommand = new ResizeCommand(1 / resizeAmount, UIManager.center, new List<BrushStrokeID>(selectedBrushStrokes));
            EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, resizeCommand);
            resizeAmount = 1;
        }

        private void ChangeDrawOrder(List<BrushStrokeID> _brushStrokeIDs, int _amount)
        {
            List<int> indexes = _brushStrokeIDs.Select(_brushStrokeID => _brushStrokeID.indexWhenDrawn).ToList();
            indexes.Sort();

            List<BrushStrokeID> movedBrushStrokes = new List<BrushStrokeID>(_brushStrokeIDs);
            int lowestIndex = int.MaxValue;
            if (_amount > 0)
            {
                for (int i = 0; i < indexes.Count; i++)
                {
                    int oldIndex = indexes[i];
                    int newIndex = oldIndex + _amount;
                    newIndex = Mathf.Clamp(newIndex, 0, drawer.brushStrokesID.Count);
                    BrushStrokeID temp = _brushStrokeIDs[i];
                    
                    drawer.brushStrokesID.Remove(temp);
                    if (newIndex >= drawer.brushStrokesID.Count)
                    {
                        Debug.Log($"added instead of inserted");
                        drawer.brushStrokesID.Add(temp);
                    }
                    else
                    {
                        drawer.brushStrokesID.Insert(newIndex, temp);
                    }

                    lowestIndex = lowestIndex > newIndex ? newIndex : lowestIndex;

                    int amountMoved = newIndex - oldIndex;
                    movedBrushStrokes.AddRange(drawer.brushStrokesID.GetRange(oldIndex, amountMoved));
                }

                int count = drawer.brushStrokesID.Count() - lowestIndex;
                Debug.Log($"{lowestIndex}  {drawer.brushStrokesID.Count()}  {count}   {indexes.Count()}");
                List<BrushStrokeID> updateID = drawer.brushStrokesID.GetRange(lowestIndex, count);
                drawer.UpdateIDTex(updateID);
                
                drawer.EraseBrushStroke(movedBrushStrokes);
                drawer.DrawBrushStroke(movedBrushStrokes);
            }
            else
            {
                for (int i = indexes.Count - 1; i >= 0; i--)
                {
                    int oldIndex = indexes[i];
                    int newIndex = oldIndex + _amount;
                    newIndex = Mathf.Clamp(newIndex, 0, drawer.brushStrokesID.Count);
                    BrushStrokeID temp = drawer.brushStrokesID[indexes[i]];
                
                    drawer.brushStrokesID.Remove(temp);
                    drawer.brushStrokesID.Insert(newIndex, temp);
                    
                    lowestIndex = lowestIndex > newIndex ? newIndex : lowestIndex;

                    int amountMoved = oldIndex - newIndex;
                    movedBrushStrokes.AddRange(drawer.brushStrokesID.GetRange(newIndex, amountMoved));
                }
                
                int count = drawer.brushStrokesID.Count() - lowestIndex;
                List<BrushStrokeID> updateID = drawer.brushStrokesID.GetRange(lowestIndex, count);
                drawer.UpdateIDTex(updateID);
                
                drawer.EraseBrushStroke(movedBrushStrokes);
                drawer.DrawBrushStroke(movedBrushStrokes);
            }
            
            highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, drawer.brushStrokesID.Count);
        }
        
        private void ChangeBrushStrokeBrushSize(List<BrushStrokeID> _brushStrokeIDs, float _newBrushSize)
        {
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeIDs);
            
            List<BrushStrokeID> affectedWithBase = new List<BrushStrokeID>(affected);
            affectedWithBase.AddRange(_brushStrokeIDs);
            drawer.EraseBrushStroke(affectedWithBase);
            
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                brushStrokeID.SetBrushSize(_newBrushSize);
            }
            
            drawer.DrawBrushStroke(affected);
            drawer.SetupDrawBrushStroke(_brushStrokeIDs);
            
            highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, drawer.brushStrokesID.Count);
        }
        private void ChangeBrushStrokeBrushSize(List<BrushStrokeID> _brushStrokeIDs, List<float> _newBrushSizes)
        {
            List<BrushStrokeID> affected = drawer.GetOverlappingBrushStrokeID(_brushStrokeIDs);
            List<BrushStrokeID> affectedWithBase = new List<BrushStrokeID>(affected);
            drawer.EraseBrushStroke(affectedWithBase);

            for (int i = 0; i < _newBrushSizes.Count; i++)
            {
                float newBrushSize = _newBrushSizes[i];
                BrushStrokeID brushStrokeID = _brushStrokeIDs[i];
                
                brushStrokeID.SetBrushSize(newBrushSize);
                drawer.DrawBrushStroke(affected);
                drawer.SetupDrawBrushStroke(brushStrokeID);
            }
            
            highlighter.HighlightStroke(selectedBrushStrokes, drawer.rtIDs, drawer.brushStrokesID.Count);
        }
        private void DrawStamp(Vector2 _mousePos, int _sides)
        {
            BrushStrokeID brushStrokeID = drawStamp.GetPolygon(_sides, _mousePos, 256, drawer.brushStrokesID.Count, 
                                                             brushSize, time, time + 0.05f);
            EventSystem<float>.RaiseEvent(EventType.ADD_TIME, 0.05f);
            
            drawer.SetupDrawBrushStroke(brushStrokeID);
            drawer.brushStrokesID.Add(brushStrokeID);
            
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, brushStrokeID);
        }

        public void LoadData(ToolData _data, ToolMetaData _metaData)
        {
            imageWidth = _data.imageWidth;
            imageHeight = _data.imageHeight;
        }
        public void SaveData(ToolData _data, ToolMetaData _metaData)
        {
            _metaData.results.Clear();
            _metaData.results.Add(drawer.rts[0].ToBytesPNG(imageWidth, imageHeight));
            _data.imageWidth = imageWidth;
            _data.imageHeight = imageHeight;
        }
    }
}
