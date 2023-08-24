using System.Collections.Generic;
using Drawing;
using Managers;
using UI;
using UnityEngine;
using EventType = Managers.EventType;

namespace Undo
{
    public class MoveSetCommand : ICommand
    {
        private List<Vector2> moveDirs;
        private List<BrushStrokeID> brushStrokeIDs;
        
        public MoveSetCommand(List<Vector2> _moveDirs, List<BrushStrokeID> _brushStrokeIDs)
        {
            moveDirs = _moveDirs;
            brushStrokeIDs = _brushStrokeIDs;
        }
        public void Undo()
        {
            EventSystem<List<BrushStrokeID>, List<Vector2>>.RaiseEvent(EventType.MOVE_STROKE, brushStrokeIDs, moveDirs);
        }
        public string GetCommandName()
        {
            return "MoveCommand";
        }
    }
}
