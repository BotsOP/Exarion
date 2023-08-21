using System.Collections.Generic;
using Drawing;
using Managers;
using UI;
using UnityEngine;
using EventType = Managers.EventType;

namespace Undo
{
    public class MoveCommand : ICommand
    {
        private Vector2 moveDir;
        private List<BrushStrokeID> brushStrokeIDs;
        
        public MoveCommand(Vector2 _moveDir, List<BrushStrokeID> _brushStrokeIDs)
        {
            moveDir = _moveDir;
            brushStrokeIDs = _brushStrokeIDs;
        }
        public void Undo()
        {
            EventSystem<Vector2, List<BrushStrokeID>>.RaiseEvent(EventType.MOVE_STROKE, -moveDir, brushStrokeIDs);
        }
        public string GetCommandName()
        {
            return "MoveCommand";
        }
    }
}
