using System.Collections.Generic;
using Managers;
using UI;

namespace Undo
{
    public class UnGroupCommand : ICommand
    {
        private List<TimelineClip> clips;
        public UnGroupCommand(List<TimelineClip> _clips)
        {
            clips = _clips;
        }
        public string GetCommandName()
        {
            return "UnGroupCommand";
        }
        public void Execute()
        {
            throw new System.NotImplementedException();
        }
        public void Undo()
        {
            EventSystem<List<TimelineClip>>.RaiseEvent(EventType.GROUP_CLIPS, clips);
        }
    }
}
