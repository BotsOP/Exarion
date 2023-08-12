using System;
using Managers;
using UnityEngine;
using EventType = Managers.EventType;

namespace UI
{
    public class DrawingInput
    {
        public float scrollZoomSensitivity;
        private ToolType currentToolType;
        private Camera viewCam;
        private Camera displayCam;
        
        private bool mouseIsDrawing;
        private Vector2 startPosWhenDragging;
        private Vector2 startMousePos;
        private Vector2 startPosViewCam;
        private Vector2 startPosDisplayCam;
        private Vector2 lastMousePos;
        private float time;
        private Vector3[] drawAreaCorners = new Vector3[4];
        private Vector3[] displayAreaCorners = new Vector3[4];
        private bool isInteracting;
        private int imageWidth;
        private int imageHeight;
        private int shapeAmountSides = 6;
        private bool isMouseInsideDrawArea => IsMouseInsideDrawArea(drawAreaCorners);
        private bool isMouseInsideDisplayArea => IsMouseInsideDrawArea(displayAreaCorners);

        public DrawingInput(Camera _viewCam, Camera _displayCam, float _scrollZoomSensitivity, int _imageWidth, int _imageHeight)
        {
            viewCam = _viewCam;
            displayCam = _displayCam;
            scrollZoomSensitivity = _scrollZoomSensitivity;
            startPosViewCam = _viewCam.transform.position;
            startPosDisplayCam = _displayCam.transform.position;
            imageWidth = _imageWidth;
            imageHeight = _imageHeight;
            
            EventSystem<float>.Subscribe(EventType.TIME, SetTime);
            EventSystem<RectTransform, RectTransform>.Subscribe(EventType.VIEW_CHANGED, SetDrawArea);
            EventSystem.Subscribe(EventType.RESET_TIME, StopDrawing);
            EventSystem<ToolType>.Subscribe(EventType.CHANGE_TOOLTYPE, SetToolType);
            EventSystem<int>.Subscribe(EventType.CHANGE_TOOLTYPE, SetAmountShapeSides);
        }

        ~DrawingInput()
        {
            EventSystem<RectTransform, RectTransform>.Unsubscribe(EventType.VIEW_CHANGED, SetDrawArea);
            EventSystem<float>.Unsubscribe(EventType.TIME, SetTime);
            EventSystem.Unsubscribe(EventType.RESET_TIME, StopDrawing);
            EventSystem<ToolType>.Unsubscribe(EventType.CHANGE_TOOLTYPE, SetToolType);
            EventSystem<int>.Unsubscribe(EventType.CHANGE_TOOLTYPE, SetAmountShapeSides);
        }

        private void SetToolType(ToolType _toolType)
        {
            currentToolType = _toolType;
        }
        private void SetAmountShapeSides(int _sides)
        {
            shapeAmountSides = _sides;
        }

        private void SetDrawArea(RectTransform _currentDrawArea, RectTransform _currentDisplayArea)
        {
            _currentDrawArea.GetWorldCorners(drawAreaCorners);
            _currentDisplayArea.GetWorldCorners(displayAreaCorners);
        }

        private void SetTime(float _time)
        {
            time = _time;
        }
        
        public void UpdateDrawingInput(Vector2 _camPos, float _camZoom)
        {
            if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.LeftControl))
            {
                isInteracting = false;
                EventSystem<bool>.RaiseEvent(EventType.IS_INTERACTING, false);
            }
            
            Vector4 drawCorners = GetScaledDrawingCorners(_camPos, _camZoom, drawAreaCorners);
            float mousePosX = Input.mousePosition.x.Remap(drawCorners.x, drawCorners.z, 0, imageWidth);
            float mousePosY = Input.mousePosition.y.Remap(drawCorners.y, drawCorners.w, 0, imageHeight);
            Vector2 mousePos = new Vector2(mousePosX, mousePosY);

            if (!UIManager.isInteracting || isInteracting)
            {
                StopDrawing();
                if (Resize(mousePos))
                {
                    StopDrawing();
                    return;
                }
                    
                if (RotateBrushStrokes(mousePos))
                {
                    StopDrawing();
                    return;
                }
                
                if (DuplicateStroke())
                    return;
                
                if (isMouseInsideDrawArea)
                {
                    if (SelectBrushStroke(mousePos))
                    {
                        StopDrawing();
                        return;
                    }
                
                    if (MoveBrushStrokes(mousePos))
                    {
                        StopDrawing();
                        return;
                    }

                    if (SpawnCircle(mousePos))
                        return;
                    if (SpawnLine(mousePos))
                        return;
                    if (SpawnPolygon(mousePos))
                        return;

                    if(DrawInput(mousePos))
                        return;
                
                    if (SetBrushSize(mousePos))
                        return;
                
                    if(MoveCamera(viewCam, drawAreaCorners, startPosViewCam))
                        return;
                }
                else if (isMouseInsideDisplayArea)
                {
                    MoveCamera(displayCam, drawAreaCorners, startPosDisplayCam);
                }
            }
            else
            {
                StopDrawing();
            }
            
