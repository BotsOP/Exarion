using System;
using System.Collections.Generic;
using System.Linq;
using Drawing;

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
                redraw.brushStokeID.lastTime = redraw.lastTime;
                redraw.brushStokeID.currentTime = redraw.currentTime;
                redraw.UpdateTimelineClip(redraw.lastTime, redraw.currentTime);
            }
            
            EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.REDRAW_STROKES, redrawCommands.Select(_command => _command.brushStokeID).ToList());
        }
        public void Undo()
        {
            foreach (var redraw in redrawCommands)
            {
                redraw.brushStokeID.lastTime = redraw.lastTimeOld;
                redraw.brushStokeID.currentTime = redraw.currentTimeOld;
                redraw.UpdateTimelineClip(redraw.lastTimeOld, redraw.currentTimeOld);
            }
            
            EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.REDRAW_STROKES, redrawCommands.Select(_command => _command.brushStokeID).ToList());
        }
    }
}
