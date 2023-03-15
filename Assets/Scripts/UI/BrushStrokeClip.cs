using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;



public class BrushStrokeClip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    enum MouseAction
    {
        Nothing,
        GrabbedClip,
        ResizeClipLeft,
        ResizeClipRight
    }
    
    public Timeline timeline;
    public int brushStrokeID;
    public float leftSideScaled
    {
        get
        {
            rect.GetWorldCorners(corners);
            parentRect.GetWorldCorners(timelineBarCorners);
            return Remap(corners[0].x, timelineBarCorners[0].x, timelineBarCorners[2].x, 0, 1);
        }
    }

    public float rightSideScaled
    {
        get
        {
            rect.GetWorldCorners(corners);
            return Remap(corners[2].x, timelineBarCorners[0].x, timelineBarCorners[2].x, 0, 1);
        }
    }
    
    private bool mouseOver;
    private RectTransform parentRect;
    private RectTransform rect;
    private float mouseOffset;
    private float mouseDeltaX;
    private float previousMouseX;
    private MouseAction mouseAction = MouseAction.Nothing;
    private Vector3[] corners;
    private Vector3[] timelineBarCorners;

    private void OnEnable()
    {
        parentRect = transform.parent.GetComponent<RectTransform>();
        rect = GetComponent<RectTransform>();
        corners = new Vector3[4];
        timelineBarCorners = new Vector3[4];
    }

    void Update()
    {
        rect.GetWorldCorners(corners);
        parentRect.GetWorldCorners(timelineBarCorners);
        
        mouseDeltaX = Input.mousePosition.x - previousMouseX;
        if (mouseOver && Input.GetMouseButton(0) && mouseAction == MouseAction.Nothing)
        {
            mouseOffset = Input.mousePosition.x - rect.position.x;

            if (Input.mousePosition.x > corners[0].x && Input.mousePosition.x < corners[0].x + 10)
            {
                
                if (rect.pivot.x < 1)
                {
                    float clipLength = rect.sizeDelta.x;
                    rect.position += new Vector3(clipLength, 0, 0);
                    rect.pivot = new Vector2(1, 0.5f);
                }
                mouseAction = MouseAction.ResizeClipLeft;
            }
            else if (Input.mousePosition.x < corners[2].x && Input.mousePosition.x > corners[2].x - 10)
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
        else if (Input.GetMouseButtonUp(0) && mouseAction != MouseAction.Nothing)
        {
            mouseAction = MouseAction.Nothing;
        }
        

        switch (mouseAction)
        {
            case MouseAction.Nothing:
                break;
            case MouseAction.GrabbedClip:
                

                float clipLength = rect.sizeDelta.x;
                float xPos = Input.mousePosition.x - mouseOffset;
            
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
                if (corners[0].x < timelineBarCorners[0].x || Input.mousePosition.x < timelineBarCorners[0].x)
                {
                    float width = corners[2].x - timelineBarCorners[0].x;
                    rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                    return;
                }

                if (Input.mousePosition.x > corners[2].x - 20)
                {
                    float width = 20;
                    rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                    return;
                }

                rect.sizeDelta -= new Vector2(mouseDeltaX, 0);
                break;
            case MouseAction.ResizeClipRight:
                if (corners[2].x > timelineBarCorners[2].x || Input.mousePosition.x > timelineBarCorners[2].x)
                {
                    float width = timelineBarCorners[2].x - corners[0].x;
                    rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                    return;
                }
                
                if (Input.mousePosition.x < corners[0].x + 20)
                {
                    float width = 20;
                    rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                    return;
                }

                rect.sizeDelta += new Vector2(mouseDeltaX, 0);
                break;
        }

        previousMouseX = Input.mousePosition.x;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseOver = true;
    }
 
    public void OnPointerExit(PointerEventData eventData)
    {
        mouseOver = false;
    }
    
    private float Remap (float value, float from1, float to1, float from2, float to2) {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
