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
            brushStrokeID = _timelineClip.brushStrokeID;
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
            EventSystem.RaiseEvent(EventType.CLEAR_HIGHLIGHT);
        }
    }
}
