using System.Collections.Generic;
using Drawing;
using Managers;
using UI;
using UnityEngine;
using EventType = Managers.EventType;

namespace Undo
{
    public class RotateSetCommand : ICommand
    {
        private List<float> rotateAmounts;
        private bool center;
        private List<BrushStrokeID> brushStrokeIDs;

        public RotateSetCommand(List<BrushStrokeID> _brushStrokeIDs, List<float> _rotateAmounts)
        {
            rotateAmounts = _rotateAmounts;
            brushStrokeIDs = _brushStrokeIDs;
        }
        public void Undo()
        {
            EventSystem<List<BrushStrokeID>, List<float>>.RaiseEvent(EventType.ROTATE_STROKE, brushStrokeIDs, rotateAmounts);
        }
        public string GetCommandName()
        {
            return "RotateCommand";
        }
    }
}
