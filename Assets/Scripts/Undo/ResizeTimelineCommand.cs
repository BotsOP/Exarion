using Managers;

namespace Undo
{
    public class ResizeTimelineCommand : ICommand
    {
        private float sizeDelta;
        public ResizeTimelineCommand(float _sizeDelta)
        {
            sizeDelta = _sizeDelta;
        }
        public string GetCommandName()
        {
            return "ResizeTimelineCommand";
        }
        public void Undo()
        {
            EventSystem<float>.RaiseEvent(EventType.RESIZE_TIMELINE, sizeDelta);
        }
    }
}
