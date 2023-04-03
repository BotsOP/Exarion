using System.Collections.Generic;
using Drawing;
using UnityEngine;

namespace Undo
{
    public class DrawCommand : ICommand
    {
        private BrushStrokeID brushStrokeID;

        public DrawCommand(BrushStrokeID _brushStrokeID)
        {
            brushStrokeID = _brushStrokeID;
        }

        public void Execute()
        {
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.ADD_STROKE, brushStrokeID);
        }
        public void Undo()
        {
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REMOVE_STROKE, brushStrokeID);
            EventSystem.RaiseEvent(EventType.CLEAR_HIGHLIGHT);
        }
    }
}
