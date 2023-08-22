using System.Collections.Generic;
using Drawing;
using Managers;
using UI;
using UnityEngine;
using EventType = Managers.EventType;

namespace Undo
{
    public class ResizeSetCommand : ICommand
    {
        private List<float> resizeAmounts;
        private List<BrushStrokeID> brushStrokeIDs;

        public ResizeSetCommand(List<BrushStrokeID> _brushStrokeIDs, List<float> _resizeAmounts)
        {
            resizeAmounts = _resizeAmounts;
            brushStrokeIDs = _brushStrokeIDs;
        }
        
        public void Undo()
        {
            EventSystem<List<BrushStrokeID>, List<float>>.RaiseEvent(EventType.RESIZE_STROKE, brushStrokeIDs, resizeAmounts);
        }
        public string GetCommandName()
        {
            return "ResizeCommand";
        }
    }
}
