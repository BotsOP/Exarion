using System;
using System.Collections;
using System.Collections.Generic;
using Drawing;
using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public  enum MouseAction
    {
        Nothing,
        GrabbedClip,
        ResizeClipLeft,
        ResizeClipRight
    }
    public  enum ClipType
    {
        Single,
        Group,
        Base,
    }
    public class TimelineClip
    {
        public Vector2 ClipTime
        {
            get
            {
                float lastTime = Corners[0].x.Remap(TimelineBarCorners[0].x, TimelineBarCorners[2].x, 0, 1);
                float currentTime = Corners[2].x.Remap(TimelineBarCorners[0].x, TimelineBarCorners[2].x, 0, 1);
                return new Vector2(lastTime, currentTime);
            }
            set
            {
                var sizeDelta = rect.sizeDelta;
                var position = rect.position;
            
                float lastTimePos = value.x.Remap(0, 1, TimelineBarCorners[0].x, TimelineBarCorners[2].x);
                float currentTimePos = value.y.Remap(0, 1, TimelineBarCorners[0].x, TimelineBarCorners[2].x);
                float clipLength = currentTimePos - lastTimePos;
                sizeDelta = new Vector2(clipLength, sizeDelta.y);
                rect.sizeDelta = sizeDelta;

                if (rect.pivot.x == 0)
                {
                    position = new Vector3(lastTimePos, position.y, position.z);
                    rect.position = position;
                }
                else
                {
                    position = new Vector3(currentTimePos, position.y, position.z);
                    rect.position = position;
                }
            }
        }

        public Vector2 clipTimeOld;
        public int previousBar;
        public bool hover;
        public List<BrushStrokeID> selectedBrushStrokes = new List<BrushStrokeID>();

        public RectTransform rect;
        public MouseAction mouseAction;
        public RawImage rawImage;
        public int currentBar;
        private float startMousePos;
        private float leftMostPos;
        private float rightMostPos;
        private float oldLeftPos;
        private float oldRightPos;
        private RectTransform timelineBarRect;
        private RectTransform timelineAreaRect;
    
        private float mouseOffset;
        private float clipHandle = 5;
        private float minumunWidth = 10;
        
        private readonly Vector3[] corners;
        public Vector3[] Corners
        {
            get {
                rect.GetWorldCorners(corners);
                return corners;
            }
        }
        private readonly Vector3[] timelineBarCorners;
        private Vector3[] TimelineBarCorners
        {
            get {
                timelineBarRect.GetWorldCorners(timelineBarCorners);
                return timelineBarCorners;
            }
        }
        private readonly Vector3[] timelineAreaCorners;
        private Vector3[] TimelineAreaCorners
        {
            get {
                timelineAreaRect.GetWorldCorners(timelineAreaCorners);
                return timelineAreaCorners;
            }
        }

        public TimelineClip(RectTransform _rect, RectTransform _timelineBarRect, RectTransform _timelineAreaRect, RawImage _rawImage)
        {
            rect = _rect;
            timelineBarRect = _timelineBarRect;
            timelineAreaRect = _timelineAreaRect;
            rawImage = _rawImage;
            
            mouseAction = MouseAction.Nothing;
            corners = new Vector3[4];
            timelineBarCorners = new Vector3[4];
            timelineAreaCorners = new Vector3[4];
        }
        
        public TimelineClip(RectTransform _timelineBarRect, RectTransform _timelineAreaRect)
        {
            timelineBarRect = _timelineBarRect;
            timelineAreaRect = _timelineAreaRect;
            
            mouseAction = MouseAction.Nothing;
            corners = new Vector3[4];
            timelineBarCorners = new Vector3[4];
            timelineAreaCorners = new Vector3[4];
        }
        
        public virtual Color GetNotSelectedColor() { return Color.magenta; }

        public virtual ClipType GetClipType() { return ClipType.Base; }

        public virtual List<BrushStrokeID> GetBrushStrokeIDs() { return new List<BrushStrokeID>(); }
        
        public virtual List<TimelineClip> GetClips() { return new List<TimelineClip>(); }
        public virtual void SetTime(Vector2 _time) { }
        public virtual Vector2 GetTime() { return Vector2.zero; }

        public virtual bool HoldingBrushStroke(BrushStrokeID _brushStrokeID) { return false; }
        
        public virtual void SetupMovement(MouseAction _mouseAction, float _leftMostPos, float _rightMostPos)
        {
            switch (_mouseAction)
            {
                case MouseAction.Nothing:
                    break;
                case MouseAction.GrabbedClip:
                    SetMouseOffset();
                    break;
                case MouseAction.ResizeClipLeft:
                    SetResizeLeft();
                    leftMostPos = _leftMostPos;
                    rightMostPos = _rightMostPos;
                    oldLeftPos = Corners[0].x;
                    oldRightPos = Corners[2].x;
                    startMousePos = Input.mousePosition.x;
                    break;
                case MouseAction.ResizeClipRight:
                    SetResizeRight();
                    leftMostPos = _leftMostPos;
                    rightMostPos = _rightMostPos;
                    oldLeftPos = Corners[0].x;
                    oldRightPos = Corners[2].x;
                    startMousePos = Input.mousePosition.x;
                    break;
            }
        }
        private void SetMouseOffset()
        {
            mouseOffset = Input.mousePosition.x - rect.position.x;
        }
        private void SetResizeLeft()
        {
            if (rect.pivot.x < 1)
            {
                float clipLength = rect.sizeDelta.x;
                rect.position += new Vector3(clipLength, 0, 0);
                rect.pivot = new Vector2(1, 1);
            }
            mouseOffset = Input.mousePosition.x - rect.position.x + rect.sizeDelta.x;
        }
        private void SetResizeRight()
        {
            if (rect.pivot.x > 0)
            {
                float clipLength = rect.sizeDelta.x;
                rect.position -= new Vector3(clipLength, 0, 0);
                rect.pivot = new Vector2(0, 1);
            }
            mouseOffset = Input.mousePosition.x - rect.position.x - rect.sizeDelta.x;
        }
        public MouseAction GetMouseAction()
        {
            if (IsMouseOver() && mouseAction == MouseAction.Nothing)
            {
                if (Input.mousePosition.x > Corners[0].x && Input.mousePosition.x < Corners[0].x + clipHandle)
                {
                    if (rect.pivot.x < 1)
                    {
                        float clipLength = rect.sizeDelta.x;
                        rect.position += new Vector3(clipLength, 0, 0);
                        rect.pivot = new Vector2(1, 1);
                    }
                    return MouseAction.ResizeClipLeft;
                }
                if (Input.mousePosition.x < Corners[2].x && Input.mousePosition.x > Corners[2].x - clipHandle)
                {
                    if (rect.pivot.x > 0)
                    {
                        float clipLength = rect.sizeDelta.x;
                        rect.position -= new Vector3(clipLength, 0, 0);
                        rect.pivot = new Vector2(0, 1);
                    }
                    return MouseAction.ResizeClipRight;
                }
                return MouseAction.GrabbedClip;
            }
            return mouseAction;
        }

        public void ResizeAllRight()
        {
            float mouseDeltaX = Input.mousePosition.x - startMousePos;

            float sizeIncreasePercentage = ((rightMostPos - leftMostPos) + mouseDeltaX) / (rightMostPos - leftMostPos) - 1;
            float newLeftPos = (oldLeftPos - leftMostPos) * sizeIncreasePercentage + oldLeftPos;
            float newRightPos = (oldRightPos - leftMostPos) * sizeIncreasePercentage + oldRightPos;
            newLeftPos = Mathf.Clamp(newLeftPos, TimelineBarCorners[0].x, newRightPos - minumunWidth);
            newRightPos = Mathf.Clamp(newRightPos, newLeftPos + minumunWidth, TimelineBarCorners[2].x);
            if (newLeftPos > newRightPos - minumunWidth) { return; }
            if (newRightPos < newLeftPos + minumunWidth) { return; }
            
            rect.position = new Vector3(newLeftPos, rect.position.y, rect.position.z);
            rect.sizeDelta = new Vector2(newRightPos - newLeftPos, rect.sizeDelta.y);
        }
        public void ResizeAllLeft()
        {
            float mouseDeltaX = startMousePos - Input.mousePosition.x;

            float sizeIncreasePercentage = ((rightMostPos - leftMostPos) + mouseDeltaX) / (rightMostPos - leftMostPos) - 1;
            float newLeftPos = (oldLeftPos - rightMostPos) * sizeIncreasePercentage + oldLeftPos;
            float newRightPos = (oldRightPos - rightMostPos) * sizeIncreasePercentage + oldRightPos;
            newLeftPos = Mathf.Clamp(newLeftPos, TimelineBarCorners[0].x, newRightPos - minumunWidth);
            newRightPos = Mathf.Clamp(newRightPos, newLeftPos + minumunWidth, TimelineBarCorners[2].x);
            if (newLeftPos > newRightPos - minumunWidth) { return; }
            if (newRightPos < newLeftPos + minumunWidth) { return; }
            
            rect.position = new Vector3(newRightPos, rect.position.y, rect.position.z);
            rect.sizeDelta = new Vector2(newRightPos - newLeftPos, rect.sizeDelta.y);
        }
        
        public void UpdateTransform()
        {
            switch (mouseAction)
            {
                case MouseAction.Nothing:
                    break;

                case MouseAction.GrabbedClip:
                    MoveClip();
                    break;

                case MouseAction.ResizeClipLeft:
                    ResizeAllLeft();
                    break;

                case MouseAction.ResizeClipRight:
                    ResizeAllRight();
                    break;
            }
        }
        private void MoveClip()
        {
            float clipLength = rect.sizeDelta.x;
            Vector3 position = rect.position;
            float yPos = GetYPos();
            float xPos = Input.mousePosition.x - mouseOffset;

            if (rect.pivot.x == 0)
            {
                xPos = Mathf.Clamp(xPos, TimelineBarCorners[0].x, TimelineBarCorners[2].x - clipLength);
            }
            else
            {
                xPos = Mathf.Clamp(xPos, TimelineBarCorners[0].x + clipLength, TimelineBarCorners[2].x);
            }

            position = new Vector3(xPos, yPos, position.z);
            rect.position = position;
        }

        private float GetYPos()
        {
            float yPos = rect.position.y;
            float timelineBarHeight = Corners[2].y - Corners[0].y + Timeline.timelineBarSpacing;
            float inputOffset = Input.mousePosition.y;
            if (inputOffset < TimelineAreaCorners[0].y || inputOffset > TimelineAreaCorners[2].y)
            {
                return yPos;
            }
        
            if (inputOffset < Corners[0].y - Timeline.timelineBarSpacing)
            {
                currentBar++;
                return yPos - timelineBarHeight;
            }
            if (inputOffset > Corners[2].y + Timeline.timelineBarSpacing)
            {
                currentBar--;
                return yPos + timelineBarHeight;
            }
            return yPos;
        }
        public void SetBar(int newBar)
        {
            float timelineBarHeight = Corners[2].y - Corners[0].y + Timeline.timelineBarSpacing;
            int amountToMove = newBar - currentBar;
            timelineBarHeight *= amountToMove;

            rect.position -= new Vector3(0, timelineBarHeight, 0);
            currentBar = newBar;
        }
        
        public bool IsMouseOver()
        {
            return Input.mousePosition.x > Corners[0].x && Input.mousePosition.x < Corners[2].x && 
                   Input.mousePosition.y > Corners[0].y && Input.mousePosition.y < Corners[2].y;
        }

        public bool IsMouseOver(Vector2 _lastMousePos)
        {
            float minCornerX = Mathf.Min(Input.mousePosition.x, _lastMousePos.x);
            float minCornerY = Mathf.Min(Input.mousePosition.y, _lastMousePos.y);
            float maxCornerX = Mathf.Max(Input.mousePosition.x, _lastMousePos.x);
            float maxCornerY = Mathf.Max(Input.mousePosition.y, _lastMousePos.y);
            Vector4 mouseBox = new Vector4(minCornerX, minCornerY, maxCornerX, maxCornerY);
            return (mouseBox.x >= Corners[0].x && mouseBox.z <= Corners[2].x) &&
                   (mouseBox.y >= Corners[0].y && mouseBox.w <= Corners[2].y) ||
                   (Corners[0].x >= mouseBox.x && Corners[0].x <= mouseBox.z) &&
                   (mouseBox.y >= Corners[0].y && mouseBox.w <= Corners[2].y);
        }
    }
}