using System;
using System.Collections;
using System.Collections.Generic;
using Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Timeline : MonoBehaviour
{
    [SerializeField] private GameObject timelineBarObject;
    [SerializeField] private GameObject timelineClipObject;
    [SerializeField] private GameObject timelineScrollBar;
    [SerializeField] private Slider timelineSpeedSlider;
    [SerializeField] private List<GameObject> timelineBars;
    [SerializeField] private TMP_InputField clipLeftInput;
    [SerializeField] private TMP_InputField clipRightInput;
    [SerializeField] private List<TimelineClip> clips;
    private RectTransform timelineRect;
    private RectTransform timelineScrollRect;
    private float time => Time.time / 10 % 1.0f;
    private Drawing.Drawing drawer;
    private Vector3[] corners;
    private Vector2 previousMousePos;
    private TimelineClip selectedTimelineClip;

    private void OnEnable()
    {
        corners = new Vector3[4];
        timelineRect = GetComponent<RectTransform>();
        timelineScrollRect = timelineScrollBar.GetComponent<RectTransform>();
        clips = new List<TimelineClip>();
    }
    private void OnDisable()
    {
        
    }

    private void Update()
    {
        timelineRect.GetWorldCorners(corners);
        float xPos = ExtensionMethods.Remap(time, 0, 1, corners[0].x, corners[2].x);
        var position = timelineScrollRect.position;
        position = new Vector3(xPos, position.y, position.z);
        timelineScrollRect.position = position;

        for (int i = 0; i < clips.Count; i++)
        {
            clips[i].UpdateUI(Input.mousePosition, previousMousePos);
            if (clips[i].mouseAction != MouseAction.Nothing)
            {
                selectedTimelineClip = clips[i];
                clipLeftInput.text = clips[i].leftSideScaled.ToString("0.###");
                clipRightInput.text = clips[i].rightSideScaled.ToString("0.###");
                break;
            }
        }

        previousMousePos = Input.mousePosition;

        if (Input.GetKeyDown(KeyCode.N))
        {
            AddNewBrushClip(0);
        }
    }

    public void ChangedInput()
    {
        if (selectedTimelineClip != null)
        {
            if (selectedTimelineClip.mouseAction == MouseAction.Nothing)
            {
                //TODO only change sideScaled if a value changed and add better clamping
                float leftSide = float.Parse(clipLeftInput.text);
                float rightSide = float.Parse(clipRightInput.text);
                if (leftSide < rightSide)
                {
                    selectedTimelineClip.leftSideScaled = Mathf.Clamp01(leftSide);
                    selectedTimelineClip.rightSideScaled = Mathf.Clamp01(rightSide);
                }
                
            }
        }
    }

    private void AddNewBrushClip(int brushStrokeID)
    {
        RectTransform rect = Instantiate(timelineClipObject, timelineBars[0].transform).GetComponent<RectTransform>();
        TimelineClip timelineClip = new TimelineClip(brushStrokeID, rect, timelineRect);
        clips.Add(timelineClip);
    }
}
