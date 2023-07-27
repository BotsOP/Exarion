using System.Collections.Generic;
using Drawing;
using Managers;
using UI;
using UnityEngine;
using EventType = Managers.EventType;

namespace Undo
{
    public class DrawCommand : ICommand
    {
        private BrushStrokeID brushStrokeID;
        private TimelineClip timelineClip;

        public DrawCommand(TimelineClip _timelineClip)
        {
            //DrawCommand is only used when drawing one stroke if you can draw multiple strokes at the same time change this
            brushStrokeID = _timelineClip.GetBrushStrokeIDs()[0];
            timelineClip = _timelineClip;
        }

        public void Execute()
        {
            //Add timeline clip ADD_STROKE event here too
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.ADD_STROKE, brushStrokeID);
            EventSystem<TimelineClip>.RaiseEvent(EventType.ADD_STROKE, timelineClip);
        }
        public void Undo()
        {
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REMOVE_STROKE, brushStrokeID);
            EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
        }
        public string GetCommandName()
        {
            return "DrawCommand";
        }
    }
}
