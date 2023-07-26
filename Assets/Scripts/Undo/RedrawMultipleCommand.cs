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

        public RedrawMultipleCommand(List<TimelineClip> _clips)
        {
            timelineClips = _clips;
            clipTimeOld = _clips.Select(_clip => _clip.clipTimeOld).ToList();
        }
        public void Execute()
        {
            
        }
        public void Undo()
        {
            List<BrushStrokeID> redrawStrokes = new List<BrushStrokeID>();
            for (int i = 0; i < timelineClips.Count; i++)
            {
                var clip = timelineClips[i];
                clip.SetTime(clipTimeOld[i]);
                clip.ClipTime = clipTimeOld[i];
                EventSystem<TimelineClip>.RaiseEvent(EventType.UPDATE_CLIP, clip);

                redrawStrokes.AddRange(clip.GetBrushStrokeIDs());
            }
            EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.REDRAW_STROKES, redrawStrokes);
        }
        public string GetCommandName()
        {
            return "RedrawMultipleCommand";
        }
    }
}
