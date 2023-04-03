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
        public int previousTimelineBar;
        public int timelineBar;
        private TimelineClip timelineClip;
        
        public RedrawCommand(TimelineClip _timelineClip)
        {
            timelineClip = _timelineClip;
            lastTimeOld = _timelineClip.lastLeftSideScaled;
            currentTimeOld = _timelineClip.lastRightSideScaled;
            previousTimelineBar = _timelineClip.previousTimelineBar;
            timelineBar = _timelineClip.currentBar;
            lastTime = _timelineClip.leftSideScaled;
            currentTime = _timelineClip.rightSideScaled;
            brushStokeID = _timelineClip.brushStrokeID;
        }
        
        public void Execute()
        {
            brushStokeID.lastTime = lastTime;
            brushStokeID.currentTime = currentTime;
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REDRAW_STROKE, brushStokeID);
            
            UpdateTimelineClip(lastTime, currentTime, timelineBar);
        }
        public void Undo()
        {
            brushStokeID.lastTime = lastTimeOld;
            brushStokeID.currentTime = currentTimeOld;
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REDRAW_STROKE, brushStokeID);
            
            UpdateTimelineClip(lastTimeOld, currentTimeOld, previousTimelineBar);
        }
        
        public void UpdateTimelineClip(float _lastTime, float _currentTime, int _timelineBar)
        {
            timelineClip.leftSideScaled = _lastTime;
            timelineClip.rightSideScaled = _currentTime;
            EventSystem<TimelineClip, int>.RaiseEvent(EventType.UPDATE_CLIP, timelineClip, _timelineBar);
        }
    }
}
