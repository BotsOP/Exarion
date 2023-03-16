using System.Collections;
using System.Collections.Generic;
using Drawing;
using UnityEngine;

public class DrawCommand : ICommand
{
    private Drawing.Drawing drawer;
    private int brushstrokStartID;
    private List<BrushStroke> brushStrokes;
    private Vector4 collisionBox;
    private PaintType paintType;
    private float lastTime;
    private float currentTime;
    private int brushStokeID;
    
    public DrawCommand(ref Drawing.Drawing drawer, Vector4 collisionBox, PaintType paintType, int brushStokeID, float lastTime, float currentTime)
    {
        this.paintType = paintType;
        this.collisionBox = collisionBox;
        this.drawer = drawer;
        this.lastTime = lastTime;
        this.currentTime = currentTime;
        this.brushStokeID = brushStokeID;
        brushstrokStartID = drawer.brushStrokesID.Count - 1;

        int startID = drawer.brushStrokesID[brushstrokStartID].startID;
        int count = drawer.brushStrokesID[brushstrokStartID].endID - startID;

        brushStrokes = drawer.brushStrokes.GetRange(startID, count);
    }

    public void Execute()
    {
        drawer.brushStrokes.AddRange(brushStrokes);
        drawer.FinishedStroke(collisionBox, paintType, lastTime, currentTime);
        EventSystem<int, float, float>.RaiseEvent(EventType.STOPPED_DRAWING, brushStokeID, lastTime, currentTime);
        drawer.RedrawAll();
    }
    public void Undo()
    {
        EventSystem<int>.RaiseEvent(EventType.REMOVE_CLIP, brushStokeID);
        drawer.RemoveStroke(brushstrokStartID);
    }
}
