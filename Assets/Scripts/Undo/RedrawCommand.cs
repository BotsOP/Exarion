namespace Undo
{
    public class RedrawCommand : ICommand
    {
        private float lastTime;
        private float currentTime;
        private float lastTimeOld;
        private float currentTimeOld;
        private int timelineBar;
        private int brushStokeID;
        public RedrawCommand(int brushStokeID, float lastTime, float currentTime, float lastTimeOld, float currentTimeOld, int _timelineBar)
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
            EventSystem<int, float, float>.RaiseEvent(EventType.REDRAW_STROKE, brushStokeID, lastTime, currentTime);
            EventSystem<int, float, float, int>.RaiseEvent(EventType.UPDATE_CLIP, brushStokeID, lastTime, currentTime, timelineBar);
        }
        public void Undo()
        {
            EventSystem<int, float, float>.RaiseEvent(EventType.REDRAW_STROKE, brushStokeID, lastTimeOld, currentTimeOld);
            EventSystem<int, float, float, int>.RaiseEvent(EventType.UPDATE_CLIP, brushStokeID, lastTimeOld, currentTimeOld, timelineBar);
        }
    }
}
