using Managers;
using UnityEngine;
using EventType = Managers.EventType;

namespace UI
{
    public class DrawingInput3D
    {
        public float scrollZoomSensitivity;
        public float rotateSensitivity = 180;
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
        private Transform viewFocus;
        private Transform displayFocus;
        private int shapeAmountSides = 6;
        private bool isMouseInsideDrawArea => IsMouseInsideDrawArea(drawAreaCorners);
        private bool isMouseInsideDisplayArea => IsMouseInsideDrawArea(displayAreaCorners);

        public DrawingInput3D(Camera _viewCam, Camera _displayCam, float _scrollZoomSensitivity, Transform _viewFocus, Transform _displayFocus)
        {
            viewCam = _viewCam;
            displayCam = _displayCam;
            scrollZoomSensitivity = _scrollZoomSensitivity;
            viewFocus = _viewFocus;
            displayFocus = _displayFocus;
            startPosViewCam = _viewCam.transform.position;
            startPosDisplayCam = _displayCam.transform.position;
            
            EventSystem<float>.Subscribe(EventType.TIME, SetTime);
            EventSystem<RectTransform, RectTransform>.Subscribe(EventType.VIEW_CHANGED, SetDrawArea);
            EventSystem.Subscribe(EventType.RESET_TIME, StopDrawing);
            EventSystem<ToolType>.Subscribe(EventType.CHANGE_TOOLTYPE, SetToolType);
            EventSystem<int>.Subscribe(EventType.CHANGE_TOOLTYPE, SetAmountShapeSides);
        }

        ~DrawingInput3D()
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
        
        public void UpdateDrawingInput(Camera cam, Vector2 _camPos, float _camZoom)
        {
            if (Input.GetMouseButtonUp(0) && !Input.GetKey(KeyCode.LeftControl) || Input.GetKeyUp(KeyCode.LeftControl))
            {
                isInteracting = false;
                EventSystem<bool>.RaiseEvent(EventType.IS_INTERACTING, false);
            }

            bool hitModel;
            Vector3 mousePos = new Vector3();
            if (Input.GetMouseButton(0))
            {
                hitModel = mousePosToWorld(cam, out mousePos);
            }

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
                    if (SpawnSquare(mousePos))
                        return;
                    if (SpawnPolygon(mousePos))
                        return;

                    if(DrawInput(mousePos))
                        return;
                
                    if (SetBrushSize(mousePos))
                        return;
                
                    if(MoveCamera(viewCam, drawAreaCorners, startPosViewCam, viewFocus))
                        return;
                }
                else if (isMouseInsideDisplayArea)
                {
                    MoveCamera(displayCam, drawAreaCorners, startPosDisplayCam, displayFocus);
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
        private bool SpawnSquare(Vector2 _mousePos)
        {
            if (currentToolType == ToolType.Square && Input.GetMouseButtonDown(0))
            {
                EventSystem<Vector2, int>.RaiseEvent(EventType.SPAWN_STAMP, _mousePos, 4);
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
        
        private bool DrawInput(Vector3 _mousePos)
        {
            if (Input.GetMouseButton(0) && currentToolType == ToolType.brush)
            {
                isInteracting = true;
                EventSystem<bool>.RaiseEvent(EventType.IS_INTERACTING, true);
                EventSystem<Vector3>.RaiseEvent(EventType.DRAW, _mousePos);
                EventSystem<bool>.RaiseEvent(EventType.DRAW, false);

                mouseIsDrawing = true;
                return true;
            }
            return false;
        }

        private Vector3 startDraggingPos;
        private Vector3 startDraggingFocusPos;
        private Quaternion startRotation;
        private bool middleClickPressed;
        private bool MoveCamera(Camera _cam, Vector3[] _corners, Vector2 _draggingBounds, Transform _focusPoint)
        {
            if (Input.mouseScrollDelta.y != 0)
            {
                float distToFocus = Vector3.Distance(_cam.transform.position, _focusPoint.position) / 5;
                distToFocus = Mathf.Clamp01(distToFocus);
                distToFocus *= distToFocus * distToFocus * distToFocus;
                _cam.transform.position += _cam.transform.forward * (Input.mouseScrollDelta.y * distToFocus);
                
                return true;
            }
            
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(2))
            {
                float mousePosX = Input.mousePosition.x.Remap(_corners[0].x, _corners[2].x, 1f, -1f);
                float mousePosY = Input.mousePosition.y.Remap(_corners[0].y, _corners[2].y, 1f, -1f);
                startPosWhenDragging = new Vector2(mousePosX, mousePosY);
                startPosWhenDragging *= Vector3.Distance(_focusPoint.position, _cam.transform.position);
                startDraggingPos = _cam.transform.position;
                startDraggingFocusPos = _focusPoint.position;
            }
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(2))
            {
                float mousePosX = Input.mousePosition.x.Remap(_corners[0].x, _corners[2].x, 1f, -1f);
                float mousePosY = Input.mousePosition.y.Remap(_corners[0].y, _corners[2].y, 1f, -1f);
                Vector2 currentPos = new Vector2(mousePosX, mousePosY);
                currentPos *= Vector3.Distance(_focusPoint.position, _cam.transform.position);
                Vector2 diff =  currentPos - startPosWhenDragging;
                Vector3 newPoss = diff.x * _cam.transform.right + diff.y * _cam.transform.up;
                _cam.transform.position = startDraggingPos + newPoss;
                _focusPoint.position = startDraggingFocusPos + newPoss;

                return true;
            }
            
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButtonDown(2))
            {
                if (mousePosToWorld(_cam, out Vector3 focusPos))
                {
                    _focusPoint.position = focusPos;
                }

                return true;
            }
            
            if (Input.GetMouseButtonDown(2))
            {
                middleClickPressed = true;

                startDraggingPos = _cam.ScreenToViewportPoint(Input.mousePosition);
            }
            if (Input.GetMouseButton(2) && middleClickPressed)
            {
                Vector3 direction = startDraggingPos - _cam.ScreenToViewportPoint(Input.mousePosition);

                float dist = Vector3.Distance(_cam.transform.position, _focusPoint.position);
                _cam.transform.position = _focusPoint.position;
                
                _cam.transform.Rotate(new Vector3(1, 0, 0), direction.y * rotateSensitivity);
                _cam.transform.Rotate(new Vector3(0, 1, 0), -direction.x * rotateSensitivity, Space.World);
                _cam.transform.Translate(new Vector3(0, 0, -dist));
                
                startDraggingPos = _cam.ScreenToViewportPoint(Input.mousePosition);
                return true;
            }

            if (Input.GetMouseButtonUp(2))
            {
                middleClickPressed = false;
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

        private bool mousePosToWorld(Camera cam, out Vector3 _mousePos)
        {
            _mousePos = new Vector3();
            
            Vector4 drawCorners = new Vector4(drawAreaCorners[0].x, drawAreaCorners[0].y, drawAreaCorners[2].x, drawAreaCorners[2].y);
            float mousePosX = Input.mousePosition.x.Remap(drawCorners.x, drawCorners.z, 0, cam.pixelWidth);
            float mousePosY = Input.mousePosition.y.Remap(drawCorners.y, drawCorners.w, 0, cam.pixelHeight);
            Vector2 mousePos = new Vector2(mousePosX, mousePosY);
            
            Ray ray = cam.ScreenPointToRay(mousePos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                _mousePos = hit.point;
                return true;
            }

            return false;
        }
        
    }
}