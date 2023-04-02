using System;
using System.Collections.Generic;
using System.Linq;
using Drawing;
using UI;


namespace Undo
{
    //Dont use a deleteClipCommand list but just use a list of bruhstrokeIDs and a list of List<brushStrokes>
    public class DeleteClipMultipleCommand : ICommand
    {
        private List<List<BrushStroke>> brushStrokesList;
        private List<BrushStrokeID> brushStrokeIDs;
        private List<TimelineClip> timelineClips;

        public DeleteClipMultipleCommand(List<List<BrushStroke>> _brushStrokesList, List<TimelineClip> _timelineClips)
        {
            brushStrokesList = _brushStrokesList;
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
            EventSystem<List<List<BrushStroke>>, List<BrushStrokeID>, List<TimelineClip>>.RaiseEvent(
                EventType.ADD_STROKE, brushStrokesList, brushStrokeIDs, timelineClips);
        }
    }
}