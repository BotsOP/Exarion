﻿using System;
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
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using EventType = Managers.EventType;

namespace Drawing
{
    public class DrawingManager3D : MonoBehaviour, IDataPersistence
    {
        [Header("Materials")]
        [SerializeField] private Shader drawShader;
        [SerializeField] private Shader displayShader;
        [SerializeField] private List<Material> drawingMats;
        [SerializeField] private List<Material> displayMats;
        
        [Header("Canvas Settings")]
        [SerializeField] private int imageWidth = 2048;
        [SerializeField] private int imageHeight = 2048;
    
        [Header("Paint Type")]
        [SerializeField] private PaintType paintType;
        
        [Header("Testing")]
        [SerializeField] private GameObject sphere1;
        [SerializeField] private GameObject sphere2;
        [SerializeField] private Renderer rend;
        [SerializeField] private RenderTexture rtTemp;
        [SerializeField] private RenderTexture rtTemp2;
        [SerializeField] private RenderTexture rtShow;
        [SerializeField] private RenderTexture rtShow2;
        [SerializeField] private RenderTexture rtID;
        [SerializeField] private RenderTexture rtHighlight;
    
        private RenderTexture drawingRenderTexture;
        private int kernelID;
        private Vector2 threadGroupSizeOut;
        private Vector2 threadGroupSize;
        private Vector3 lastCursorPos;
        private bool firstUse = true;
        private List<BrushStrokeID> selectedBrushStrokes;
        private List<BrushStroke> tempBrushStrokes;

        private Drawing3D drawer;
        private DrawHighlight3D highlighter;
        private DrawPreview3D previewer;
        private DrawStamp drawStamp;
        private float brushSize;
        private float newBrushStrokeID;
        private float cachedTime;
        private float startBrushStrokeTime;
        private Vector3 collisionBoxMin;
        private Vector3 collisionBoxMax;
        private Vector2 tempAvgPos;
        private float time;

