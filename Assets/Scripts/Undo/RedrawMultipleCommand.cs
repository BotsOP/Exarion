using System;
using System.Collections.Generic;
using System.Linq;
using Drawing;
using Managers;
using UI;
using UnityEngine;
using EventType = Managers.EventType;

namespace Undo
{
    public class RedrawMultipleCommand : ICommand
    {
        private List<TimelineClip> timelineClips;
        private List<Vector2> clipTimeOld;
        private List<int> timelineBars;

        public RedrawMultipleCommand(List<TimelineClip> _clips)
        {
            timelineClips = _clips;
            clipTimeOld = _clips.Select(_clip => _clip.clipTimeOld).ToList();
            timelineBars = _clips.Select(_clip => _clip.previousBar).ToList();
        }
        public void Execute()
        {
            
        }
        public void Undo()
        {
            List<BrushStrokeID> redrawStrokes = timelineClips.SelectMany(clip => clip.GetBrushStrokeIDs()).ToList();
            for (int i = 0; i < timelineClips.Count; i++)
            {
                var clip = timelineClips[i];
                clip.SetTime(clipTimeOld[i]);
                clip.ClipTime = clipTimeOld[i];
                EventSystem<TimelineClip, int>.RaiseEvent(EventType.UPDATE_CLIP, clip, timelineBars[i]);
            }
            EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.REDRAW_STROKES, redrawStrokes);
        }
        public string GetCommandName()
        {
            return "RedrawMultipleCommand";
        }
    }
}
