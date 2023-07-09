using System.Collections.Generic;
using Drawing;
using Managers;
using UnityEngine;
using EventType = Managers.EventType;

namespace Undo
{
    public class DeleteClipCommand : ICommand
    {
        public BrushStrokeID brushStrokeID;

        public DeleteClipCommand(BrushStrokeID _brushStrokeID)
        {
            Debug.LogWarning("didnt update this command properly");
            brushStrokeID = _brushStrokeID;
        }

        public void Execute()
        {
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REMOVE_STROKE, brushStrokeID);
            EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
        }
        public void Undo()
        {
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.ADD_STROKE, brushStrokeID);
        }
        public string GetCommandName()
        {
            return "DeleteClipCommand";
        }
    }
}
