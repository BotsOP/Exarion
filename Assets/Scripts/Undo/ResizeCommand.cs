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
        private List<BrushStrokeID> brushStrokeIDs;
        public ResizeCommand(float _resizeAmount, List<BrushStrokeID> _brushStrokeIDs)
        {
            resizeAmount = _resizeAmount;
            brushStrokeIDs = _brushStrokeIDs;
        }
        public void Execute()
        {
            throw new System.NotImplementedException();
        }
        public void Undo()
        {
            EventSystem<float, List<BrushStrokeID>>.RaiseEvent(EventType.RESIZE_STROKE, resizeAmount, brushStrokeIDs);
        }
        public string GetCommandName()
        {
            return "ResizeCommand";
        }
    }
}
