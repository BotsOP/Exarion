using System;
using System.Collections;
using System.Collections.Generic;
using Drawing;
using UnityEngine;
using UnityEngine.UI;

public class Timeline : MonoBehaviour
{
    [SerializeField] private GameObject newTimelineObject;
    [SerializeField] private GameObject newBrushClip;
    [SerializeField] private GameObject timelineScrollBar;
    [SerializeField] private Slider timelineSpeedSlider;
    private RectTransform rectTransform;
    private float time => Time.time / 10 % 1.0f;
    private Drawing.Drawing drawer;
    private Vector3[] corners;

    private void OnEnable()
    {
        corners = new Vector3[4];
        rectTransform = GetComponent<RectTransform>();
        
        var position = timelineScrollBar.GetComponent<RectTransform>().position;
    }
    private void OnDisable()
    {
        
    }

    private void Update()
    {
        rectTransform.GetWorldCorners(corners);
        float xPos = ExtensionMethods.Remap(time, 0, 1, corners[0].x, corners[2].x);
        var position = timelineScrollBar.GetComponent<RectTransform>().position;
        position = new Vector3(xPos, position.y, position.z);
        timelineScrollBar.GetComponent<RectTransform>().position = position;
    }

    private void AddNewBrushClip()
    {
        //Instantiate new brushClip with brushstrokID linked
    }
}
