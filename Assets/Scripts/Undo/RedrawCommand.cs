using Drawing;
using UI;

namespace Undo
{
    public class RedrawCommand : ICommand
    {
        public float lastTime;
        public float currentTime;
        public float lastTimeOld;
        public float currentTimeOld;
        public BrushStrokeID brushStokeID;
        private int previousTimelineBar;
        private TimelineClip timelineClip;
        
        public RedrawCommand(TimelineClip _timelineClip)
        {
            timelineClip = _timelineClip;
            lastTimeOld = _timelineClip.lastLeftSideScaled;
            currentTimeOld = _timelineClip.lastRightSideScaled;
            previousTimelineBar = _timelineClip.previousTimelineBar;
            lastTime = _timelineClip.leftSideScaled;
            currentTime = _timelineClip.rightSideScaled;
            brushStokeID = _timelineClip.brushStrokeID;
        }
        
        public void Execute()
        {
            brushStokeID.lastTime = lastTime;
            brushStokeID.currentTime = currentTime;
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REDRAW_STROKE, brushStokeID);
            
            UpdateTimelineClip(lastTime, currentTime);
        }
        public void Undo()
        {
            brushStokeID.lastTime = lastTimeOld;
            brushStokeID.currentTime = currentTimeOld;
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REDRAW_STROKE, brushStokeID);
            
            UpdateTimelineClip(lastTimeOld, currentTimeOld);
        }
        
        public void UpdateTimelineClip(float _lastTime, float _currentTime)
        {
            timelineClip.leftSideScaled = _lastTime;
            timelineClip.rightSideScaled = _currentTime;
            EventSystem<TimelineClip, int>.RaiseEvent(EventType.UPDATE_CLIP, timelineClip, previousTimelineBar);
        }
    }
}
