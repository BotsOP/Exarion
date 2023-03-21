using System.Collections.Generic;
using Drawing;
using UnityEngine;

namespace Undo
{
    public class DrawCommand : ICommand
    {
        private List<BrushStroke> brushStrokes;
        private BrushStrokeID brushStrokeID;

        public DrawCommand(List<BrushStroke> _brushStrokes, BrushStrokeID _brushStrokeID)
        {
            brushStrokeID = _brushStrokeID;
            brushStrokes = _brushStrokes;
        }

        public void Execute()
        {
            EventSystem<List<BrushStroke>, BrushStrokeID>.RaiseEvent(EventType.ADD_STROKE, brushStrokes, brushStrokeID);
        }
        public void Undo()
        {
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REMOVE_STROKE, brushStrokeID);
            EventSystem.RaiseEvent(EventType.CLEAR_HIGHLIGHT);
        }
    }
}
