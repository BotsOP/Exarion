using System.Collections.Generic;
using Drawing;
using Managers;
using UI;
using UnityEngine;
using EventType = Managers.EventType;

namespace Undo
{
    public class RotateCommand : ICommand
    {
        private float rotateAmount;
        private bool center;
        private List<BrushStrokeID> brushStrokeIDs;

        public RotateCommand(float _rotateAmount, bool _center, List<BrushStrokeID> _brushStrokeIDs)
        {
            rotateAmount = _rotateAmount;
            center = _center;
            brushStrokeIDs = _brushStrokeIDs;
        }

        public void Execute()
        {
            throw new System.NotImplementedException();
        }
        public void Undo()
        {
            EventSystem<float, bool, List<BrushStrokeID>>.RaiseEvent(EventType.ROTATE_STROKE, -rotateAmount, center, brushStrokeIDs);
        }
        public string GetCommandName()
        {
            return "RotateCommand";
        }
    }
}
