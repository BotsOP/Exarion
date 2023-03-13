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
    
    public DrawCommand(ref Drawing.Drawing drawer, Vector4 collisionBox, PaintType paintType)
    {
        this.paintType = paintType;
        this.collisionBox = collisionBox;
        this.drawer = drawer;
        brushstrokStartID = drawer.brushStrokesID.Count - 1;

        int startID = drawer.brushStrokesID[brushstrokStartID].startID;
        int count = drawer.brushStrokesID[brushstrokStartID].endID - startID;

        brushStrokes = drawer.brushStrokes.GetRange(startID, count);
    }

    public void Execute()
    {
        drawer.brushStrokes.AddRange(brushStrokes);
        drawer.FinishedStroke(collisionBox, paintType);
        drawer.RedrawAll();
    }
    public void Undo()
    {
        drawer.RemoveStroke(brushstrokStartID);
    }
}
