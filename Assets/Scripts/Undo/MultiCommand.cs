using System;
using System.Collections.Generic;

namespace Undo
{
    public class MultiCommand : ICommand
    {
        private List<ICommand> commands;
        public MultiCommand(List<ICommand> _commands)
        {
            commands = _commands;
        }
        public string GetCommandName()
        {
            string allNames = "";
            foreach (var command in commands)
            {
                allNames += command.GetCommandName() + " ";
            }
            return allNames;
        }

        public void Undo()
        {
            foreach (var command in commands)
            {
                command.Undo();
            }
        }
    }
}
