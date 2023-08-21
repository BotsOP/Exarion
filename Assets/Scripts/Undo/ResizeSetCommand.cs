﻿using System.Collections.Generic;
using Drawing;
using Managers;
using UI;
using UnityEngine;
using EventType = Managers.EventType;

namespace Undo
{
    public class ResizeSetCommand : ICommand
    {
        private float resizeAmount;
        private bool center;
        private List<BrushStrokeID> brushStrokeIDs;

        public ResizeSetCommand(float _resizeAmount, bool _center, List<BrushStrokeID> _brushStrokeIDs)
        {
            resizeAmount = _resizeAmount;
            center = _center;
            brushStrokeIDs = _brushStrokeIDs;
        }
        
        public void Undo()
        {
            EventSystem<float, bool, List<BrushStrokeID>>.RaiseEvent(EventType.RESIZE_STROKE, resizeAmount, center, brushStrokeIDs);
        }
        public string GetCommandName()
        {
            return "ResizeCommand";
        }
    }
}
