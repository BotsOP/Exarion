using System;
using System.Collections;
using System.Collections.Generic;
using Drawing;
using UnityEngine;

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

        public int brushStrokeID;
        public RectTransform rect;
        public MouseAction mouseAction;
        private RectTransform timelineBarRect;
        private RectTransform timelineAreaRect;
    
        private Vector3[] corners;
        private Vector3[] timelineBarCorners;
        private Vector3[] timelineAreaCorners;
        private Vector2 mouseOffset;
        private float minumunWidth = 10;

        public TimelineClip(int brushStrokeID, RectTransform rect, RectTransform timelineBarRect, RectTransform timelineAreaRect)
        {
            this.brushStrokeID = brushStrokeID;
            this.rect = rect;
            this.timelineBarRect = timelineBarRect;
            this.timelineAreaRect = timelineAreaRect;
            corners = new Vector3[4];
            timelineBarCorners = new Vector3[4];
            timelineAreaCorners = new Vector3[4];
            mouseAction = MouseAction.Nothing;
        }

        public void UpdateUI(Vector2 mousePos, Vector2 previousMousePos)
        {
            rect.GetWorldCorners(corners);
            timelineBarRect.GetWorldCorners(timelineBarCorners);
            timelineAreaRect.GetWorldCorners(timelineAreaCorners);

            float mouseDeltaX = mousePos.x - previousMousePos.x;

            if (IsMouseOver(mousePos) && mouseAction == MouseAction.Nothing)
            {
                mouseOffset = new Vector2(mousePos.x - rect.position.x, mousePos.y - rect.position.y) ;

                if (mousePos.x > corners[0].x && mousePos.x < corners[0].x + 10)
                {
                    if (rect.pivot.x < 1)
                    {
                        float clipLength = rect.sizeDelta.x;
                        rect.position += new Vector3(clipLength, 0, 0);
                        rect.pivot = new Vector2(1, 1);
                    }
                    mouseAction = MouseAction.ResizeClipLeft;
                }
                else if (mousePos.x < corners[2].x && mousePos.x > corners[2].x - 10)
                {
                    if (rect.pivot.x > 0)
                    {
                        float clipLength = rect.sizeDelta.x;
                        rect.position -= new Vector3(clipLength, 0, 0);
                        rect.pivot = new Vector2(0, 1);
                    }
                    mouseAction = MouseAction.ResizeClipRight;
                }
                else
                {
                    mouseAction = MouseAction.GrabbedClip;
                }
            }


            switch (mouseAction)
            {
                case MouseAction.Nothing:
                    break;
                case MouseAction.GrabbedClip:
                    float clipLength = rect.sizeDelta.x;
                    Vector3 position = rect.position;
                    float spacing = 10;
                    float yPos = GetYPos(mousePos, spacing, mousePos.y);
                    float xPos = mousePos.x - mouseOffset.x;
                
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
                    if (ClampResizeLeft(mousePos))
                        return;

                    
                    rect.sizeDelta -= new Vector2(mouseDeltaX, 0);
                    break;
                case MouseAction.ResizeClipRight:
                    if (ClampResizeRight(mousePos))
                        return;

                    Debug.Log($"{corners[2].x - corners[0].x} {(mousePos.x - corners[2].x)}");
                    float newWidth = corners[2].x - corners[0].x + (mousePos.x - corners[2].x);
                    rect.sizeDelta = new Vector2(newWidth, rect.sizeDelta.y);
                    //rect.sizeDelta += new Vector2(mouseDeltaX, 0);
                    break;
            }
        }
        
        private bool ClampResizeRight(Vector2 mousePos)
        {

            if (corners[2].x > timelineBarCorners[2].x || mousePos.x > timelineBarCorners[2].x)
            {
                float width = timelineBarCorners[2].x - corners[0].x;
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                return true;
            }

            if (mousePos.x < corners[0].x + 20)
            {
                rect.sizeDelta = new Vector2(minumunWidth, rect.sizeDelta.y);
                return true;
            }
            return false;
        }
        
        private bool ClampResizeLeft(Vector2 mousePos)
        {

            if (corners[0].x < timelineBarCorners[0].x || mousePos.x < timelineBarCorners[0].x)
            {
                float width = corners[2].x - timelineBarCorners[0].x;
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                return true;
            }

            if (mousePos.x > corners[2].x - 20)
            {
                rect.sizeDelta = new Vector2(minumunWidth, rect.sizeDelta.y);
                return true;
            }
            return false;
        }

        private float GetYPos(Vector2 mousePos, float spacing, float mousePosY)
        {
            float yPos = rect.position.y;
            float timelineBarHeight = corners[2].y - corners[0].y + spacing;
            if (mousePosY < timelineAreaCorners[0].y || mousePosY > timelineAreaCorners[2].y)
            {
                return yPos;
            }
        
            if (mousePos.y < corners[0].y - (spacing))
            {
                return yPos - timelineBarHeight;
            }
            if (mousePos.y > corners[2].y + (spacing))
            {
                return yPos + timelineBarHeight;
            }
            return yPos;
        }

        private bool IsMouseOver(Vector2 mousePos)
        {
            return mousePos.x > corners[0].x && mousePos.x < corners[2].x && mousePos.y > corners[0].y &&
                   mousePos.y < corners[2].y;
        }
    }
}