            lastMousePos = mousePos;
        }

        private bool SpawnCircle(Vector2 _mousePos)
        {
            if (currentToolType == ToolType.Circle && Input.GetMouseButtonDown(0))
            {
                EventSystem<Vector2, int>.RaiseEvent(EventType.SPAWN_STAMP, _mousePos, 360);
                return true;
            }
            return false;
        }
        private bool SpawnLine(Vector2 _mousePos)
        {
            if (currentToolType == ToolType.Line && Input.GetMouseButtonDown(0))
            {
                EventSystem<Vector2, int>.RaiseEvent(EventType.SPAWN_STAMP, _mousePos, 2);
                return true;
            }
            return false;
        }
        private bool SpawnPolygon(Vector2 _mousePos)
        {
            if (currentToolType == ToolType.Polygon && Input.GetMouseButtonDown(0))
            {
                EventSystem<Vector2, int>.RaiseEvent(EventType.SPAWN_STAMP, _mousePos, shapeAmountSides);
                return true;
            }
            return false;
        }

        private void StopDrawing()
        {
            if (mouseIsDrawing)
            {
                if (Input.GetMouseButtonUp(0) || !isMouseInsideDrawArea || time > 1 && !Input.GetMouseButton(0) || Input.GetKeyUp(KeyCode.LeftControl))
                {
                    if(isInteracting && Input.GetKey(KeyCode.LeftControl))
                        return;
                    
                    EventSystem.RaiseEvent(EventType.FINISHED_STROKE);
                    EventSystem<bool>.RaiseEvent(EventType.DRAW, true);

                    mouseIsDrawing = false;
                }
            }
        }

        private bool DuplicateStroke()
        {
            if (Input.GetKeyDown(KeyCode.D) && Input.GetKey(KeyCode.LeftControl))
            {
                EventSystem.RaiseEvent(EventType.DUPLICATE_STROKE);
                return true;
            }

            return false;
        }
        private bool SelectBrushStroke(Vector2 _mousePos)
        {
            if (Input.GetMouseButtonDown(0) && currentToolType == ToolType.select)
            {
                if (!Input.GetKey(KeyCode.LeftShift) && currentToolType != ToolType.select)
                {
                    EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
                    Debug.Log($"clear3");
                }

                EventSystem<bool>.RaiseEvent(EventType.IS_INTERACTING, true);
                EventSystem<Vector2>.RaiseEvent(EventType.SELECT_BRUSHSTROKE, _mousePos);
                return true;
            }
            return false;
        }

        private bool isMoving;
        private bool MoveBrushStrokes(Vector2 _mousePos)
        {
            if (Input.GetMouseButton(0) && currentToolType == ToolType.move)
            {
                isInteracting = true;
                isMoving = true;
                EventSystem<bool>.RaiseEvent(EventType.IS_INTERACTING, true);
                EventSystem<Vector2>.RaiseEvent(EventType.MOVE_STROKE, (_mousePos - lastMousePos));
                lastMousePos = _mousePos;
                return true;
            }
            if ((Input.GetMouseButtonUp(0)) && isMoving)
            {
                isMoving = false;
                EventSystem.RaiseEvent(EventType.MOVE_STROKE);
            }
            return false;
        }

        private bool isRotating;
        private bool RotateBrushStrokes(Vector2 _mousePos)
        {
            if (Input.GetMouseButton(0) && (Input.GetKey(KeyCode.E) || currentToolType == ToolType.rotate))
            {
                isInteracting = true;
                isRotating = true;
                EventSystem<bool>.RaiseEvent(EventType.IS_INTERACTING, true);
                EventSystem<float, bool>.RaiseEvent(EventType.ROTATE_STROKE, (_mousePos.x - lastMousePos.x) / 1000, UIManager.center);
                lastMousePos = _mousePos;
                return true;
            }
            if ((Input.GetMouseButtonUp(0) || (Input.GetKey(KeyCode.E) || currentToolType == ToolType.rotate)) && isRotating)
            {
                isRotating = false;
                EventSystem.RaiseEvent(EventType.ROTATE_STROKE);
            }
            return false;
        }

        private bool isResizing;
        private bool Resize(Vector2 _mousePos)
        {
            if (Input.GetMouseButton(0) && (Input.GetKey(KeyCode.R) || currentToolType == ToolType.resize))
            {
                isInteracting = true;
                isResizing = true;
                float resizeAmount = 1 + Mathf.Clamp((_mousePos.x - lastMousePos.x) / 1000, -1f, 1f);
                EventSystem<bool>.RaiseEvent(EventType.IS_INTERACTING, true);
                EventSystem<float, bool>.RaiseEvent(EventType.RESIZE_STROKE, resizeAmount, UIManager.center);
                lastMousePos = _mousePos;
                return true;
            }
            if ((Input.GetMouseButtonUp(0) || (Input.GetKey(KeyCode.R) || currentToolType == ToolType.resize)) && isResizing)
            {
                isResizing = false;
                EventSystem.RaiseEvent(EventType.RESIZE_STROKE);
            }
            return false;
        }
        
        private bool DrawInput(Vector2 _mousePos)
        {
            if (Input.GetMouseButton(0) && currentToolType == ToolType.brush)
            {
                isInteracting = true;
                EventSystem<bool>.RaiseEvent(EventType.IS_INTERACTING, true);
                EventSystem<Vector2>.RaiseEvent(EventType.DRAW, _mousePos);
                EventSystem<bool>.RaiseEvent(EventType.DRAW, false);

                mouseIsDrawing = true;
                return true;
            }
            return false;
        }
        private bool MoveCamera(Camera _cam, Vector3[] _corners, Vector2 _draggingBounds)
        {
            if (Input.mouseScrollDelta.y != 0)
            {
                _cam.orthographicSize -= Mathf.Pow(_cam.orthographicSize * scrollZoomSensitivity, 1.3f) * Input.mouseScrollDelta.y;
                _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, 0.01f, 0.5f);
                
                return true;
            }
            if (Input.GetMouseButtonDown(1))
            {
                Vector4 drawCorners = GetScaledDrawingCorners(_cam.transform.position, _cam.orthographicSize, _corners);
                float mousePosX = Input.mousePosition.x.Remap(drawCorners.x, drawCorners.z, -0.5f, 0.5f);
                float mousePosY = Input.mousePosition.y.Remap(drawCorners.y, drawCorners.w, -0.5f, 0.5f);
                startPosWhenDragging = new Vector2(mousePosX, mousePosY);
            }
            if (Input.GetMouseButton(1))
            {
                var position = _cam.transform.position;
                Vector4 drawCorners = GetScaledDrawingCorners(position, _cam.orthographicSize, _corners);
                float mousePosX = Input.mousePosition.x.Remap(drawCorners.x, drawCorners.z, -0.5f, 0.5f);
                float mousePosY = Input.mousePosition.y.Remap(drawCorners.y, drawCorners.w, -0.5f, 0.5f);
                Vector2 mousePos = new Vector2(mousePosX, mousePosY);
                mousePos -= startPosWhenDragging;

                mousePos = (Vector2)position - mousePos;
                mousePos.x = Mathf.Clamp(mousePos.x, _draggingBounds.x - 0.5f, _draggingBounds.x + 0.5f);
                mousePos.y = Mathf.Clamp(mousePos.y, _draggingBounds.y -0.5f, _draggingBounds.y + 0.5f);
                position = new Vector3(mousePos.x, mousePos.y, position.z);
                _cam.transform.position = position;

                return true;
            }
            return false;
        }

        private bool SetBrushSize(Vector2 _mousePos)
        {
            if (Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.LeftAlt))
            {
                startMousePos = _mousePos;
            }
            if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftAlt))
            {
                float brushSize = Vector2.Distance(startMousePos, _mousePos);
                brushSize = Mathf.Clamp(brushSize, 1, 1024);
                EventSystem<float>.RaiseEvent(EventType.SET_BRUSH_SIZE, brushSize);
                EventSystem<Vector2>.RaiseEvent(EventType.SET_BRUSH_SIZE, startMousePos);
                return true;
            }
            
            EventSystem.RaiseEvent(EventType.STOPPED_SETTING_BRUSH_SIZE);
            return false;
        }

        private Vector4 GetScaledDrawingCorners(Vector2 _camPos, float _camZoom, Vector3[] _drawAreaCorners)
        {
            //Remap camPos to drawAreaHeight then offset that with the center
            float drawAreaHeight = _drawAreaCorners[2].y - _drawAreaCorners[0].y;
            float camSize = _camZoom + _camZoom;
            float height = 1 / camSize;
            height = drawAreaHeight * height;
            
            float offsetX = _camPos.x.Remap(0, 1, 0, height);
            float offsetY = _camPos.y.Remap(0, 1, 0, height);
            
            Vector2 centerPos = new Vector2(
                (_drawAreaCorners[2].x - _drawAreaCorners[0].x) / 2 + _drawAreaCorners[0].x - offsetX,
                (_drawAreaCorners[2].y - _drawAreaCorners[0].y) / 2 + _drawAreaCorners[0].y - offsetY);
        
            height /= 2;
            return new Vector4(centerPos.x - height, centerPos.y - height, centerPos.x + height, centerPos.y + height);
        }

        private bool IsMouseInsideDrawArea(Vector3[] _drawAreaCorners)
        {
            return Input.mousePosition.x > _drawAreaCorners[0].x && Input.mousePosition.y > _drawAreaCorners[0].y &&
                   Input.mousePosition.x < _drawAreaCorners[2].x && Input.mousePosition.y < _drawAreaCorners[2].y;
        }
    }
}

