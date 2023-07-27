using System;
using System.Collections.Generic;

namespace Undo
{
    public class UnGroupMultipleCommand : ICommand
    {
        private List<UnGroupCommand> unGroupCommands;
        public UnGroupMultipleCommand(List<UnGroupCommand> _unGroupCommands)
        {
            unGroupCommands = _unGroupCommands;
        }
        public string GetCommandName()
        {
            return "UnGroupMultipleCommand";
        }
        public void Execute()
        {
            throw new NotImplementedException();
        }
        public void Undo()
        {
            foreach (var unGroupCommand in unGroupCommands)
            {
                unGroupCommand.Undo();
            }
        }
    }
}
