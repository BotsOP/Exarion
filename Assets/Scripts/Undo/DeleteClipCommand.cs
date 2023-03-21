using System.Collections.Generic;
using Drawing;

namespace Undo
{
    public class DeleteClipCommand : ICommand
    {
        private List<BrushStroke> brushStrokes;
        private BrushStrokeID brushStrokeID;

        public DeleteClipCommand(List<BrushStroke> _brushStrokes, BrushStrokeID _brushStrokeID)
        {
            brushStrokeID = _brushStrokeID;
            brushStrokes = _brushStrokes;
        }

        public void Execute()
        {
            EventSystem<BrushStrokeID>.RaiseEvent(EventType.REMOVE_STROKE, brushStrokeID);
            EventSystem.RaiseEvent(EventType.CLEAR_HIGHLIGHT);
        }
        public void Undo()
        {
            EventSystem<List<BrushStroke>, BrushStrokeID>.RaiseEvent(EventType.ADD_STROKE, brushStrokes, brushStrokeID);
        }
    }
}
