using System.Collections.Generic;
using Drawing;
using Managers;
using UI;
using UnityEngine;
using EventType = Managers.EventType;

namespace Undo
{
    public class ResizeCommand : ICommand
    {
        private float resizeAmount;
        private bool center;
        private List<BrushStrokeID> brushStrokeIDs;

        public ResizeCommand(float _resizeAmount, bool _center, List<BrushStrokeID> _brushStrokeIDs)
        {
            resizeAmount = _resizeAmount;
            center = _center;
            brushStrokeIDs = _brushStrokeIDs;
        }

        public void Execute()
        {
            throw new System.NotImplementedException();
        }
        public void Undo()
        {
            EventSystem<float, bool, List<BrushStrokeID>>.RaiseEvent(EventType.RESIZE_STROKE, resizeAmount, UIManager.center, brushStrokeIDs);
        }
        public string GetCommandName()
        {
            return "ResizeCommand";
        }
    }
}
