using Managers;
using UnityEngine;

namespace UI
{
    public class DrawingInput
    {
        public float scrollZoomSensitivity;
        public float moveSensitivity;
        private Camera viewCam;
        private Camera displayCam;
        
        private bool mouseIsDrawing;
        private Vector2 startPosWhenDragging;
        private Vector2 startPosViewCam;
        private Vector2 startPosDisplayCam;

        public DrawingInput(Camera _viewCam, Camera _displayCam, float _scrollZoomSensitivity, float _moveSensitivity)
        {
            viewCam = _viewCam;
            displayCam = _displayCam;
            scrollZoomSensitivity = _scrollZoomSensitivity;
            moveSensitivity = _moveSensitivity;
            startPosViewCam = _viewCam.transform.position;
            startPosDisplayCam = _displayCam.transform.position;
        }
        
        public void UpdateDrawingInput(Vector3[] _drawAreaCorners, Vector3[] _displayAreaCorners, Vector2 _camPos, float _camZoom)
        {
            bool isMouseInsideDrawArea = IsMouseInsideDrawArea(_drawAreaCorners);
            bool isMouseInsideDisplayArea = IsMouseInsideDrawArea(_displayAreaCorners);
            
            if (isMouseInsideDrawArea)
            {
                DrawInput(_drawAreaCorners, _camPos, _camZoom);
                MoveCamera(viewCam, _drawAreaCorners, startPosViewCam);
            }
            else if (isMouseInsideDisplayArea)
            {
                MoveCamera(displayCam, _displayAreaCorners, startPosDisplayCam);
            }
            
            if(Input.GetMouseButtonUp(0) && mouseIsDrawing || !isMouseInsideDrawArea && mouseIsDrawing)
            {
                EventSystem.RaiseEvent(EventType.FINISHED_STROKE);
                mouseIsDrawing = false;
            }
        }
        private void DrawInput(Vector3[] _drawAreaCorners, Vector2 _camPos, float _camZoom)
        {
            if (Input.GetMouseButton(0))
            {
                Vector4 drawCorners = GetScaledDrawingCorners(_camPos, _camZoom, _drawAreaCorners);
                float mousePosX = Input.mousePosition.x.Remap(drawCorners.x, drawCorners.z, 0, 2048);
                float mousePosY = Input.mousePosition.y.Remap(drawCorners.y, drawCorners.w, 0, 2048);
                Vector2 mousePos = new Vector2(mousePosX, mousePosY);
                EventSystem<Vector2>.RaiseEvent(EventType.DRAW, mousePos);

                mouseIsDrawing = true;
            }
        }
        private void MoveCamera(Camera _cam, Vector3[] _corners, Vector2 _draggingBounds)
        {
            if (Input.mouseScrollDelta.y != 0)
            {
                _cam.orthographicSize -= Input.mouseScrollDelta.y * scrollZoomSensitivity;
                _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, 0.01f, 0.5f);
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
            }
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

