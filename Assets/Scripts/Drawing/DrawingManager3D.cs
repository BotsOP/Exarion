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
    public class DrawingManager3D : MonoBehaviour, IDataPersistence
    {
        [Header("Materials")]
        // [SerializeField] private Material drawingMat;
        // [SerializeField] private Material displayMat;
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
        [SerializeField] private RenderTexture rt;
        [SerializeField] private RenderTexture rtID;
        [SerializeField] private RenderTexture rtHighlight;
    
        private RenderTexture drawingRenderTexture;
        private int kernelID;
        private Vector2 threadGroupSizeOut;
        private Vector2 threadGroupSize;
        private Vector3 lastCursorPos;
        private bool firstUse = true;
        private List<BrushStroke> tempBrushStrokes;
        private List<BrushStrokeID> selectedBrushStrokes;

        private Drawing3D drawer;
        private DrawHighlight3D highlighter;
        private DrawPreview previewer;
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
            previewer = new DrawPreview(imageWidth, imageHeight);
            drawStamp = new DrawStamp();
            
            tempBrushStrokes = new List<BrushStroke>();
            selectedBrushStrokes = new List<BrushStrokeID>();
            collisionBoxMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            collisionBoxMax = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
        }

        void OnEnable()
        {
            EventSystem.Subscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem.Subscribe(EventType.REDRAW_ALL, RedrawAll);
            EventSystem.Subscribe(EventType.CLEAR_SELECT, ClearHighlightStroke);
            EventSystem.Subscribe(EventType.STOPPED_SETTING_BRUSH_SIZE, ClearPreview);
            EventSystem<int>.Subscribe(EventType.CHANGE_PAINTTYPE, SetPaintType);
            EventSystem<Vector3>.Subscribe(EventType.DRAW, Draw);
            EventSystem<float>.Subscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Subscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector3>.Subscribe(EventType.SELECT_BRUSHSTROKE, SelectBrushStroke);
            EventSystem<float>.Subscribe(EventType.TIME, SetTime);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem<BrushStrokeID>.Subscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<Vector2>.Subscribe(EventType.MOVE_STROKE, MoveDirStrokes);
            EventSystem<float, bool>.Subscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            EventSystem<float, bool>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<Vector2, string>.Subscribe(EventType.SPAWN_STAMP, DrawStamp);
            EventSystem<Vector2, int>.Subscribe(EventType.SPAWN_STAMP, DrawStamp);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem.Subscribe(EventType.MOVE_STROKE, StoppedMovingStroke);
            EventSystem.Subscribe(EventType.RESIZE_STROKE, StoppedResizing);
            EventSystem.Subscribe(EventType.ROTATE_STROKE, StoppedRotating);
            EventSystem<Vector2, List<BrushStrokeID>>.Subscribe(EventType.MOVE_STROKE, MoveDirStrokes);
            EventSystem<List<BrushStrokeID>, Vector2>.Subscribe(EventType.MOVE_STROKE, MovePosStrokes);
            EventSystem<List<BrushStrokeID>, List<Vector2>>.Subscribe(EventType.MOVE_STROKE, MovePosStrokes);
            EventSystem<float, bool, List<BrushStrokeID>>.Subscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            EventSystem<List<BrushStrokeID>, float>.Subscribe(EventType.RESIZE_STROKE, ResizeSetStrokes);
            EventSystem<List<BrushStrokeID>, List<float>>.Subscribe(EventType.RESIZE_STROKE, ResizeSetStrokes);
            EventSystem<float, bool, List<BrushStrokeID>>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<List<BrushStrokeID>, float>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<List<BrushStrokeID>, List<float>>.Subscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem.Subscribe(EventType.DUPLICATE_STROKE, DuplicateBrushStrokes);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.SELECT_BRUSHSTROKE, HighlightStroke);
            EventSystem<List<BrushStrokeID>, int>.Subscribe(EventType.CHANGE_DRAW_ORDER, ChangeDrawOrder);
            EventSystem<List<BrushStrokeID>, float>.Subscribe(EventType.CHANGE_BRUSH_SIZE, ChangeBrushStrokeBrushSize);
            EventSystem<List<BrushStrokeID>, List<float>>.Subscribe(EventType.CHANGE_BRUSH_SIZE, ChangeBrushStrokeBrushSize);
            EventSystem<Renderer, Renderer>.Subscribe(EventType.CHANGED_MODEL, SetRenderer);
            EventSystem<int, Texture2D>.Subscribe(EventType.IMPORT_MODEL_TEXTURE, UpdateTexture);
        }

        private void OnDisable()
        {
            EventSystem.Unsubscribe(EventType.FINISHED_STROKE, StoppedDrawing);
            EventSystem.Unsubscribe(EventType.REDRAW_ALL, RedrawAll);
            EventSystem.Unsubscribe(EventType.CLEAR_SELECT, ClearHighlightStroke);
            EventSystem.Unsubscribe(EventType.STOPPED_SETTING_BRUSH_SIZE, ClearPreview);
            EventSystem<int>.Unsubscribe(EventType.CHANGE_PAINTTYPE, SetPaintType);
            EventSystem<Vector3>.Unsubscribe(EventType.DRAW, Draw);
            EventSystem<float>.Unsubscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector2>.Unsubscribe(EventType.SET_BRUSH_SIZE, SetBrushSize);
            EventSystem<Vector3>.Unsubscribe(EventType.SELECT_BRUSHSTROKE, SelectBrushStroke);
            EventSystem<float>.Unsubscribe(EventType.TIME, SetTime);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<List<BrushStrokeID>>.Subscribe(EventType.ADD_SELECT, HighlightStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REMOVE_STROKE, RemoveStroke);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REDRAW_STROKE, RedrawStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REDRAW_STROKES, RedrawStrokes);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.ADD_STROKE, AddStroke);
            EventSystem<Vector2>.Unsubscribe(EventType.MOVE_STROKE, MoveDirStrokes);
            EventSystem<float, bool>.Unsubscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            EventSystem<float, bool>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<Vector2, string>.Unsubscribe(EventType.SPAWN_STAMP, DrawStamp);
            EventSystem<Vector2, int>.Unsubscribe(EventType.SPAWN_STAMP, DrawStamp);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.REMOVE_SELECT, RemoveHighlight);
            EventSystem.Unsubscribe(EventType.MOVE_STROKE, StoppedMovingStroke);
            EventSystem.Unsubscribe(EventType.RESIZE_STROKE, StoppedResizing);
            EventSystem.Unsubscribe(EventType.ROTATE_STROKE, StoppedRotating);
            EventSystem<Vector2, List<BrushStrokeID>>.Unsubscribe(EventType.MOVE_STROKE, MoveDirStrokes);
            EventSystem<List<BrushStrokeID>, Vector2>.Unsubscribe(EventType.MOVE_STROKE, MovePosStrokes);
            EventSystem<List<BrushStrokeID>, List<Vector2>>.Unsubscribe(EventType.MOVE_STROKE, MovePosStrokes);
            EventSystem<float, bool, List<BrushStrokeID>>.Unsubscribe(EventType.RESIZE_STROKE, ResizeStrokes);
            EventSystem<List<BrushStrokeID>, float>.Unsubscribe(EventType.RESIZE_STROKE, ResizeSetStrokes);
            EventSystem<List<BrushStrokeID>, List<float>>.Unsubscribe(EventType.RESIZE_STROKE, ResizeSetStrokes);
            EventSystem<float, bool, List<BrushStrokeID>>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<List<BrushStrokeID>, float>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem<List<BrushStrokeID>, List<float>>.Unsubscribe(EventType.ROTATE_STROKE, RotateStroke);
            EventSystem.Unsubscribe(EventType.DUPLICATE_STROKE, DuplicateBrushStrokes);
            EventSystem<List<BrushStrokeID>>.Unsubscribe(EventType.SELECT_BRUSHSTROKE, HighlightStroke);
            EventSystem<List<BrushStrokeID>, int>.Unsubscribe(EventType.CHANGE_DRAW_ORDER, ChangeDrawOrder);
            EventSystem<List<BrushStrokeID>, float>.Unsubscribe(EventType.CHANGE_BRUSH_SIZE, ChangeBrushStrokeBrushSize);
            EventSystem<List<BrushStrokeID>, List<float>>.Unsubscribe(EventType.CHANGE_BRUSH_SIZE, ChangeBrushStrokeBrushSize);
            EventSystem<Renderer, Renderer>.Unsubscribe(EventType.CHANGED_MODEL, SetRenderer);
            EventSystem<int, Texture2D>.Unsubscribe(EventType.IMPORT_MODEL_TEXTURE, UpdateTexture);
        }

        private void SetRenderer(Renderer _drawingRend, Renderer _displayRend)
        {
            rend = _drawingRend;
            drawer.rend = _drawingRend;
            highlighter.rend = _drawingRend;

            Mesh mesh = _drawingRend.gameObject.GetComponent<MeshFilter>().sharedMesh;
            EventSystem<int>.RaiseEvent(EventType.UPDATE_SUBMESH_COUNT, mesh.subMeshCount);

            drawingMats.Clear();
            displayMats.Clear();
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                Material drawingMat = new Material(drawShader);
                Material displayMat = new Material(displayShader);
                CustomRenderTexture drawingRT = drawer.addRT();
                drawingMat.SetTexture("_MainTex", drawingRT);
                displayMat.SetTexture("_MainTex", drawingRT);
                drawingMat.SetTexture("_SelectTex", highlighter.AddRT());
                
                drawingMats.Add(drawingMat);
                displayMats.Add(displayMat);

                _drawingRend.materials[i] = drawingMat;
                _displayRend.materials[i] = displayMat;
            }
            
            _drawingRend.materials = drawingMats.ToArray();
            _displayRend.materials = displayMats.ToArray();
            
            rt = drawer.rts[0];
            rtID = drawer.rtIDs[0];
            rtHighlight = highlighter.rtHighlights[0];
        }

        private void UpdateTexture(int _subMesh, Texture2D _texture)
        {
            if (_subMesh >= drawingMats.Count)
            {
                Debug.LogError($"given submesh {_subMesh} is bigger than amount mats {drawingMats.Count - 1}");
                return;
            }
            
            drawingMats[_subMesh].SetTexture("_Mask", _texture);
            displayMats[_subMesh].SetTexture("_Mask", _texture);
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
        private void SetBrushSize(Vector2 _mousePos)
        {
            previewer.Preview(_mousePos, brushSize);
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
                cachedTime = cachedTime > 1 ? 0 : time;
                newBrushStrokeID = drawer.GetNewID();
                lastCursorPos = _worldPos;
                firstUse = false;
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

            //Move this to drawing unput
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
            tempAvgPos /= tempBrushStrokes.Count;
            List<BrushStroke> brushStrokes = new List<BrushStroke>(tempBrushStrokes);
            
            BrushStrokeID brushStrokeID = new BrushStrokeID(
                brushStrokes, paintType, startBrushStrokeTime, time, collisionBoxMin, collisionBoxMax, drawer.brushStrokesID.Count, tempAvgPos);

            sphere1.transform.position = collisionBoxMin;
            sphere2.transform.position = collisionBoxMax;
            
            drawer.brushStrokesID.Add(brushStrokeID);
            
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, brushStrokeID);
            
            tempAvgPos = Vector2.zero;
            collisionBoxMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            collisionBoxMax = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);
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
            Debug.Log($"Remove stroke");
            foreach (BrushStroke brushStroke in _brushStrokeID.brushStrokes)
            {
                drawer.Draw(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, PaintType.Erase);
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
                    drawer.Draw(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, PaintType.Erase);
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
                // if (selectedBrushStrokes.Remove(brushStrokeID))
                // {
                //     highlighter.HighlightStroke(selectedBrushStrokes);
                //     return;
                // }

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
            for (var i = 0; i < _brushStrokeIDs.Count; i++)
            {
                selectedBrushStrokes.Remove(_brushStrokeIDs[i]);
            }

            highlighter.HighlightStroke(selectedBrushStrokes);
        }
        private void ClearHighlightStroke()
        {
            selectedBrushStrokes.Clear();
            highlighter.ClearHighlight();
        }
        
        private void SelectBrushStroke(Vector3 _worldPos)
        {
            foreach (BrushStrokeID brushStrokeID in drawer.brushStrokesID)
            {
                if (drawer.IsMouseOverBrushStroke(brushStrokeID, _worldPos))
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
            
            MoveDirStrokes(new Vector2(10, 10), duplicateBrushStrokeIDs);
            EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
            selectedBrushStrokes = duplicateBrushStrokeIDs;
            EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.SELECT_TIMELINECLIP, selectedBrushStrokes);
        }

        private void RedrawAll()
        {
            drawer.RedrawAll();
        }

        private Vector2 lastMovePos;
        private void MoveDirStrokes(Vector2 _dir)
        {
            if (_dir == Vector2.zero)
                return;

            EventSystem.RaiseEvent(EventType.UPDATE_CLIP_INFO);
            lastMovePos += _dir;
            
            foreach (var brushStrokeID in selectedBrushStrokes)
            {
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
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
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(selectedBrushStrokes);
        }
        
        private void MoveDirStrokes(Vector2 _dir, List<BrushStrokeID> _brushStrokeIDs)
        {
            if (_dir == Vector2.zero)
                return;
            
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
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
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        private void MovePosStrokes(List<BrushStrokeID> _brushStrokeIDs, Vector2 _pos)
        {
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
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
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        private void MovePosStrokes(List<BrushStrokeID> _brushStrokeIDs, List<Vector2> _newPositions)
        {
            for (int i = 0; i < _brushStrokeIDs.Count; i++)
            {
                var brushStrokeID = _brushStrokeIDs[i];
                Vector2 pos = _newPositions[i];
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
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
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        private void StoppedMovingStroke()
        {
            ICommand moveCommand = new MoveCommand(lastMovePos, selectedBrushStrokes);
            EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, moveCommand);
            lastMovePos = Vector2.zero;
        }

        private float rotateAmount;
        private void RotateStroke(float _angle, bool _center)
        {
            if (_angle == 0)
                return;

            EventSystem.RaiseEvent(EventType.UPDATE_CLIP_INFO);
            rotateAmount += _angle;
            
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
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
                
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
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(selectedBrushStrokes);
        }
        
        private void RotateStroke(float _angle, bool _center, List<BrushStrokeID> _brushStrokeIDs)
        {
            if (_angle == 0)
                return;

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
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
                
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
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        
        private void RotateStroke(List<BrushStrokeID> _brushStrokeIDs, float _angle)
        {
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                float angleDelta = _angle - brushStrokeID.angle;
                brushStrokeID.angle = _angle;
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
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
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        
        private void RotateStroke(List<BrushStrokeID> _brushStrokeIDs, List<float> _angles)
        {
            for (int i = 0; i < _brushStrokeIDs.Count; i++)
            {
                var brushStrokeID = _brushStrokeIDs[i];
                float angleDelta = _angles[i] - brushStrokeID.angle;
                brushStrokeID.angle = _angles[i];
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
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
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }

        private void StoppedRotating()
        {
            ICommand rotateCommand = new RotateCommand(rotateAmount, UIManager.center, selectedBrushStrokes);
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
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
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
            
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(selectedBrushStrokes);
        }
        private void ResizeStrokes(float _sizeIncrease, bool _center, List<BrushStrokeID> _brushStrokeIDs)
        {
            if (Math.Abs(_sizeIncrease - 1) < 0.001f)
                return;

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
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
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
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        private void ResizeSetStrokes(List<BrushStrokeID> _brushStrokeIDs, float _newSize)
        {
            Vector2 allAvgPos;
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                float sizeIncrease = _newSize / brushStrokeID.scale;
                brushStrokeID.scale = _newSize;
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
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
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        private void ResizeSetStrokes(List<BrushStrokeID> _brushStrokeIDs, List<float> _newSize)
        {
            Vector2 allAvgPos;
            for (int i = 0; i < _brushStrokeIDs.Count; i++)
            {
                var brushStrokeID = _brushStrokeIDs[i];
                float sizeIncrease = _newSize[i] / brushStrokeID.scale;
                brushStrokeID.scale = _newSize[i];
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
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
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        private void StoppedResizing()
        {
            ICommand resizeCommand = new ResizeCommand(resizeAmount, UIManager.center, selectedBrushStrokes);
            EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, resizeCommand);
            resizeAmount = 1;
        }

        private void ChangeDrawOrder(List<BrushStrokeID> _brushStrokeIDs, int _amount)
        {
            List<int> indexes = _brushStrokeIDs.Select(_brushStrokeID => drawer.brushStrokesID.IndexOf(_brushStrokeID)).ToList();
            indexes.Sort();

            if (_amount > 0)
            {
                for (int i = 0; i < indexes.Count; i++)
                {
                    int newIndex = indexes[i] + _amount;
                    BrushStrokeID temp = drawer.brushStrokesID[indexes[i]];
                    drawer.RedrawStroke(temp, PaintType.Erase);
                    temp.indexWhenDrawn = newIndex;
                    newIndex = Mathf.Clamp(newIndex, 0, drawer.brushStrokesID.Count - 1);
                    
                    drawer.brushStrokesID.Remove(temp);
                    if (newIndex == drawer.brushStrokesID.Count - 1)
                    {
                        drawer.brushStrokesID.Add(temp);
                        continue;
                    }
                    drawer.brushStrokesID.Insert(newIndex, temp);
                }
            }
            else
            {
                for (int i = indexes.Count - 1; i >= 0; i--)
                {
                    int newIndex = indexes[i] + _amount;
                    BrushStrokeID temp = drawer.brushStrokesID[indexes[i]];
                    drawer.RedrawStroke(temp, PaintType.Erase);
                    temp.indexWhenDrawn = newIndex;
                    newIndex = Mathf.Clamp(newIndex, 0, drawer.brushStrokesID.Count);
                
                    drawer.brushStrokesID.Remove(temp);
                    drawer.brushStrokesID.Insert(newIndex, temp);
                }
            }
            drawer.RedrawAllDirect(_brushStrokeIDs);
        }

        private void ChangeBrushStrokeBrushSize(List<BrushStrokeID> _brushStrokeIDs, float _amount)
        {
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
                brushStrokeID.SetBrushSize(_amount);
                brushStrokeID.RecalculateCollisionBox();
            }
            highlighter.HighlightStroke(selectedBrushStrokes);
            drawer.RedrawAllSafe(_brushStrokeIDs);
        }
        private void ChangeBrushStrokeBrushSize(List<BrushStrokeID> _brushStrokeIDs, List<float> _amount)
        {
            for (var i = 0; i < _brushStrokeIDs.Count; i++)
            {
                var brushStrokeID = _brushStrokeIDs[i];
                drawer.RedrawStroke(brushStrokeID, PaintType.Erase);
                brushStrokeID.SetBrushSize(_amount[i]);
                brushStrokeID.RecalculateCollisionBox();
            }
            HighlightStroke(_brushStrokeIDs);
            drawer.RedrawAllSafe(_brushStrokeIDs);
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
        private void DrawStamp(Vector2 _mousePos, int _sides)
        {
            BrushStrokeID brushStrokeID = drawStamp.GetPolygon(_sides, _mousePos, 256, drawer.brushStrokesID.Count, 
                                                             brushSize, time, Mathf.Clamp01(time + 0.05f));
            EventSystem<float>.RaiseEvent(EventType.ADD_TIME, 0.05f);
            drawer.RedrawStrokeInterpolation(brushStrokeID);
            
            drawer.brushStrokesID.Add(brushStrokeID);
            
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, brushStrokeID);
        }

        public void LoadData(ToolData _data)
        {
            imageWidth = _data.imageWidth;
            imageHeight = _data.imageHeight;
        }
        public void SaveData(ToolData _data)
        {
            _data.displayImg = drawer.rts[0].ToBytesPNG(imageWidth, imageHeight);
            _data.imageWidth = imageWidth;
            _data.imageHeight = imageHeight;
        }
    }
}
