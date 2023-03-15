using System.Collections;
using System.Collections.Generic;
using Drawing;
using UnityEngine;

public struct TimelineClip
{
    enum MouseAction
    {
        Nothing,
        GrabbedClip,
        ResizeClipLeft,
        ResizeClipRight
    }
    
    public float leftSideScaled
    {
        get
        {
            rect.GetWorldCorners(corners);
            parentRect.GetWorldCorners(timelineBarCorners);
            return ExtensionMethods.Remap(corners[0].x, timelineBarCorners[0].x, timelineBarCorners[2].x, 0, 1);
        }
    }

    public float rightSideScaled
    {
        get
        {
            rect.GetWorldCorners(corners);
            return ExtensionMethods.Remap(corners[2].x, timelineBarCorners[0].x, timelineBarCorners[2].x, 0, 1);
        }
    }
    
    public int brushStrokeID;
    private RectTransform rect;
    private RectTransform parentRect;
    
    private Vector3[] corners;
    private Vector3[] timelineBarCorners;

    public TimelineClip(int brushStrokeID, RectTransform rect, RectTransform parentRect)
    {
        Debug.Log($"test");
        this.brushStrokeID = brushStrokeID;
        this.rect = rect;
        this.parentRect = parentRect;
        corners = new Vector3[4];
        timelineBarCorners = new Vector3[4];
    }
    
    public void UpdateUI(Vector2 mousePos, Vector2 previouMousePos)
    {
        rect.GetWorldCorners(corners);
        parentRect.GetWorldCorners(timelineBarCorners);

        MouseAction mouseAction = MouseAction.Nothing;
        
        float mouseDeltaX = mousePos.x - previouMousePos.x;
        float mouseOffset = 0;

        if (IsMouseOver(mousePos) && Input.GetMouseButtonDown(0) && mouseAction == MouseAction.Nothing)
        {
            mouseOffset = mousePos.x - rect.position.x;

            if (mousePos.x > corners[0].x && mousePos.x < corners[0].x + 10)
            {
                Debug.Log($"set resize left");
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
                Debug.Log($"set resize right");
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
                Debug.Log($"set grabbed");
                mouseAction = MouseAction.GrabbedClip;
            }
        }
        else if (Input.GetMouseButtonUp(0) && mouseAction != MouseAction.Nothing)
        {
            Debug.Log($"set to nothing");
            mouseAction = MouseAction.Nothing;
        }
        

        switch (mouseAction)
        {
            case MouseAction.Nothing:
                break;
            case MouseAction.GrabbedClip:
                Debug.Log($"grabbed");
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
                Debug.Log($"resize left");
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
                Debug.Log($"resize right");
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
