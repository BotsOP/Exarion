using System.Collections;
using System.Collections.Generic;
using Drawing;
using UnityEngine;

public class DrawingInput : MonoBehaviour
{
    [SerializeField] private UIManager UIManager;
    [SerializeField] private int brushStrokeToRedraw;
    [SerializeField] private float startTime;
    [SerializeField] private float endTime;
    [SerializeField] private float brushSize;
    private Vector3[] drawAreaCorners;
    private bool mouseWasDrawing;
    
    

    void Update()
    {
        if (UIManager.isFullView)
        {
            drawAreaCorners = new Vector3[4];
            UIManager.paintBoard.rectTransform.GetWorldCorners(drawAreaCorners);
        }
        else
        {
            drawAreaCorners = new Vector3[4];
            UIManager.paintBoard2.rectTransform.GetWorldCorners(drawAreaCorners);
        }
        
        
        Vector2 mousePos = Input.mousePosition;
        if (Input.GetMouseButton(0) && IsMouseInsideDrawArea(mousePos))
        {
            mouseWasDrawing = true;
            float mousePosX = ExtensionMethods.Remap(mousePos.x, drawAreaCorners[0].x, drawAreaCorners[2].x, 0, 2048);
            float mousePosY = ExtensionMethods.Remap(mousePos.y, drawAreaCorners[0].y, drawAreaCorners[2].y, 0, 2048);
            mousePos = new Vector2(mousePosX, mousePosY);
            EventSystem<Vector2>.RaiseEvent(EventType.DRAW, mousePos);
        }
        else if (Input.GetMouseButtonUp(0) && mouseWasDrawing)
        {
            EventSystem.RaiseEvent(EventType.STOPPED_DRAWING);
            mouseWasDrawing = false;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            EventSystem<int, float, float>.RaiseEvent(EventType.REDRAW_STROKE, brushStrokeToRedraw, startTime, endTime);
        }
        
        EventSystem<float>.RaiseEvent(EventType.CHANGE_BRUSH_SIZE, brushSize);
    }

    

    private bool IsMouseInsideDrawArea(Vector2 mousePos)
    {
        if (mousePos.x > drawAreaCorners[0].x && mousePos.y > drawAreaCorners[0].y &&
            mousePos.x < drawAreaCorners[2].x && mousePos.y < drawAreaCorners[2].y)
        {
            return true;
        }
        return false;
    }
}
