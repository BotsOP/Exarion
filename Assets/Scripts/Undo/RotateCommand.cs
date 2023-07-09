﻿using System.Collections.Generic;
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
        private List<BrushStrokeID> brushStrokeIDs;
        
        public RotateCommand(float _rotateAmount, List<BrushStrokeID> _brushStrokeIDs)
        {
            rotateAmount = _rotateAmount;
            brushStrokeIDs = _brushStrokeIDs;
        }
        public void Execute()
        {
            throw new System.NotImplementedException();
        }
        public void Undo()
        {
            EventSystem<float, List<BrushStrokeID>>.RaiseEvent(EventType.ROTATE_STROKE, -rotateAmount, brushStrokeIDs);
        }
        public string GetCommandName()
        {
            return "RotateCommand";
        }
    }
}
