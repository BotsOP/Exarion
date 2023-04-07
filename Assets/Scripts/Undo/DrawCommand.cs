using System.Collections.Generic;
using Drawing;
using Managers;
using UnityEngine;
using EventType = Managers.EventType;

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
            //Add timeline clip ADD_STROKE event here too
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.ADD_STROKE, brushStrokeID);
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.FINISHED_STROKE, brushStrokeID);
        }
        public void Undo()
        {
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REMOVE_STROKE, brushStrokeID);
            EventSystem.RaiseEvent(EventType.CLEAR_HIGHLIGHT);
        }
    }
}
