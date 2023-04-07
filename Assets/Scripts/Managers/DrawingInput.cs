using System;
using Managers;
using UnityEngine;
using EventType = Managers.EventType;

namespace UI
{
    public class DrawingInput
    {
        public float scrollZoomSensitivity;
        private Camera viewCam;
        private Camera displayCam;
        
        private bool mouseIsDrawing;
        private Vector2 startPosWhenDragging;
        private Vector2 startMousePos;
        private Vector2 startPosViewCam;
        private Vector2 startPosDisplayCam;
        private float time;

        public DrawingInput(Camera _viewCam, Camera _displayCam, float _scrollZoomSensitivity)
        {
            viewCam = _viewCam;
            displayCam = _displayCam;
            scrollZoomSensitivity = _scrollZoomSensitivity;
            startPosViewCam = _viewCam.transform.position;
            startPosDisplayCam = _displayCam.transform.position;
            
            EventSystem<float>.Subscribe(EventType.TIME, SetTime);
        }

        ~DrawingInput()
        {
            EventSystem<float>.Unsubscribe(EventType.TIME, SetTime);
        }

        private void SetTime(float _time)
        {
            time = _time;
        }
        
        public void UpdateDrawingInput(Vector3[] _drawAreaCorners, Vector3[] _displayAreaCorners, Vector2 _camPos, float _camZoom)
        {
            bool isMouseInsideDrawArea = IsMouseInsideDrawArea(_drawAreaCorners);
            bool isMouseInsideDisplayArea = IsMouseInsideDrawArea(_displayAreaCorners);
            
            if (mouseIsDrawing)
            {
                if(Input.GetMouseButtonUp(0) || !isMouseInsideDrawArea || Math.Abs(time - 1.1) < 0.1)
                {
                    EventSystem.RaiseEvent(EventType.FINISHED_STROKE);
                    EventSystem<bool>.RaiseEvent(EventType.DRAW, true);

                    mouseIsDrawing = false;
                }
            }
            
            if (isMouseInsideDrawArea)
            {
                Vector4 drawCorners = GetScaledDrawingCorners(_camPos, _camZoom, _drawAreaCorners);
                float mousePosX = Input.mousePosition.x.Remap(drawCorners.x, drawCorners.z, 0, 2048);
                float mousePosY = Input.mousePosition.y.Remap(drawCorners.y, drawCorners.w, 0, 2048);
                Vector2 mousePos = new Vector2(mousePosX, mousePosY);
                
                if(DrawInput(mousePos))
                    return;
                
                if (SetBrushSize(mousePos))
                    return;
                
                if(MoveCamera(viewCam, _drawAreaCorners, startPosViewCam))
                    return;

            }
            else if (isMouseInsideDisplayArea)
            {
                MoveCamera(displayCam, _displayAreaCorners, startPosDisplayCam);
            }
        }
        private bool DrawInput(Vector2 _mousePos)
        {
            if (Input.GetMouseButton(0) && !(Math.Abs(time - 1.1) < 0.1))
            {
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
                Debug.Log($"{brushSize}");
                EventSystem<float>.RaiseEvent(EventType.SET_BRUSH_SIZE, brushSize);
                EventSystem<Vector2>.RaiseEvent(EventType.SET_BRUSH_SIZE, _mousePos);
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

