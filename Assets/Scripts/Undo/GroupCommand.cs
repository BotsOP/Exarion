using System.Collections.Generic;
using Managers;
using UI;

namespace Undo
{
    public class GroupCommand : ICommand
    {
        private TimelineClip clip;
        
        public GroupCommand(TimelineClip _clip)
        {
            clip = _clip;
        }
        public string GetCommandName()
        {
            return "GroupCommand";
        }
        public void Execute()
        {
            throw new System.NotImplementedException();
        }
        public void Undo()
        {
            EventSystem<List<TimelineClip>>.RaiseEvent(EventType.UNGROUP_CLIPS, new List<TimelineClip> { clip });
        }
    }
}
