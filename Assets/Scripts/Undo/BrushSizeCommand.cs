using System.Collections.Generic;
using Drawing;
using Managers;

namespace Undo
{
    public class BrushSizeCommand : ICommand
    {
        private List<BrushStrokeID> brushStrokeIDs;
        private List<float> values;

        public BrushSizeCommand(List<BrushStrokeID> _brushStrokeIDs, List<float> _values)
        {
            brushStrokeIDs = _brushStrokeIDs;
            values = _values;
        }

        public string GetCommandName()
        {
            return "BrushSizeCommand";
        }

        public void Undo()
        {
            EventSystem<List<BrushStrokeID>, List<float>>.RaiseEvent(EventType.CHANGE_BRUSH_SIZE, brushStrokeIDs, values);
        }
    }
}