using Managers;
using UnityEngine;

namespace UI
{
    public class DrawingInput
    {
        private bool mouseWasDrawing;

        public void UpdateDrawingInput(Vector3[] drawAreaCorners, Vector2 camPos, float camZoom)
        {
            Vector2 mousePos = Input.mousePosition;
            if (Input.GetMouseButton(0) && IsMouseInsideDrawArea(mousePos, drawAreaCorners))
            {
                mouseWasDrawing = true;
                Vector4 drawCorners = GetScaledDrawingCorners(camPos, camZoom, drawAreaCorners);
            
            
                float mousePosX = mousePos.x.Remap(drawCorners.x, drawCorners.z, 0, 2048);
                float mousePosY = mousePos.y.Remap(drawCorners.y, drawCorners.w, 0, 2048);
                mousePos = new Vector2(mousePosX, mousePosY);
                EventSystem<Vector2>.RaiseEvent(EventType.DRAW, mousePos);
            }
            else if (Input.GetMouseButtonUp(0) && mouseWasDrawing || !IsMouseInsideDrawArea(mousePos, drawAreaCorners) && mouseWasDrawing)
            {
                EventSystem.RaiseEvent(EventType.FINISHED_STROKE);
                mouseWasDrawing = false;
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

        private bool IsMouseInsideDrawArea(Vector2 mousePos, Vector3[] drawAreaCorners)
        {
            return mousePos.x > drawAreaCorners[0].x && mousePos.y > drawAreaCorners[0].y &&
                   mousePos.x < drawAreaCorners[2].x && mousePos.y < drawAreaCorners[2].y;
        }
    }
}

