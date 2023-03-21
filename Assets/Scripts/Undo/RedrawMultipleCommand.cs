using System;
using System.Collections.Generic;

namespace Undo
{
    public class RedrawMultipleCommand : ICommand
    {
        private List<RedrawCommand> redrawCommands;
        
        public RedrawMultipleCommand(List<RedrawCommand> _redrawCommands)
        {
            redrawCommands = _redrawCommands;
        }
        public void Execute()
        {
            foreach (var redraw in redrawCommands)
            {
                redraw.Execute();
            }
        }
        public void Undo()
        {
            foreach (var redraw in redrawCommands)
            {
                redraw.Undo();
            }
        }
    }
}
