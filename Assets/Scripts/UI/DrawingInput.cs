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
        private Vector2 lastMousePos;

        public DrawingInput(Camera viewCam, Camera displayCam, float scrollZoomSensitivity, float moveSensitivity)
        {
            this.viewCam = viewCam;
            this.displayCam = displayCam;
            this.scrollZoomSensitivity = scrollZoomSensitivity;
            this.moveSensitivity = moveSensitivity;
        }
        
        public void UpdateDrawingInput(Vector3[] drawAreaCorners, Vector3[] displayAreaCorners, Vector2 camPos, float camZoom)
        {
            Vector2 mousePos = Input.mousePosition;
            bool isMouseInsideDrawArea = IsMouseInsideDrawArea(drawAreaCorners);
            bool isMouseInsideDisplayArea = IsMouseInsideDrawArea(displayAreaCorners);
            
            if (isMouseInsideDrawArea)
            {
                DrawInput(drawAreaCorners, camPos, camZoom, mousePos);
                MoveCamera(viewCam);
            }
            else if (isMouseInsideDisplayArea)
            {
                MoveCamera(displayCam);
            }
            
            if(Input.GetMouseButtonUp(0) && mouseIsDrawing || !isMouseInsideDrawArea && mouseIsDrawing)
            {
                EventSystem.RaiseEvent(EventType.FINISHED_STROKE);
                mouseIsDrawing = false;
            }

            lastMousePos = Input.mousePosition;
        }
        private void DrawInput(Vector3[] drawAreaCorners, Vector2 camPos, float camZoom, Vector2 mousePos)
        {
            if (Input.GetMouseButton(0))
            {
                Vector4 drawCorners = GetScaledDrawingCorners(camPos, camZoom, drawAreaCorners);
                float mousePosX = mousePos.x.Remap(drawCorners.x, drawCorners.z, 0, 2048);
                float mousePosY = mousePos.y.Remap(drawCorners.y, drawCorners.w, 0, 2048);
                mousePos = new Vector2(mousePosX, mousePosY);
                EventSystem<Vector2>.RaiseEvent(EventType.DRAW, mousePos);

                mouseIsDrawing = true;
            }
        }
        private void MoveCamera(Camera cam)
        {

            if (Input.mouseScrollDelta.y != 0)
            {
                cam.orthographicSize -= Input.mouseScrollDelta.y * scrollZoomSensitivity;
            }
            if (Input.GetMouseButton(1))
            {
                Vector3 pos = new Vector3(
                    (Input.mousePosition.x - lastMousePos.x) / Screen.width * moveSensitivity,
                    (Input.mousePosition.y - lastMousePos.y) / Screen.width * moveSensitivity,
                    0);
                cam.transform.position -= pos;
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

