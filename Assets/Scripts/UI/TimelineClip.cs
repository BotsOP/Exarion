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
        public float leftSideScaled
        {
            get
            {
                rect.GetWorldCorners(corners);
                timelineBarRect.GetWorldCorners(timelineBarCorners);
                float leftSide = ExtensionMethods.Remap(corners[0].x, timelineBarCorners[0].x, timelineBarCorners[2].x, 0, 1);
                return leftSide;
            }
            set
            {
                rect.GetWorldCorners(corners);
                timelineBarRect.GetWorldCorners(timelineBarCorners);
                var sizeDelta = rect.sizeDelta;
                var position = rect.position;
            
                float xPos = ExtensionMethods.Remap(value, 0, 1, timelineBarCorners[0].x, timelineBarCorners[2].x);
                float differenceInLength = corners[0].x - xPos;
                float clipLength = sizeDelta.x + differenceInLength;
                sizeDelta = new Vector2(clipLength, sizeDelta.y);
                rect.sizeDelta = sizeDelta;

                if (rect.pivot.x == 0)
                {
                    position = new Vector3(xPos, position.y, position.z);
                    rect.position = position;
                }
                else
                {
                    xPos += clipLength;
                    position = new Vector3(xPos, position.y, position.z);
                    rect.position = position;
                }
            }
        }

        public float rightSideScaled
        {
            get
            {
                rect.GetWorldCorners(corners);
                timelineBarRect.GetWorldCorners(timelineBarCorners);
                float rightSide = ExtensionMethods.Remap(corners[2].x, timelineBarCorners[0].x, timelineBarCorners[2].x, 0, 1);
                return rightSide;
            }
            set
            {
                rect.GetWorldCorners(corners);
                timelineBarRect.GetWorldCorners(timelineBarCorners);
                var sizeDelta = rect.sizeDelta;
                var position = rect.position;
            
                float xPos = ExtensionMethods.Remap(value, 0, 1, timelineBarCorners[0].x, timelineBarCorners[2].x);
                float differenceInLength = xPos - corners[2].x;
                float clipLength = sizeDelta.x + differenceInLength;
                sizeDelta = new Vector2(clipLength, sizeDelta.y);
                rect.sizeDelta = sizeDelta;

                if (rect.pivot.x == 0)
                {
                    xPos -= clipLength;
                    position = new Vector3(xPos, position.y, position.z);
                    rect.position = position;
                }
                else
                {
                    position = new Vector3(xPos, position.y, position.z);
                    rect.position = position;
                }
            }
        }
        
        public float lastLeftSideScaled;
        public float lastRightSideScaled;
        public int previousBar;

        public BrushStrokeID brushStrokeID;
        public RectTransform rect;
        public MouseAction mouseAction;
        public RawImage rawImage;
        public int currentBar;
        public int barOffset;
        private RectTransform timelineBarRect;
        private RectTransform timelineAreaRect;
    
        private Vector3[] corners;
        private Vector3[] timelineBarCorners;
        private Vector3[] timelineAreaCorners;
        private float mouseOffset;
        private float minumunWidth = 10;
        private float spacing = 10;

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
        public void SetupMovement(MouseAction _mouseAction)
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
                    break;
                case MouseAction.ResizeClipRight:
                    SetResizeRight();
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
            rect.GetWorldCorners(corners);
            if (IsMouseOver() && mouseAction == MouseAction.Nothing)
            {
                if (Input.mousePosition.x > corners[0].x && Input.mousePosition.x < corners[0].x + 10)
                {
                    if (rect.pivot.x < 1)
                    {
                        float clipLength = rect.sizeDelta.x;
                        rect.position += new Vector3(clipLength, 0, 0);
                        rect.pivot = new Vector2(1, 1);
                    }
                    return MouseAction.ResizeClipLeft;
                }
                if (Input.mousePosition.x < corners[2].x && Input.mousePosition.x > corners[2].x - 10)
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
        
        public void UpdateTransform(Vector2 _previousMousePos)
        {
            timelineBarRect.GetWorldCorners(timelineBarCorners);
            timelineAreaRect.GetWorldCorners(timelineAreaCorners);
            
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
                        xPos = Mathf.Clamp(xPos, timelineBarCorners[0].x, timelineBarCorners[2].x - clipLength);
                    }
                    else
                    {
                        xPos = Mathf.Clamp(xPos, timelineBarCorners[0].x + clipLength, timelineBarCorners[2].x);
                    }

                    position = new Vector3(xPos, yPos, position.z);
                    rect.position = position;
                    break;

                case MouseAction.ResizeClipLeft:
                    if (ClampResizeLeft(Input.mousePosition))
                        return;

                    rect.sizeDelta -= new Vector2(mouseDeltaX, 0);
                    break;

                case MouseAction.ResizeClipRight:
                    if (ClampResizeRight(Input.mousePosition))
                        return;

                    rect.sizeDelta += new Vector2(mouseDeltaX, 0);
                    break;
            }
        }

        private bool ClampResizeLeft(Vector2 _mousePos)
        {
            if (corners[0].x < timelineBarCorners[0].x || (_mousePos.x - mouseOffset) < timelineBarCorners[0].x)
            {
                float width = corners[2].x - timelineBarCorners[0].x;
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                return true;
            }

            if ((_mousePos.x - mouseOffset) > corners[2].x - 20)
            {
                rect.sizeDelta = new Vector2(minumunWidth, rect.sizeDelta.y);
                return true;
            }
            return false;
        }
        private bool ClampResizeRight(Vector2 _mousePos)
        {

            if (corners[2].x > timelineBarCorners[2].x || (_mousePos.x - mouseOffset) > timelineBarCorners[2].x)
            {
                float width = timelineBarCorners[2].x - corners[0].x;
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                return true;
            }

            if ((_mousePos.x - mouseOffset) < corners[0].x + 20)
            {
                rect.sizeDelta = new Vector2(minumunWidth, rect.sizeDelta.y);
                return true;
            }
            return false;
        }

        private float GetYPos()
        {
            float yPos = rect.position.y;
            float timelineBarHeight = corners[2].y - corners[0].y + spacing;
            float inputOffset = Input.mousePosition.y - timelineBarHeight * barOffset;
            if (inputOffset < timelineAreaCorners[0].y || inputOffset > timelineAreaCorners[2].y)
            {
                return yPos;
            }
        
            if (inputOffset < corners[0].y - spacing)
            {
                currentBar++;
                return yPos - timelineBarHeight;
            }
            if (inputOffset > corners[2].y + spacing)
            {
                currentBar--;
                return yPos + timelineBarHeight;
            }
            return yPos;
        }
        public void SetBar(int newBar)
        {
            float timelineBarHeight = corners[2].y - corners[0].y + spacing;
            int amountToMove = newBar - currentBar;
            timelineBarHeight *= amountToMove;

            rect.position -= new Vector3(0, timelineBarHeight, 0);
            currentBar = newBar;
        }

        public bool IsMouseOver()
        {
            rect.GetWorldCorners(corners);
            return Input.mousePosition.x > corners[0].x && Input.mousePosition.x < corners[2].x && Input.mousePosition.y > corners[0].y &&
                   Input.mousePosition.y < corners[2].y;
        }
    }
}