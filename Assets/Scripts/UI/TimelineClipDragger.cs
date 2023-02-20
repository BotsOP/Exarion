using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TimelineClipDragger : MouseManipulator
{
    private Vector2 startPos;
    private Vector2 elementStartPos;
    private bool isActive;
    private float initialOffset;

    public TimelineClipDragger()
    {
        activators.Add(new ManipulatorActivationFilter{ button = MouseButton.LeftMouse });
        initialOffset = -1;
        isActive = false;
    }
    
    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        target.RegisterCallback<MouseUpEvent>(OnMouseUp);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
    }

    protected void OnMouseDown(MouseDownEvent e)
    {
        if (CanStartManipulation(e))
        {
            if (initialOffset == -1.0f)
            {
                initialOffset = target.layout.x;
            }

            startPos = e.localMousePosition;
            isActive = true;
            target.CaptureMouse();
            e.StopPropagation();
        }
    }
    protected void OnMouseMove(MouseMoveEvent e)
    {
        if (!isActive || !target.HasMouseCapture()) return;
        
        Vector2 diff = e.localMousePosition - startPos;
//        Debug.Log($"{e.localMousePosition.x} {diff.x} {startPos} {target.layout.x}");

        target.style.left = target.layout.x + diff.x - initialOffset;
    }
    protected void OnMouseUp(MouseUpEvent e)
    {
        if (!isActive || !target.HasMouseCapture() || !CanStartManipulation(e)) return;
        
        isActive = false;
        target.ReleaseMouse();
        e.StopPropagation();
    }
}
