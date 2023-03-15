using System.Collections;
using System.Collections.Generic;
using Drawing;
using UnityEngine;

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
            parentRect.GetWorldCorners(timelineBarCorners);
            return ExtensionMethods.Remap(corners[0].x, timelineBarCorners[0].x, timelineBarCorners[2].x, 0, 1);
        }
        set
        {
            rect.GetWorldCorners(corners);
            parentRect.GetWorldCorners(timelineBarCorners);
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
            return ExtensionMethods.Remap(corners[2].x, timelineBarCorners[0].x, timelineBarCorners[2].x, 0, 1);
        }
        set
        {
            Debug.Log($"test");
            rect.GetWorldCorners(corners);
            parentRect.GetWorldCorners(timelineBarCorners);
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
    private RectTransform rect;
    private RectTransform parentRect;
    
    private Vector3[] corners;
    private Vector3[] timelineBarCorners;
    public MouseAction mouseAction;
    private float mouseOffset;

    public TimelineClip(int brushStrokeID, RectTransform rect, RectTransform parentRect)
    {
        this.brushStrokeID = brushStrokeID;
        this.rect = rect;
        this.parentRect = parentRect;
        corners = new Vector3[4];
        timelineBarCorners = new Vector3[4];
        mouseAction = MouseAction.Nothing;
    }
    
    public void UpdateUI(Vector2 mousePos, Vector2 previousMousePos)
    {
        rect.GetWorldCorners(corners);
        parentRect.GetWorldCorners(timelineBarCorners);

        Debug.Log(mouseAction);
        
        float mouseDeltaX = mousePos.x - previousMousePos.x;

        if (IsMouseOver(mousePos) && Input.GetMouseButtonDown(0) && mouseAction == MouseAction.Nothing)
        {
            mouseOffset = mousePos.x - rect.position.x;

            if (mousePos.x > corners[0].x && mousePos.x < corners[0].x + 10)
            {
                if (rect.pivot.x < 1)
                {
                    float clipLength = rect.sizeDelta.x;
                    rect.position += new Vector3(clipLength, 0, 0);
                    rect.pivot = new Vector2(1, 0.5f);
                }
                mouseAction = MouseAction.ResizeClipLeft;
            }
            else if (mousePos.x < corners[2].x && mousePos.x > corners[2].x - 10)
            {
                if (rect.pivot.x > 0)
                {
                    float clipLength = rect.sizeDelta.x;
                    rect.position -= new Vector3(clipLength, 0, 0);
                    rect.pivot = new Vector2(0, 0.5f);
                }
                mouseAction = MouseAction.ResizeClipRight;
            }
            else
            {
                mouseAction = MouseAction.GrabbedClip;
            }
        }
        else if(Input.GetMouseButtonUp(0) && mouseAction != MouseAction.Nothing)
        {
            mouseAction = MouseAction.Nothing;
        }


        switch (mouseAction)
        {
            case MouseAction.Nothing:
                break;
            case MouseAction.GrabbedClip:
                float clipLength = rect.sizeDelta.x;
                float xPos = mousePos.x - mouseOffset;
            
                if (rect.pivot.x == 0)
                {
                    xPos = Mathf.Clamp(xPos, timelineBarCorners[0].x, timelineBarCorners[2].x - clipLength);
                }
                else
                {
                    xPos = Mathf.Clamp(xPos, timelineBarCorners[0].x + clipLength, timelineBarCorners[2].x);
                }
                
                Vector3 position = rect.position;
                position = new Vector3(xPos, position.y, position.z);
                rect.position = position;
                break;
            case MouseAction.ResizeClipLeft:
                if (corners[0].x < timelineBarCorners[0].x || mousePos.x < timelineBarCorners[0].x)
                {
                    float width = corners[2].x - timelineBarCorners[0].x;
                    rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                    return;
                }

                if (mousePos.x > corners[2].x - 20)
                {
                    float width = 20;
                    rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                    return;
                }

                rect.sizeDelta -= new Vector2(mouseDeltaX, 0);
                break;
            case MouseAction.ResizeClipRight:
                if (corners[2].x > timelineBarCorners[2].x || mousePos.x > timelineBarCorners[2].x)
                {
                    float width = timelineBarCorners[2].x - corners[0].x;
                    rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                    return;
                }
                
                if (mousePos.x < corners[0].x + 20)
                {
                    float width = 20;
                    rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                    return;
                }

                rect.sizeDelta += new Vector2(mouseDeltaX, 0);
                break;
        }

    }

    public bool IsMouseOver(Vector2 mousePos)
    {
        return mousePos.x > corners[0].x && mousePos.x < corners[2].x && mousePos.y > corners[0].y &&
               mousePos.y < corners[2].y;
    }
}
