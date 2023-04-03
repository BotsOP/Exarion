using System;
using System.Collections.Generic;
using System.Linq;
using Drawing;
using UI;


namespace Undo
{
    public class DeleteClipMultipleCommand : ICommand
    {
        private List<BrushStrokeID> brushStrokeIDs;
        private List<TimelineClip> timelineClips;

        public DeleteClipMultipleCommand(List<TimelineClip> _timelineClips)
        {
            brushStrokeIDs = _timelineClips.Select(_clip => _clip.brushStrokeID).ToList();
            timelineClips = _timelineClips;
        }

        public void Execute()
        {
            EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.REMOVE_STROKE, brushStrokeIDs);
            EventSystem<List<TimelineClip>>.RaiseEvent(EventType.REMOVE_STROKE, timelineClips);
            EventSystem.RaiseEvent(EventType.CLEAR_HIGHLIGHT);
        }

        public void Undo()
        {
            EventSystem<List<BrushStrokeID>, List<TimelineClip>>.RaiseEvent(
                EventType.ADD_STROKE, brushStrokeIDs, timelineClips);
        }
    }
}