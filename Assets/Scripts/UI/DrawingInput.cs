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

        public DrawingInput(Camera viewCam, Camera displayCam, float scrollZoomSensitivity, float moveSensitivity)
        {
            this.viewCam = viewCam;
            this.displayCam = displayCam;
            this.scrollZoomSensitivity = scrollZoomSensitivity;
            this.moveSensitivity = moveSensitivity;
            startPosViewCam = viewCam.transform.position;
            startPosDisplayCam = displayCam.transform.position;
        }
        
        public void UpdateDrawingInput(Vector3[] drawAreaCorners, Vector3[] displayAreaCorners, Vector2 camPos, float camZoom)
        {
            bool isMouseInsideDrawArea = IsMouseInsideDrawArea(drawAreaCorners);
            bool isMouseInsideDisplayArea = IsMouseInsideDrawArea(displayAreaCorners);
            
            if (isMouseInsideDrawArea)
            {
                DrawInput(drawAreaCorners, camPos, camZoom);
                MoveCamera(viewCam, drawAreaCorners, startPosViewCam);
            }
            else if (isMouseInsideDisplayArea)
            {
                MoveCamera(displayCam, displayAreaCorners, startPosDisplayCam);
            }
            
            if(Input.GetMouseButtonUp(0) && mouseIsDrawing || !isMouseInsideDrawArea && mouseIsDrawing)
            {
                EventSystem.RaiseEvent(EventType.FINISHED_STROKE);
                mouseIsDrawing = false;
            }
        }
        private void DrawInput(Vector3[] drawAreaCorners, Vector2 camPos, float camZoom)
        {
            if (Input.GetMouseButton(0))
            {
                Vector4 drawCorners = GetScaledDrawingCorners(camPos, camZoom, drawAreaCorners);
                float mousePosX = Input.mousePosition.x.Remap(drawCorners.x, drawCorners.z, 0, 2048);
                float mousePosY = Input.mousePosition.y.Remap(drawCorners.y, drawCorners.w, 0, 2048);
                Vector2 mousePos = new Vector2(mousePosX, mousePosY);
                EventSystem<Vector2>.RaiseEvent(EventType.DRAW, mousePos);

                mouseIsDrawing = true;
            }
        }
        private void MoveCamera(Camera cam, Vector3[] corners, Vector2 draggingBounds)
        {
            if (Input.mouseScrollDelta.y != 0)
            {
                cam.orthographicSize -= Input.mouseScrollDelta.y * scrollZoomSensitivity;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, 0.01f, 0.5f);
            }
            if (Input.GetMouseButtonDown(1))
            {
                Vector4 drawCorners = GetScaledDrawingCorners(cam.transform.position, cam.orthographicSize, corners);
                float mousePosX = Input.mousePosition.x.Remap(drawCorners.x, drawCorners.z, -0.5f, 0.5f);
                float mousePosY = Input.mousePosition.y.Remap(drawCorners.y, drawCorners.w, -0.5f, 0.5f);
                startPosWhenDragging = new Vector2(mousePosX, mousePosY);
            }
            if (Input.GetMouseButton(1))
            {
                var position = cam.transform.position;
                Vector4 drawCorners = GetScaledDrawingCorners(position, cam.orthographicSize, corners);
                float mousePosX = Input.mousePosition.x.Remap(drawCorners.x, drawCorners.z, -0.5f, 0.5f);
                float mousePosY = Input.mousePosition.y.Remap(drawCorners.y, drawCorners.w, -0.5f, 0.5f);
                Vector2 mousePos = new Vector2(mousePosX, mousePosY);
                mousePos -= startPosWhenDragging;

                mousePos = (Vector2)position - mousePos;
                mousePos.x = Mathf.Clamp(mousePos.x, draggingBounds.x - 0.5f, draggingBounds.x + 0.5f);
                mousePos.y = Mathf.Clamp(mousePos.y, draggingBounds.y -0.5f, draggingBounds.y + 0.5f);
                position = new Vector3(mousePos.x, mousePos.y, position.z);
                cam.transform.position = position;
            }
        }

        private Vector4 GetScaledDrawingCorners(Vector2 camPos, float camZoom, Vector3[] drawAreaCorners)
        {
            //Remap camPos to drawAreaHeight then offset that with the center
            float drawAreaHeight = drawAreaCorners[2].y - drawAreaCorners[0].y;
            float camSize = camZoom + camZoom;
            float height = 1 / camSize;
            height = drawAreaHeight * height;
            
            float offsetX = camPos.x.Remap(0, 1, 0, height);
            float offsetY = camPos.y.Remap(0, 1, 0, height);
            
            Vector2 centerPos = new Vector2(
                (drawAreaCorners[2].x - drawAreaCorners[0].x) / 2 + drawAreaCorners[0].x - offsetX,
                (drawAreaCorners[2].y - drawAreaCorners[0].y) / 2 + drawAreaCorners[0].y - offsetY);
        
            height /= 2;
            return new Vector4(centerPos.x - height, centerPos.y - height, centerPos.x + height, centerPos.y + height);
        }

        private bool IsMouseInsideDrawArea(Vector3[] drawAreaCorners)
        {
            return Input.mousePosition.x > drawAreaCorners[0].x && Input.mousePosition.y > drawAreaCorners[0].y &&
                   Input.mousePosition.x < drawAreaCorners[2].x && Input.mousePosition.y < drawAreaCorners[2].y;
        }
    }
}

