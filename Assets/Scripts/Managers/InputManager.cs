using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private RectTransform currentDrawArea;
    private RectTransform currentDisplayArea;
    private DrawingInput drawingInput;
    [SerializeField] private Camera viewCam;
    [SerializeField] private Camera displayCam;
    [SerializeField, Range(0.1f, 0.2f)] private float scrollZoomSensitivity;
    [SerializeField] private float moveSensitivity;

    private void OnEnable()
    {
        drawingInput = new DrawingInput(viewCam, displayCam, scrollZoomSensitivity, moveSensitivity);
        EventSystem<RectTransform, RectTransform>.Subscribe(EventType.VIEW_CHANGED, SetDrawArea);
    }
    private void OnDisable()
    {
        EventSystem<RectTransform, RectTransform>.Unsubscribe(EventType.VIEW_CHANGED, SetDrawArea);
    }

    private void SetDrawArea(RectTransform currentDrawArea, RectTransform currentDisplayArea)
    {
        this.currentDrawArea = currentDrawArea;
        this.currentDisplayArea = currentDisplayArea;
    }

    private void Update()
    {
        Vector3[] drawAreaCorners = new Vector3[4];
        currentDrawArea.GetWorldCorners(drawAreaCorners);
        
        Vector3[] displayAreaCorners = new Vector3[4];
        currentDisplayArea.GetWorldCorners(displayAreaCorners);
        
        drawingInput.scrollZoomSensitivity = scrollZoomSensitivity;
        drawingInput.moveSensitivity = moveSensitivity;
        drawingInput.UpdateDrawingInput(drawAreaCorners, displayAreaCorners, viewCam.transform.position, viewCam.orthographicSize);
    }
}
