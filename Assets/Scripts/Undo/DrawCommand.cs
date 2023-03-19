using System.Collections.Generic;
using Drawing;
using UnityEngine;

namespace Undo
{
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
    
        public DrawCommand(ref Drawing.Drawing _drawer, Vector4 _collisionBox, PaintType _paintType, int _brushStokeID, float _lastTime, float _currentTime)
        {
            paintType = _paintType;
            collisionBox = _collisionBox;
            drawer = _drawer;
            lastTime = _lastTime;
            currentTime = _currentTime;
            brushStokeID = _brushStokeID;
            brushstrokStartID = drawer.brushStrokesID.Count - 1;

            int startID = drawer.brushStrokesID[brushstrokStartID].startID;
            int count = drawer.brushStrokesID[brushstrokStartID].endID - startID;

            brushStrokes = drawer.brushStrokes.GetRange(startID, count);
        }
    

        public void Execute()
        {
            drawer.brushStrokes.AddRange(brushStrokes);
            drawer.FinishedStroke(collisionBox, paintType, lastTime, currentTime);
            EventSystem<int, float, float>.RaiseEvent(EventType.FINISHED_STROKE, brushStokeID, lastTime, currentTime);
            drawer.RedrawAll();
        }
        public void Undo()
        {
            EventSystem<int>.RaiseEvent(EventType.REMOVE_CLIP, brushStokeID);
            EventSystem.RaiseEvent(EventType.CLEAR_HIGHLIGHT);
            drawer.RemoveStroke(brushstrokStartID);
        }
    }
}
