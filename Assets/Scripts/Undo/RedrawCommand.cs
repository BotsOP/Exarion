using Drawing;

namespace Undo
{
    public class RedrawCommand : ICommand
    {
        private float lastTime;
        private float currentTime;
        private float lastTimeOld;
        private float currentTimeOld;
        private int timelineBar;
        private BrushStrokeID brushStokeID;
        public RedrawCommand(BrushStrokeID brushStokeID, float lastTime, float currentTime, float lastTimeOld, float currentTimeOld, int _timelineBar)
        {
            this.lastTime = lastTime;
            this.currentTime = currentTime;
            this.lastTimeOld = lastTimeOld;
            this.currentTimeOld = currentTimeOld;
            this.brushStokeID = brushStokeID;
            timelineBar = _timelineBar;
        }
        public void Execute()
        {
            EventSystem<BrushStrokeID, float, float>.RaiseEvent(EventType.REDRAW_STROKE, brushStokeID, lastTime, currentTime);
            EventSystem<BrushStrokeID, float, float, int>.RaiseEvent(EventType.UPDATE_CLIP, brushStokeID, lastTime, currentTime, timelineBar);
        }
        public void Undo()
        {
            EventSystem<BrushStrokeID, float, float>.RaiseEvent(EventType.REDRAW_STROKE, brushStokeID, lastTimeOld, currentTimeOld);
            EventSystem<BrushStrokeID, float, float, int>.RaiseEvent(EventType.UPDATE_CLIP, brushStokeID, lastTimeOld, currentTimeOld, timelineBar);
        }
    }
}
