using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BrushStrokeClip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Timeline timeline;
    public int brushStrokeID;
    private bool mouseOver;
    private bool shouldRun;
    private RectTransform parentRect;
    private RectTransform rect;
    private float mouseOffset;
    private void OnEnable()
    {
        parentRect = transform.parent.GetComponent<RectTransform>();
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (mouseOver && Input.GetMouseButton(0) && !shouldRun)
        {
            mouseOffset = Input.mousePosition.x - rect.position.x;
            shouldRun = true;
        }
        else if (Input.GetMouseButtonUp(0) && shouldRun)
        {
            shouldRun = false;
        }

        if (shouldRun)
        {
            Vector3[] timelineBarCorners = new Vector3[4];
            parentRect.GetWorldCorners(timelineBarCorners);
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            
            float clipLength = rect.sizeDelta.x / 2;
            float xPos = Input.mousePosition.x - mouseOffset;
            
            xPos = Mathf.Clamp(xPos, timelineBarCorners[0].x + clipLength, timelineBarCorners[2].x - clipLength);
            rect.position = new Vector3(xPos, rect.position.y, rect.position.z);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseOver = true;
    }
 
    public void OnPointerExit(PointerEventData eventData)
    {
        mouseOver = false;
    }
}