        private void Start()
        {
            drawer = new Drawing3D(imageWidth, imageHeight, sphere1.transform);
            highlighter = new DrawHighlight3D(imageWidth, imageHeight);
            previewer = new DrawPreview3D(imageWidth, imageHeight);
            drawStamp = new DrawStamp();

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
            EventSystem<Vector3>.Subscribe(EventType.DRAW, Draw);
            EventSystem<float>.Subscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector3>.Subscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector3>.Subscribe(EventType.SELECT_BRUSHSTROKE, SelectBrushStroke);
            EventSystem<float>.Subscribe(EventType.TIME, SetTime);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem.Subscribe(EventType.REDRAW_ALL, RedrawAllStrokes);
            EventSystem<BrushStrokeID>.Subscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.SETUP_BRUSHSTROKES, SetupBrushStrokes);
            // EventSystem<Vector2>.Subscribe(EventType.MOVE_STROKE, MoveDirStrokes);
            // EventSystem<float, bool>.Subscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            // EventSystem<float, bool>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            // EventSystem<Vector2, string>.Subscribe(EventType.SPAWN_STAMP, DrawStamp);
            // EventSystem<Vector2, int>.Subscribe(EventType.SPAWN_STAMP, DrawStamp);
            // EventSystem.Subscribe(EventType.MOVE_STROKE, StoppedMovingStroke);
            // EventSystem.Subscribe(EventType.RESIZE_STROKE, StoppedResizing);
            // EventSystem.Subscribe(EventType.ROTATE_STROKE, StoppedRotating);
            // EventSystem<Vector2, List<BrushStrokeID>>.Subscribe(EventType.MOVE_STROKE, MoveDirStrokes);
            // EventSystem<List<BrushStrokeID>, Vector2>.Subscribe(EventType.MOVE_STROKE, MovePosStrokes);
            // EventSystem<List<BrushStrokeID>, List<Vector2>>.Subscribe(EventType.MOVE_STROKE, MovePosStrokes);
            // EventSystem<float, bool, List<BrushStrokeID>>.Subscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            // EventSystem<List<BrushStrokeID>, float>.Subscribe(EventType.RESIZE_STROKE, ResizeSetStrokes);
            // EventSystem<List<BrushStrokeID>, List<float>>.Subscribe(EventType.RESIZE_STROKE, ResizeSetStrokes);
            // EventSystem<float, bool, List<BrushStrokeID>>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            // EventSystem<List<BrushStrokeID>, float>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            // EventSystem<List<BrushStrokeID>, List<float>>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<List<BrushStrokeID>, int>.Subscribe(EventType.CHANGE_DRAW_ORDER, ChangeDrawOrder);
            EventSystem<List<BrushStrokeID>, float>.Subscribe(EventType.CHANGE_BRUSH_SIZE, ChangeBrushStrokeBrushSize);
            EventSystem<List<BrushStrokeID>, List<float>>.Subscribe(EventType.CHANGE_BRUSH_SIZE, ChangeBrushStrokeBrushSize);
            EventSystem.Subscribe(EventType.DUPLICATE_STROKE, DuplicateBrushStrokes);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.SELECT_BRUSHSTROKE, HighlightStroke);
            EventSystem<Renderer, Renderer>.Subscribe(EventType.CHANGED_MODEL, SetRenderer);
            EventSystem<int, Texture2D>.Subscribe(EventType.IMPORT_MODEL_TEXTURE, UpdateTexture);
        }

        private void OnDisable()
        {
            drawer.OnDestroy();
            
            EventSystem.Unsubscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem.Unsubscribe(EventType.CLEAR_SELECT, ClearHighlightStroke);
            EventSystem.Unsubscribe(EventType.STOPPED_SETTING_BRUSH_SIZE, ClearPreview);
            EventSystem<int>.Unsubscribe(EventType.CHANGE_PAINTTYPE, SetPaintType);
            EventSystem<Vector3>.Unsubscribe(EventType.DRAW, Draw);
            EventSystem<float>.Unsubscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector3>.Unsubscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector3>.Unsubscribe(EventType.SELECT_BRUSHSTROKE, SelectBrushStroke);
            EventSystem<float>.Unsubscribe(EventType.TIME, SetTime);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem.Unsubscribe(EventType.REDRAW_ALL, RedrawAllStrokes);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.SETUP_BRUSHSTROKES, SetupBrushStrokes);
            // EventSystem<Vector2>.Unsubscribe(EventType.MOVE_STROKE, MoveDirStrokes);
            // EventSystem<float, bool>.Unsubscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            // EventSystem<float, bool>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            // EventSystem<Vector2, string>.Unsubscribe(EventType.SPAWN_STAMP, DrawStamp);
            // EventSystem<Vector2, int>.Unsubscribe(EventType.SPAWN_STAMP, DrawStamp);
            // EventSystem.Unsubscribe(EventType.MOVE_STROKE, StoppedMovingStroke);
            // EventSystem.Unsubscribe(EventType.RESIZE_STROKE, StoppedResizing);
            // EventSystem.Unsubscribe(EventType.ROTATE_STROKE, StoppedRotating);
            // EventSystem<Vector2, List<BrushStrokeID>>.Unsubscribe(EventType.MOVE_STROKE, MoveDirStrokes);
            // EventSystem<List<BrushStrokeID>, Vector2>.Unsubscribe(EventType.MOVE_STROKE, MovePosStrokes);
            // EventSystem<List<BrushStrokeID>, List<Vector2>>.Unsubscribe(EventType.MOVE_STROKE, MovePosStrokes);
            // EventSystem<float, bool, List<BrushStrokeID>>.Unsubscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            // EventSystem<List<BrushStrokeID>, float>.Unsubscribe(EventType.RESIZE_STROKE, ResizeSetStrokes);
            // EventSystem<List<BrushStrokeID>, List<float>>.Unsubscribe(EventType.RESIZE_STROKE, ResizeSetStrokes);
            // EventSystem<float, bool, List<BrushStrokeID>>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            // EventSystem<List<BrushStrokeID>, float>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            // EventSystem<List<BrushStrokeID>, List<float>>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<List<BrushStrokeID>, int>.Unsubscribe(EventType.CHANGE_DRAW_ORDER, ChangeDrawOrder);
            EventSystem<List<BrushStrokeID>, float>.Unsubscribe(EventType.CHANGE_BRUSH_SIZE, ChangeBrushStrokeBrushSize);
            EventSystem<List<BrushStrokeID>, List<float>>.Unsubscribe(EventType.CHANGE_BRUSH_SIZE, ChangeBrushStrokeBrushSize);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.SELECT_BRUSHSTROKE, HighlightStroke);
            EventSystem.Unsubscribe(EventType.DUPLICATE_STROKE, DuplicateBrushStrokes);
            EventSystem<Renderer, Renderer>.Unsubscribe(EventType.CHANGED_MODEL, SetRenderer);
            EventSystem<int, Texture2D>.Unsubscribe(EventType.IMPORT_MODEL_TEXTURE, UpdateTexture);
        }

        private void SetRenderer(Renderer _drawingRend, Renderer _displayRend)
        {
            rend = _drawingRend;
            drawer.rend = _drawingRend;
            highlighter.rend = _drawingRend;
            previewer.rend = _drawingRend;

            Mesh mesh = _drawingRend.gameObject.GetComponent<MeshFilter>().sharedMesh;
            EventSystem<int>.RaiseEvent(EventType.UPDATE_SUBMESH_COUNT, mesh.subMeshCount);
            
            drawingMats.Clear();
            displayMats.Clear();
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                Material drawingMat = new Material(drawShader);
                Material displayMat = new Material(displayShader);
                
                drawer.addRTWholeIDTemp();
                CustomRenderTexture drawingTempRT = drawer.addRTWholeTemp();
                CustomRenderTexture drawingRT = drawer.addRT();
                drawingMat.SetTexture("_PreviewTex", previewer.AddRTPReview());
                drawingMat.SetTexture("_IDTex", drawer.addRTID());
                drawingMat.SetTexture("_MainTex", drawingRT);
                drawingMat.SetTexture("_TempBrushStroke", drawingTempRT);
                displayMat.SetTexture("_MainTex", drawingRT);
                drawingMat.SetTexture("_SelectTex", highlighter.AddRT());
                
                drawingMats.Add(drawingMat);
                displayMats.Add(displayMat);
            }
            
            _drawingRend.materials = drawingMats.ToArray();
            _displayRend.materials = displayMats.ToArray();

            rtTemp = drawer.rtWholeTemps[0];
            rtShow = drawer.rts[0];
            rtID = drawer.rtIDs[0];
            rtHighlight = highlighter.rtHighlights[0];
        }

        public List<CustomRenderTexture> GetTextures()
        {
            return drawer.rts;
        }

        private void UpdateTexture(int _subMesh, Texture2D _texture)
        {
            if (_subMesh >= drawingMats.Count)
            {
                Debug.LogError($"given submesh {_subMesh} is bigger than amount mats {drawingMats.Count - 1}");
                return;
            }
            
            drawingMats[_subMesh].SetTexture("_Mask", _texture);
            drawingMats[_subMesh].SetInt("_UseMaskTex", 1);
            displayMats[_subMesh].SetTexture("_Mask", _texture);
            displayMats[_subMesh].SetInt("_UseMaskTex", 1);
        }

        private void SetPaintType(int index)
        {
            paintType = (PaintType)index;
        }

        private void SetTime(float _time)
        {
            time = _time;
            foreach (var drawingMat in displayMats)
            {
                drawingMat.SetFloat("_CustomTime", Mathf.Clamp(_time, 0, 1f));
            }
        }

        private void SetBrushSize(float _brushSize)
        {
            brushSize = _brushSize;
        }
        private void SetBrushSize(Vector3 _worldPos)
        {
            sphere1.transform.position = _worldPos;
            previewer.DrawPreview(_worldPos, brushSize, time);
        }
        private void ClearPreview()
        {
            previewer.ClearPreview();
        }

        private void Draw(Vector3 _worldPos)
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
            collisionBoxMin.z = collisionBoxMin.z > _worldPos.z ? _worldPos.z - brushSize: collisionBoxMin.z;
            collisionBoxMax.x = collisionBoxMax.x < _worldPos.x ? _worldPos.x + brushSize: collisionBoxMax.x;
            collisionBoxMax.y = collisionBoxMax.y < _worldPos.y ? _worldPos.y + brushSize: collisionBoxMax.y;
            collisionBoxMax.z = collisionBoxMax.z < _worldPos.z ? _worldPos.z + brushSize: collisionBoxMax.z;

            drawer.Draw(lastCursorPos, _worldPos, brushSize, paintType, cachedTime, time, firstDraw, newBrushStrokeID);
            tempBrushStrokes.Add(new BrushStroke(lastCursorPos, _worldPos, brushSize, time, cachedTime));
            
            lastCursorPos = _worldPos;
        }
        
        void Update()
        {
            cachedTime = time;
        }

        private void StoppedDrawing()
        {
            (List<BrushStrokePixel[]>, List<uint[]>) result = drawer.FinishDrawing();
            List<BrushStrokePixel[]> pixels = result.Item1;
            List<uint[]> bounds = result.Item2;
            
            tempAvgPos /= tempBrushStrokes.Count;
            List<BrushStroke> brushStrokes = new List<BrushStroke>(tempBrushStrokes);
            
            BrushStrokeID brushStrokeID = new BrushStrokeID(
                pixels, brushStrokes, bounds, paintType, brushStrokes[0].startTime, brushStrokes[^1].endTime, collisionBoxMin, collisionBoxMax, drawer.brushStrokesID.Count, tempAvgPos);

            sphere1.transform.position = collisionBoxMin;
            sphere2.transform.position = collisionBoxMax;
            
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
        
        private void SelectBrushStroke(Vector3 _worldPos)
        {
            int amountBrushStrokes = drawer.brushStrokesID.Count;

            foreach (BrushStrokeID brushStrokeID in drawer.brushStrokesID)
            {
                if (drawer.IsMouseOverBrushStroke(brushStrokeID, _worldPos))
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
            // List<BrushStrokeID> duplicateBrushStrokeIDs = new List<BrushStrokeID>();
            // foreach (var brushStrokeID in selectedBrushStrokes)
            // {
            //     BrushStrokeID duplicateBrushStrokeID = new BrushStrokeID(brushStrokeID, drawer.brushStrokesID.Count);
            //     
            //     drawer.brushStrokesID.Add(duplicateBrushStrokeID);
            //     EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, duplicateBrushStrokeID);
            //     
            //     duplicateBrushStrokeIDs.Add(duplicateBrushStrokeID);
            // }
            //
            // //MoveDirStrokes(new Vector2(10, 10), duplicateBrushStrokeIDs);
            // EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
            // selectedBrushStrokes = duplicateBrushStrokeIDs;
            // EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.SELECT_TIMELINECLIP, selectedBrushStrokes);
        }
        
        // #region manipulatingBrushStrokes
        //
        // private Vector2 lastMovePos;
        // private void MoveDirStrokes(Vector2 _dir)
        // {
        //     if (_dir == Vector2.zero)
        //         return;
        //
        //     EventSystem.RaiseEvent(EventType.UPDATE_CLIP_INFO);
        //     lastMovePos += _dir;
        //     
        //     foreach (var brushStrokeID in selectedBrushStrokes)
        //     {
        //         drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
        //         for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
        //         {
        //             var brushStroke = brushStrokeID.brushStrokes[i];
        //             brushStroke.startPosX += _dir.x;
        //             brushStroke.startPosY += _dir.y;
        //             brushStroke.endPosX += _dir.x;
        //             brushStroke.endPosY += _dir.y;
        //             brushStrokeID.brushStrokes[i] = brushStroke;
        //         }
        //         brushStrokeID.avgPosX += _dir.x;
        //         brushStrokeID.avgPosY += _dir.y;
        //         brushStrokeID.collisionBoxMinX += _dir.x;
        //         brushStrokeID.collisionBoxMinY += _dir.y;
        //         brushStrokeID.collisionBoxMaxX += _dir.x;
        //         brushStrokeID.collisionBoxMaxY += _dir.y;
        //         brushStrokeID.RecalculateAvgPos();
        //     }
        //     highlighter.HighlightStroke(selectedBrushStrokes);
        //     drawer.RedrawAllSafe(selectedBrushStrokes);
        // }
        //
        // private void MoveDirStrokes(Vector2 _dir, List<BrushStrokeID> _brushStrokeIDs)
        // {
        //     if (_dir == Vector2.zero)
        //         return;
        //     
        //     foreach (var brushStrokeID in _brushStrokeIDs)
        //     {
        //         drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
        //         for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
        //         {
        //             var brushStroke = brushStrokeID.brushStrokes[i];
        //             brushStroke.startPosX += _dir.x;
        //             brushStroke.startPosY += _dir.y;
        //             brushStroke.endPosX += _dir.x;
        //             brushStroke.endPosY += _dir.y;
        //             brushStrokeID.brushStrokes[i] = brushStroke;
        //         }
        //         brushStrokeID.avgPosX += _dir.x;
        //         brushStrokeID.avgPosY += _dir.y;
        //         brushStrokeID.collisionBoxMinX += _dir.x;
        //         brushStrokeID.collisionBoxMinY += _dir.y;
        //         brushStrokeID.collisionBoxMaxX += _dir.x;
        //         brushStrokeID.collisionBoxMaxY += _dir.y;
        //         brushStrokeID.RecalculateAvgPos();
        //     }
        //     highlighter.HighlightStroke(selectedBrushStrokes);
        //     drawer.RedrawAllSafe(_brushStrokeIDs);
        // }
        // private void MovePosStrokes(List<BrushStrokeID> _brushStrokeIDs, Vector2 _pos)
        // {
        //     foreach (var brushStrokeID in _brushStrokeIDs)
        //     {
        //         drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
        //         if (_pos.x < 0)
        //         {
        //             _pos.x = brushStrokeID.avgPosX;
        //         }
        //         if (_pos.y < 0)
        //         {
        //             _pos.y = brushStrokeID.avgPosY;
        //         }
        //         Vector2 dir = _pos - brushStrokeID.GetAvgPos();
        //         
        //         for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
        //         {
        //             var brushStroke = brushStrokeID.brushStrokes[i];
        //             brushStroke.startPosX += dir.x;
        //             brushStroke.startPosY += dir.y;
        //             brushStroke.endPosX += dir.x;
        //             brushStroke.endPosY += dir.y;
        //             brushStrokeID.brushStrokes[i] = brushStroke;
        //         }
        //         
        //         brushStrokeID.avgPosX += dir.x;
        //         brushStrokeID.avgPosY += dir.y;
        //         brushStrokeID.collisionBoxMinX += dir.x;
        //         brushStrokeID.collisionBoxMinY += dir.y;
        //         brushStrokeID.collisionBoxMaxX += dir.x;
        //         brushStrokeID.collisionBoxMaxY += dir.y;
        //         brushStrokeID.RecalculateAvgPos();
        //     }
        //     highlighter.HighlightStroke(selectedBrushStrokes);
        //     drawer.RedrawAllSafe(_brushStrokeIDs);
        // }
        // private void MovePosStrokes(List<BrushStrokeID> _brushStrokeIDs, List<Vector2> _newPositions)
        // {
        //     for (int i = 0; i < _brushStrokeIDs.Count; i++)
        //     {
        //         var brushStrokeID = _brushStrokeIDs[i];
        //         Vector2 pos = _newPositions[i];
        //         drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
        //         if (pos.x < 0)
        //         {
        //             pos.x = brushStrokeID.avgPosX;
        //         }
        //         if (pos.y < 0)
        //         {
        //             pos.y = brushStrokeID.avgPosY;
        //         }
        //         Vector2 dir = pos - brushStrokeID.GetAvgPos();
        //
        //         for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
        //         {
        //             var brushStroke = brushStrokeID.brushStrokes[j];
        //             brushStroke.startPosX += dir.x;
        //             brushStroke.startPosY += dir.y;
        //             brushStroke.endPosX += dir.x;
        //             brushStroke.endPosY += dir.y;
        //             brushStrokeID.brushStrokes[j] = brushStroke;
        //         }
        //
        //         brushStrokeID.avgPosX += dir.x;
        //         brushStrokeID.avgPosY += dir.y;
        //         brushStrokeID.collisionBoxMinX += dir.x;
        //         brushStrokeID.collisionBoxMinY += dir.y;
        //         brushStrokeID.collisionBoxMaxX += dir.x;
        //         brushStrokeID.collisionBoxMaxY += dir.y;
        //         brushStrokeID.RecalculateAvgPos();
        //     }
        //     highlighter.HighlightStroke(selectedBrushStrokes);
        //     drawer.RedrawAllSafe(_brushStrokeIDs);
        // }
        // private void StoppedMovingStroke()
        // {
        //     ICommand moveCommand = new MoveCommand(lastMovePos, selectedBrushStrokes);
        //     EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, moveCommand);
        //     lastMovePos = Vector2.zero;
        // }
        //
        // private float rotateAmount;
        // private void RotateStroke(float _angle, bool _center)
        // {
        //     if (_angle == 0)
        //         return;
        //
        //     EventSystem.RaiseEvent(EventType.UPDATE_CLIP_INFO);
        //     rotateAmount += _angle;
        //     
        //     Vector2 allAvgPos = Vector2.zero;
        //
        //     if (_center)
        //     {
        //         foreach (var brushStrokeID in selectedBrushStrokes)
        //         {
        //             allAvgPos += brushStrokeID.GetAvgPos();
        //         }
        //         allAvgPos /= selectedBrushStrokes.Count;
        //     }
        //
        //     foreach (var brushStrokeID in selectedBrushStrokes)
        //     {
        //         brushStrokeID.angle += _angle;
        //         drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
        //         
        //         if (!_center)
        //         {
        //             allAvgPos = brushStrokeID.GetAvgPos();
        //         }
        //
        //         for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
        //         {
        //             var brushStroke = brushStrokeID.brushStrokes[j];
        //             Vector2 lastPos = brushStroke.GetStartPos();
        //             Vector2 currentPos = brushStroke.GetEndPos();
        //             Vector2 lastPosDir = (lastPos - allAvgPos);
        //             Vector2 currentPosDir = (currentPos - allAvgPos);
        //
        //             float cosTheta = Mathf.Cos(_angle);
        //             float sinTheta = Mathf.Sin(_angle);
        //
        //             float lastPosRotatedX = lastPosDir.x * cosTheta - lastPosDir.y * sinTheta;
        //             float lastPosRotatedY = lastPosDir.x * sinTheta + lastPosDir.y * cosTheta;
        //             float currentPosRotatedX = currentPosDir.x * cosTheta - currentPosDir.y * sinTheta;
        //             float currentPosRotatedY = currentPosDir.x * sinTheta + currentPosDir.y * cosTheta;
        //
        //
        //             Vector2 lastPosRotated = new Vector2(lastPosRotatedX, lastPosRotatedY) + allAvgPos;
        //             Vector2 currentPosRotated = new Vector2(currentPosRotatedX, currentPosRotatedY) + allAvgPos;
        //
        //             brushStroke.startPosX = lastPosRotated.x;
        //             brushStroke.startPosY = lastPosRotated.y;
        //             brushStroke.endPosX = currentPosRotated.x;
        //             brushStroke.endPosY = currentPosRotated.y;
        //             brushStrokeID.brushStrokes[j] = brushStroke;
        //         }
        //         brushStrokeID.RecalculateCollisionBoxAndAvgPos();
        //     }
        //     highlighter.HighlightStroke(selectedBrushStrokes);
        //     drawer.RedrawAllSafe(selectedBrushStrokes);
        // }
        //
        // private void RotateStroke(float _angle, bool _center, List<BrushStrokeID> _brushStrokeIDs)
        // {
        //     if (_angle == 0)
        //         return;
        //
        //     Vector2 allAvgPos = Vector2.zero;
        //     if (_center)
        //     {
        //         foreach (var brushStrokeID in _brushStrokeIDs)
        //         {
        //             allAvgPos += brushStrokeID.GetAvgPos();
        //         }
        //         allAvgPos /= _brushStrokeIDs.Count;
        //     }
        //
        //     foreach (var brushStrokeID in _brushStrokeIDs)
        //     {
        //         brushStrokeID.angle += _angle;
        //         drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
        //         
        //         if (!_center)
        //         {
        //             allAvgPos = brushStrokeID.GetAvgPos();
        //         }
        //
        //         for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
        //         {
        //             var brushStroke = brushStrokeID.brushStrokes[j];
        //             Vector2 lastPos = brushStroke.GetStartPos();
        //             Vector2 currentPos = brushStroke.GetEndPos();
        //             Vector2 lastPosDir = (lastPos - allAvgPos);
        //             Vector2 currentPosDir = (currentPos - allAvgPos);
        //
        //             float cosTheta = Mathf.Cos(_angle);
        //             float sinTheta = Mathf.Sin(_angle);
        //
        //             float lastPosRotatedX = lastPosDir.x * cosTheta - lastPosDir.y * sinTheta;
        //             float lastPosRotatedY = lastPosDir.x * sinTheta + lastPosDir.y * cosTheta;
        //             float currentPosRotatedX = currentPosDir.x * cosTheta - currentPosDir.y * sinTheta;
        //             float currentPosRotatedY = currentPosDir.x * sinTheta + currentPosDir.y * cosTheta;
        //
        //
        //             Vector2 lastPosRotated = new Vector2(lastPosRotatedX, lastPosRotatedY) + allAvgPos;
        //             Vector2 currentPosRotated = new Vector2(currentPosRotatedX, currentPosRotatedY) + allAvgPos;
        //
        //             brushStroke.startPosX = lastPosRotated.x;
        //             brushStroke.startPosY = lastPosRotated.y;
        //             brushStroke.endPosX = currentPosRotated.x;
        //             brushStroke.endPosY = currentPosRotated.y;
        //             brushStrokeID.brushStrokes[j] = brushStroke;
        //         }
        //         brushStrokeID.RecalculateCollisionBoxAndAvgPos();
        //     }
        //     highlighter.HighlightStroke(selectedBrushStrokes);
        //     drawer.RedrawAllSafe(_brushStrokeIDs);
        // }
        //
        // private void RotateStroke(List<BrushStrokeID> _brushStrokeIDs, float _angle)
        // {
        //     foreach (var brushStrokeID in _brushStrokeIDs)
        //     {
        //         float angleDelta = _angle - brushStrokeID.angle;
        //         brushStrokeID.angle = _angle;
        //         drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
        //         Vector2 allAvgPos = brushStrokeID.GetAvgPos();
        //
        //         for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
        //         {
        //             var brushStroke = brushStrokeID.brushStrokes[j];
        //             Vector2 lastPos = brushStroke.GetStartPos();
        //             Vector2 currentPos = brushStroke.GetEndPos();
        //             Vector2 lastPosDir = (lastPos - allAvgPos);
        //             Vector2 currentPosDir = (currentPos - allAvgPos);
        //
        //             float cosTheta = Mathf.Cos(angleDelta);
        //             float sinTheta = Mathf.Sin(angleDelta);
        //
        //             float lastPosRotatedX = lastPosDir.x * cosTheta - lastPosDir.y * sinTheta;
        //             float lastPosRotatedY = lastPosDir.x * sinTheta + lastPosDir.y * cosTheta;
        //             float currentPosRotatedX = currentPosDir.x * cosTheta - currentPosDir.y * sinTheta;
        //             float currentPosRotatedY = currentPosDir.x * sinTheta + currentPosDir.y * cosTheta;
        //
        //
        //             Vector2 lastPosRotated = new Vector2(lastPosRotatedX, lastPosRotatedY) + allAvgPos;
        //             Vector2 currentPosRotated = new Vector2(currentPosRotatedX, currentPosRotatedY) + allAvgPos;
        //
        //             brushStroke.startPosX = lastPosRotated.x;
        //             brushStroke.startPosY = lastPosRotated.y;
        //             brushStroke.endPosX = currentPosRotated.x;
        //             brushStroke.endPosY = currentPosRotated.y;
        //             brushStrokeID.brushStrokes[j] = brushStroke;
        //         }
        //         brushStrokeID.RecalculateCollisionBoxAndAvgPos();
        //     }
        //     highlighter.HighlightStroke(selectedBrushStrokes);
        //     drawer.RedrawAllSafe(_brushStrokeIDs);
        // }
        //
        // private void RotateStroke(List<BrushStrokeID> _brushStrokeIDs, List<float> _angles)
        // {
        //     for (int i = 0; i < _brushStrokeIDs.Count; i++)
        //     {
        //         var brushStrokeID = _brushStrokeIDs[i];
        //         float angleDelta = _angles[i] - brushStrokeID.angle;
        //         brushStrokeID.angle = _angles[i];
        //         drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
        //         Vector2 allAvgPos = brushStrokeID.GetAvgPos();
        //
        //         for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
        //         {
        //             var brushStroke = brushStrokeID.brushStrokes[j];
        //             Vector2 lastPos = brushStroke.GetStartPos();
        //             Vector2 currentPos = brushStroke.GetEndPos();
        //             Vector2 lastPosDir = (lastPos - allAvgPos);
        //             Vector2 currentPosDir = (currentPos - allAvgPos);
        //
        //             float cosTheta = Mathf.Cos(angleDelta);
        //             float sinTheta = Mathf.Sin(angleDelta);
        //
        //             float lastPosRotatedX = lastPosDir.x * cosTheta - lastPosDir.y * sinTheta;
        //             float lastPosRotatedY = lastPosDir.x * sinTheta + lastPosDir.y * cosTheta;
        //             float currentPosRotatedX = currentPosDir.x * cosTheta - currentPosDir.y * sinTheta;
        //             float currentPosRotatedY = currentPosDir.x * sinTheta + currentPosDir.y * cosTheta;
        //
        //
        //             Vector2 lastPosRotated = new Vector2(lastPosRotatedX, lastPosRotatedY) + allAvgPos;
        //             Vector2 currentPosRotated = new Vector2(currentPosRotatedX, currentPosRotatedY) + allAvgPos;
        //
        //             brushStroke.startPosX = lastPosRotated.x;
        //             brushStroke.startPosY = lastPosRotated.y;
        //             brushStroke.endPosX = currentPosRotated.x;
        //             brushStroke.endPosY = currentPosRotated.y;
        //             brushStrokeID.brushStrokes[j] = brushStroke;
        //         }
        //         brushStrokeID.RecalculateCollisionBoxAndAvgPos();
        //     }
        //     highlighter.HighlightStroke(selectedBrushStrokes);
        //     drawer.RedrawAllSafe(_brushStrokeIDs);
        // }
        //
        // private void StoppedRotating()
        // {
        //     ICommand rotateCommand = new RotateCommand(rotateAmount, UIManager.center, selectedBrushStrokes);
        //     EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, rotateCommand);
        //     rotateAmount = 0;
        // }
        //
        // private float resizeAmount = 1;
        // private void ResizeStrokes(float _sizeIncrease, bool _center)
        // {
        //     if (Math.Abs(_sizeIncrease - 1) < 0.001f)
        //         return;
        //
        //     EventSystem.RaiseEvent(EventType.UPDATE_CLIP_INFO);
        //     resizeAmount -= 1 - _sizeIncrease;
        //     
        //     Vector2 allAvgPos = Vector2.zero;
        //     if (_center)
        //     {
        //         foreach (var brushStrokeID in selectedBrushStrokes)
        //         {
        //             allAvgPos += brushStrokeID.GetAvgPos();
        //         }
        //         allAvgPos /= selectedBrushStrokes.Count;
        //     }
        //     
        //     foreach (var brushStrokeID in selectedBrushStrokes)
        //     {
        //         brushStrokeID.scale *= _sizeIncrease;
        //         drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
        //         if (!_center)
        //         {
        //             allAvgPos = brushStrokeID.GetAvgPos();
        //         }
        //         
        //         for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
        //         {
        //             var brushStroke = brushStrokeID.brushStrokes[i];
        //             Vector2 lastPos = brushStroke.GetStartPos();
        //             Vector2 currentPos = brushStroke.GetEndPos();
        //             Vector2 lastPosDir = (lastPos - allAvgPos);
        //             Vector2 currentPosDir = (currentPos - allAvgPos);
        //             lastPos = allAvgPos + lastPosDir * _sizeIncrease;
        //             currentPos = allAvgPos + currentPosDir * _sizeIncrease;
        //             
        //             brushStroke.startPosX = lastPos.x;
        //             brushStroke.startPosY = lastPos.y;
        //             brushStroke.endPosX = currentPos.x;
        //             brushStroke.endPosY = currentPos.y;
        //             brushStrokeID.brushStrokes[i] = brushStroke;
        //         }
        //         brushStrokeID.RecalculateCollisionBoxAndAvgPos();
        //     }
        //     
        //     highlighter.HighlightStroke(selectedBrushStrokes);
        //     drawer.RedrawAllSafe(selectedBrushStrokes);
        // }
        // private void ResizeStrokes(float _sizeIncrease, bool _center, List<BrushStrokeID> _brushStrokeIDs)
        // {
        //     if (Math.Abs(_sizeIncrease - 1) < 0.001f)
        //         return;
        //
        //     Vector2 allAvgPos = Vector2.zero;
        //     if (_center)
        //     {
        //         foreach (var brushStrokeID in _brushStrokeIDs)
        //         {
        //             allAvgPos += brushStrokeID.GetAvgPos();
        //         }
        //         allAvgPos /= _brushStrokeIDs.Count;
        //     }
        //     
        //     foreach (var brushStrokeID in _brushStrokeIDs)
        //     {
        //         brushStrokeID.scale *= _sizeIncrease;
        //         drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
        //         if (!_center)
        //         {
        //             allAvgPos = brushStrokeID.GetAvgPos();
        //         }
        //         
        //         for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
        //         {
        //             var brushStroke = brushStrokeID.brushStrokes[i];
        //             Vector2 lastPos = brushStroke.GetStartPos();
        //             Vector2 currentPos = brushStroke.GetEndPos();
        //             Vector2 lastPosDir = (lastPos - allAvgPos);
        //             Vector2 currentPosDir = (currentPos - allAvgPos);
        //             lastPos = allAvgPos + lastPosDir * _sizeIncrease;
        //             currentPos = allAvgPos + currentPosDir * _sizeIncrease;
        //             
        //             brushStroke.startPosX = lastPos.x;
        //             brushStroke.startPosY = lastPos.y;
        //             brushStroke.endPosX = currentPos.x;
        //             brushStroke.endPosY = currentPos.y;
        //             brushStrokeID.brushStrokes[i] = brushStroke;
        //         }
        //         brushStrokeID.RecalculateCollisionBoxAndAvgPos();
        //     }
        //     highlighter.HighlightStroke(selectedBrushStrokes);
        //     drawer.RedrawAllSafe(_brushStrokeIDs);
        // }
        // private void ResizeSetStrokes(List<BrushStrokeID> _brushStrokeIDs, float _newSize)
        // {
        //     Vector2 allAvgPos;
        //     foreach (var brushStrokeID in _brushStrokeIDs)
        //     {
        //         float sizeIncrease = _newSize / brushStrokeID.scale;
        //         brushStrokeID.scale = _newSize;
        //         drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
        //         allAvgPos = brushStrokeID.GetAvgPos();
        //         
        //         for (int i = 0; i < brushStrokeID.brushStrokes.Count; i++)
        //         {
        //             var brushStroke = brushStrokeID.brushStrokes[i];
        //             Vector2 lastPos = brushStroke.GetStartPos();
        //             Vector2 currentPos = brushStroke.GetEndPos();
        //             Vector2 lastPosDir = (lastPos - allAvgPos);
        //             Vector2 currentPosDir = (currentPos - allAvgPos);
        //             lastPos = allAvgPos + lastPosDir * sizeIncrease;
        //             currentPos = allAvgPos + currentPosDir * sizeIncrease;
        //             
        //             brushStroke.startPosX = lastPos.x;
        //             brushStroke.startPosY = lastPos.y;
        //             brushStroke.endPosX = currentPos.x;
        //             brushStroke.endPosY = currentPos.y;
        //             brushStrokeID.brushStrokes[i] = brushStroke;
        //         }
        //         brushStrokeID.RecalculateCollisionBoxAndAvgPos();
        //     }
        //     highlighter.HighlightStroke(selectedBrushStrokes);
        //     drawer.RedrawAllSafe(_brushStrokeIDs);
        // }
        // private void ResizeSetStrokes(List<BrushStrokeID> _brushStrokeIDs, List<float> _newSize)
        // {
        //     Vector2 allAvgPos;
        //     for (int i = 0; i < _brushStrokeIDs.Count; i++)
        //     {
        //         var brushStrokeID = _brushStrokeIDs[i];
        //         float sizeIncrease = _newSize[i] / brushStrokeID.scale;
        //         brushStrokeID.scale = _newSize[i];
        //         drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
        //         allAvgPos = brushStrokeID.GetAvgPos();
        //
        //         for (int j = 0; j < brushStrokeID.brushStrokes.Count; j++)
        //         {
        //             var brushStroke = brushStrokeID.brushStrokes[j];
        //             Vector2 lastPos = brushStroke.GetStartPos();
        //             Vector2 currentPos = brushStroke.GetEndPos();
        //             Vector2 lastPosDir = (lastPos - allAvgPos);
        //             Vector2 currentPosDir = (currentPos - allAvgPos);
        //             lastPos = allAvgPos + lastPosDir * sizeIncrease;
        //             currentPos = allAvgPos + currentPosDir * sizeIncrease;
        //
        //             brushStroke.startPosX = lastPos.x;
        //             brushStroke.startPosY = lastPos.y;
        //             brushStroke.endPosX = currentPos.x;
        //             brushStroke.endPosY = currentPos.y;
        //             brushStrokeID.brushStrokes[j] = brushStroke;
        //         }
        //         brushStrokeID.RecalculateCollisionBoxAndAvgPos();
        //     }
        //     highlighter.HighlightStroke(selectedBrushStrokes);
        //     drawer.RedrawAllSafe(_brushStrokeIDs);
        // }
        // private void StoppedResizing()
        // {
        //     ICommand resizeCommand = new ResizeCommand(resizeAmount, UIManager.center, selectedBrushStrokes);
        //     EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, resizeCommand);
        //     resizeAmount = 1;
        // }
        // #endregion
        
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

        // private void DrawStamp(Vector2 _mousePos, string _key)
        // {
        //     BrushStrokeID brushStrokeID = drawStamp.GetStamp(_key, _mousePos, 256, drawer.brushStrokesID.Count, 
        //         brushSize, time, Mathf.Clamp01(time + 0.05f));
        //     EventSystem<float>.RaiseEvent(EventType.ADD_TIME, 0.05f);
        //     drawer.RedrawStrokeInterpolation(brushStrokeID);
        //     
        //     drawer.brushStrokesID.Add(brushStrokeID);
        //     
        //     EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, brushStrokeID);
        // }
        // private void DrawStamp(Vector2 _mousePos, int _sides)
        // {
        //     BrushStrokeID brushStrokeID = drawStamp.GetPolygon(_sides, _mousePos, 256, drawer.brushStrokesID.Count, 
        //                                                      brushSize, time, Mathf.Clamp01(time + 0.05f));
        //     EventSystem<float>.RaiseEvent(EventType.ADD_TIME, 0.05f);
        //     drawer.RedrawStrokeInterpolation(brushStrokeID);
        //     
        //     drawer.brushStrokesID.Add(brushStrokeID);
        //     
        //     EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, brushStrokeID);
        // }

        public void LoadData(ToolData _data, ToolMetaData _metaData)
        {
            imageWidth = _data.imageWidth;
            imageHeight = _data.imageHeight;
        }
        public void SaveData(ToolData _data, ToolMetaData _metaData)
        {
            _metaData.results.Clear();
            foreach (var rt in drawer.rts)
            {
                _metaData.results.Add(rt.ToBytesPNG());
            }
            _data.imageWidth = imageWidth;
            _data.imageHeight = imageHeight;
        }
    }
}
