using System;
using System.Collections.Generic;
using System.Linq;
using Drawing;
using Managers;
using UnityEngine;
using EventType = Managers.EventType;

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
                redraw.brushStokeID.lastTime = redraw.clipTime.x;
                redraw.brushStokeID.currentTime = redraw.clipTime.y;
                redraw.UpdateTimelineClip(redraw.clipTime, redraw.timelineBar);
            }
            
            EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.REDRAW_STROKES, redrawCommands.Select(_command => _command.brushStokeID).ToList());
        }
        public void Undo()
        {
            Debug.Log($"redraw");
            //Updates all timeline clips
            foreach (var redraw in redrawCommands)
            {
                redraw.brushStokeID.lastTime = redraw.clipTimeOld.x;
                redraw.brushStokeID.currentTime = redraw.clipTimeOld.y;
                redraw.UpdateTimelineClip(redraw.clipTimeOld, redraw.previousTimelineBar);
            }
            
            //Updates all brushstrokes
            EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.REDRAW_STROKES, redrawCommands.Select(_command => _command.brushStokeID).ToList());
        }
    }
}
