using Drawing;
using Managers;
using UI;
using UnityEngine;
using EventType = Managers.EventType;

namespace Undo
{
    public class RedrawCommand : ICommand
    {
        public Vector2 clipTime;
        public Vector2 clipTimeOld;
        public BrushStrokeID brushStokeID;
        public int previousTimelineBar;
        public int timelineBar;
        private TimelineClip timelineClip;
        
        public RedrawCommand(TimelineClip _timelineClip)
        {
            timelineClip = _timelineClip;
            clipTime = _timelineClip.ClipTime;
            clipTimeOld = _timelineClip.clipTimeOld;
            previousTimelineBar = _timelineClip.previousBar;
            timelineBar = _timelineClip.currentBar;
            brushStokeID = _timelineClip.brushStrokeID;
        }
        
        public void Execute()
        {
            brushStokeID.lastTime = clipTime.x;
            brushStokeID.currentTime = clipTime.y;
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REDRAW_STROKE, brushStokeID);
            
            UpdateTimelineClip(clipTime, timelineBar);
        }
        public void Undo()
        {
            brushStokeID.lastTime = clipTimeOld.x;
            brushStokeID.currentTime = clipTimeOld.y;
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REDRAW_STROKE, brushStokeID);
            
            UpdateTimelineClip(clipTimeOld, previousTimelineBar);
        }
        
        public void UpdateTimelineClip(Vector2 _clipTime, int _timelineBar)
        {
            timelineClip.ClipTime = _clipTime;
            EventSystem<TimelineClip, int>.RaiseEvent(EventType.UPDATE_CLIP, timelineClip, _timelineBar);
        }
    }
}
