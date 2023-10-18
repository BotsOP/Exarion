using System;
using System.Collections;
using System.Collections.Generic;
using DataPersistence.Data;
using Managers;
using TMPro;
using UI;
using UnityEngine;
using EventType = Managers.EventType;

public class InputManager : MonoBehaviour, IDataPersistence
{
    [SerializeField] private bool edit3D;
    [SerializeField] private Camera viewCam;
    [SerializeField] private Camera displayCam;
    [SerializeField, Range(0.01f, 1f)] private float scrollZoomSensitivity;
    [SerializeField] private Transform viewFocus;
    [SerializeField] private Transform displayFocus;
    private RectTransform currentDrawArea;
    private RectTransform currentDisplayArea;
    private DrawingInput drawingInput;
    private DrawingInput3D drawingInput3D;
    private int imageWidth;
    private int imageHeight;

    private void Start()
    {
        if (edit3D)
        {
            drawingInput3D = new DrawingInput3D(viewCam, displayCam, scrollZoomSensitivity, viewFocus, displayFocus);
            return;
        }
        drawingInput = new DrawingInput(viewCam, displayCam, scrollZoomSensitivity, imageWidth, imageHeight);
    }
    private void OnEnable()
    {
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
        
        if (!UIManager.stopInteracting)
        {
            if (edit3D)
            {
                drawingInput3D.scrollZoomSensitivity = scrollZoomSensitivity;
                drawingInput3D.UpdateDrawingInput(viewCam, viewCam.transform.position, viewCam.orthographicSize);
            }
            else
            {
                drawingInput.scrollZoomSensitivity = scrollZoomSensitivity;
                drawingInput.UpdateDrawingInput(viewCam.transform.position, viewCam.orthographicSize);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            EventSystem.RaiseEvent(EventType.TIMELINE_PAUSE);
        }
    }
    public void LoadData(ToolData _data, ToolMetaData _metaData)
    {
        imageWidth = _data.imageWidth;
        imageHeight = _data.imageHeight;
    }
    public void SaveData(ToolData _data, ToolMetaData _metaData)
    {
        
    }
}
