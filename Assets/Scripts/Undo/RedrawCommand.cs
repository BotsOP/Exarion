namespace Undo
{
    public class RedrawCommand : ICommand
    {
        private float lastTime;
        private float currentTime;
        private float lastTimeOld;
        private float currentTimeOld;
        private int brushStokeID;
        public RedrawCommand(int brushStokeID, float lastTime, float currentTime, float lastTimeOld, float currentTimeOld)
        {
            this.lastTime = lastTime;
            this.currentTime = currentTime;
            this.lastTimeOld = lastTimeOld;
            this.currentTimeOld = currentTimeOld;
            this.brushStokeID = brushStokeID;
        }
        public void Execute()
        {
            EventSystem<int, float, float>.RaiseEvent(EventType.REDRAW_STROKE, brushStokeID, lastTime, currentTime);
            EventSystem<int, float, float>.RaiseEvent(EventType.UPDATE_CLIP, brushStokeID, lastTime, currentTime);
        }
        public void Undo()
        {
            EventSystem<int, float, float>.RaiseEvent(EventType.REDRAW_STROKE, brushStokeID, lastTimeOld, currentTimeOld);
            EventSystem<int, float, float>.RaiseEvent(EventType.UPDATE_CLIP, brushStokeID, lastTimeOld, currentTimeOld);
        }
    }
}
