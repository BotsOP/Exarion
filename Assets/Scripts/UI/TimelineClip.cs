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
    public class TimelineClip
    {
        public Vector2 ClipTime
        {
            get
            {
                float lastTime = ExtensionMethods.Remap(Corners[0].x, TimelineBarCorners[0].x, TimelineBarCorners[2].x, 0, 1);
                float currentTime = ExtensionMethods.Remap(Corners[2].x, TimelineBarCorners[0].x, TimelineBarCorners[2].x, 0, 1);
                return new Vector2(lastTime, currentTime);
            }
            set
            {
                var sizeDelta = rect.sizeDelta;
                var position = rect.position;
            
                float lastTimePos = ExtensionMethods.Remap(value.x, 0, 1, TimelineBarCorners[0].x, TimelineBarCorners[2].x);
                float currentTimePos = ExtensionMethods.Remap(value.y, 0, 1, TimelineBarCorners[0].x, TimelineBarCorners[2].x);
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

        public BrushStrokeID brushStrokeID;
        public RectTransform rect;
        public MouseAction mouseAction;
        public RawImage rawImage;
        public int currentBar;
        public int barOffset;
        public float startMousePos;
        public float leftMostPos;
        public float rightMostPos;
        public float oldLeftPos;
        public float oldRightPos;
        private RectTransform timelineBarRect;
        private RectTransform timelineAreaRect;
    
        private float mouseOffset;
        private float minumunWidth = 10;
        private float spacing = 10;
        
        private Vector3[] corners;
        private Vector3[] Corners
        {
            get {
                rect.GetWorldCorners(corners);
                return corners;
            }
        }
        private Vector3[] timelineBarCorners;
        private Vector3[] TimelineBarCorners
        {
            get {
                timelineBarRect.GetWorldCorners(timelineBarCorners);
                return timelineBarCorners;
            }
        }
        private Vector3[] timelineAreaCorners;
        private Vector3[] TimelineAreaCorners
        {
            get {
                timelineAreaRect.GetWorldCorners(timelineAreaCorners);
                return timelineAreaCorners;
            }
        }

        public TimelineClip(BrushStrokeID _brushStrokeID, RectTransform _rect, RectTransform _timelineBarRect, RectTransform _timelineAreaRect, RawImage _rawImage)
        {
            brushStrokeID = _brushStrokeID;
            rect = _rect;
            timelineBarRect = _timelineBarRect;
            timelineAreaRect = _timelineAreaRect;
            rawImage = _rawImage;
            
            mouseAction = MouseAction.Nothing;
            corners = new Vector3[4];
            timelineBarCorners = new Vector3[4];
            timelineAreaCorners = new Vector3[4];
        }
        public void SetupMovement(MouseAction _mouseAction, float _leftMostPos, float _rightMostPos)
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
        public void SetMouseOffset()
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
            Debug.Log($"{rect.pivot}");
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
                if (Input.mousePosition.x > Corners[0].x && Input.mousePosition.x < Corners[0].x + 10)
                {
                    if (rect.pivot.x < 1)
                    {
                        float clipLength = rect.sizeDelta.x;
                        rect.position += new Vector3(clipLength, 0, 0);
                        rect.pivot = new Vector2(1, 1);
                    }
                    return MouseAction.ResizeClipLeft;
                }
                if (Input.mousePosition.x < Corners[2].x && Input.mousePosition.x > Corners[2].x - 10)
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
            newLeftPos = Mathf.Clamp(newLeftPos, TimelineBarCorners[0].x, TimelineBarCorners[2].x);
            newRightPos = Mathf.Clamp(newRightPos, TimelineBarCorners[0].x, TimelineBarCorners[2].x);
            
            rect.position = new Vector3(newLeftPos, rect.position.y, rect.position.z);
            rect.sizeDelta = new Vector2(newRightPos - newLeftPos, rect.sizeDelta.y);
            //Debug.Log($"l: {leftMostPos}  r: {rightMostPos}   m: {mouseDeltaX} s: {sizeIncreasePercentage}  el: {extraLeftPos} er: {extraRightPos}  ol: {oldLeftPos} or: {oldRightPos}");
        }
        public void ResizeAllLeft()
        {
            float mouseDeltaX = startMousePos - Input.mousePosition.x;

            float sizeIncreasePercentage = ((rightMostPos - leftMostPos) + mouseDeltaX) / (rightMostPos - leftMostPos) - 1;
            float newLeftPos = (oldLeftPos - rightMostPos) * sizeIncreasePercentage + oldLeftPos;
            float newRightPos = (oldRightPos - rightMostPos) * sizeIncreasePercentage + oldRightPos;
            newLeftPos = Mathf.Clamp(newLeftPos, TimelineBarCorners[0].x, TimelineBarCorners[2].x);
            newRightPos = Mathf.Clamp(newRightPos, TimelineBarCorners[0].x, TimelineBarCorners[2].x);
            
            //Debug.Log($"l: {leftMostPos}  r: {rightMostPos}   m: {mouseDeltaX} s: {sizeIncreasePercentage}  el: {newLeftPos} er: {newRightPos}  ol: {oldLeftPos} or: {oldRightPos}");
            rect.position = new Vector3(newRightPos, rect.position.y, rect.position.z);
            rect.sizeDelta = new Vector2(newRightPos - newLeftPos, rect.sizeDelta.y);
        }
        
        public void UpdateTransform(Vector2 _previousMousePos)
        {
            float mouseDeltaX = Input.mousePosition.x - _previousMousePos.x;
            
            switch (mouseAction)
            {
                case MouseAction.Nothing:
                    break;

                case MouseAction.GrabbedClip:
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
                    break;

                case MouseAction.ResizeClipLeft:
                    // if (ClampResizeLeft())
                    //     return;
                    //
                    // rect.sizeDelta -= new Vector2(mouseDeltaX, 0);
                    ResizeAllLeft();
                    break;

                case MouseAction.ResizeClipRight:
                    // if (ClampResizeRight())
                    //     return;
                    //
                    // rect.sizeDelta += new Vector2(mouseDeltaX, 0);
                    ResizeAllRight();
                    break;
            }
        }

        private bool ClampResizeLeft()
        {
            if (Corners[0].x < TimelineBarCorners[0].x || (Input.mousePosition.x - mouseOffset) < TimelineBarCorners[0].x)
            {
                float width = Corners[2].x - TimelineBarCorners[0].x;
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                return true;
            }

            if ((Input.mousePosition.x - mouseOffset) > Corners[2].x - minumunWidth * 2)
            {
                rect.sizeDelta = new Vector2(minumunWidth, rect.sizeDelta.y);
                return true;
            }
            return false;
        }
        private bool ClampResizeRight()
        {

            if (Corners[2].x > TimelineBarCorners[2].x || (Input.mousePosition.x - mouseOffset) > TimelineBarCorners[2].x)
            {
                float width = TimelineBarCorners[2].x - Corners[0].x;
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                return true;
            }

            if ((Input.mousePosition.x - mouseOffset) < Corners[0].x + minumunWidth * 2)
            {
                rect.sizeDelta = new Vector2(minumunWidth, rect.sizeDelta.y);
                return true;
            }
            return false;
        }

        private float GetYPos()
        {
            float yPos = rect.position.y;
            float timelineBarHeight = Corners[2].y - Corners[0].y + spacing;
            float inputOffset = Input.mousePosition.y - timelineBarHeight * barOffset;
            if (inputOffset < TimelineAreaCorners[0].y || inputOffset > TimelineAreaCorners[2].y)
            {
                return yPos;
            }
        
            if (inputOffset < Corners[0].y - spacing)
            {
                currentBar++;
                return yPos - timelineBarHeight;
            }
            if (inputOffset > Corners[2].y + spacing)
            {
                currentBar--;
                return yPos + timelineBarHeight;
            }
            return yPos;
        }
        public void SetBar(int newBar)
        {
            float timelineBarHeight = Corners[2].y - Corners[0].y + spacing;
            int amountToMove = newBar - currentBar;
            timelineBarHeight *= amountToMove;

            rect.position -= new Vector3(0, timelineBarHeight, 0);
            currentBar = newBar;
        }

        public bool IsMouseOver()
        {
            return Input.mousePosition.x > Corners[0].x && Input.mousePosition.x < Corners[2].x && Input.mousePosition.y > Corners[0].y &&
                   Input.mousePosition.y < Corners[2].y;
        }
    }
}