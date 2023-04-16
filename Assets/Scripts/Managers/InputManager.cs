using System;
using System.Collections;
using System.Collections.Generic;
using Managers;
using UI;
using UnityEngine;
using EventType = Managers.EventType;

public class InputManager : MonoBehaviour
{
    private RectTransform currentDrawArea;
    private RectTransform currentDisplayArea;
    private DrawingInput drawingInput;
    [SerializeField] private Camera viewCam;
    [SerializeField] private Camera displayCam;
    [SerializeField, Range(0.01f, 1f)] private float scrollZoomSensitivity;

    private void OnEnable()
    {
        drawingInput = new DrawingInput(viewCam, displayCam, scrollZoomSensitivity);
        EventSystem<RectTransform, RectTransform>.Subscribe(EventType.VIEW_CHANGED, SetDrawArea);
    }
    private void OnDisable()
    {
        EventSystem<RectTransform, RectTransform>.Unsubscribe(EventType.VIEW_CHANGED, SetDrawArea);
    }

    private void SetDrawArea(RectTransform _currentDrawArea, RectTransform _currentDisplayArea)
    {
        currentDrawArea = _currentDrawArea;
        currentDisplayArea = _currentDisplayArea;
    }

    private void Update()
    {
        Vector3[] drawAreaCorners = new Vector3[4];
        currentDrawArea.GetWorldCorners(drawAreaCorners);
        
        Vector3[] displayAreaCorners = new Vector3[4];
        currentDisplayArea.GetWorldCorners(displayAreaCorners);
        
        drawingInput.scrollZoomSensitivity = scrollZoomSensitivity;
        drawingInput.UpdateDrawingInput(viewCam.transform.position, viewCam.orthographicSize);
    }
}
