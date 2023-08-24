using System.Collections.Generic;
using Drawing;
using Managers;

namespace Undo
{
    public class DrawOrderCommand : ICommand
    {
        private List<BrushStrokeID> brushStrokeIDs;
        private int amount;

        public DrawOrderCommand(List<BrushStrokeID> _brushStrokeIDs, int _amount)
        {
            brushStrokeIDs = _brushStrokeIDs;
            amount = _amount;
        }

        public string GetCommandName()
        {
            return "DrawOrderCommand";
        }

        public void Undo()
        {
            EventSystem<List<BrushStrokeID>, int>.RaiseEvent(EventType.CHANGE_DRAW_ORDER, brushStrokeIDs, -amount);
        }
    }
}