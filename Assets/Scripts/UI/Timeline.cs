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
    private RectTransform timelineAreaRect;
    private RectTransform timelineScrollRect;
    private float time => Time.time / 10 % 1.0f;
    private Drawing.Drawing drawer;
    private Vector3[] corners;
    private Vector2 previousMousePos;
    private TimelineClip selectedTimelineClip;

    private void Awake()
    {
        corners = new Vector3[4];
        timelineRect = GetComponent<RectTransform>();
        timelineScrollRect = timelineScrollBar.GetComponent<RectTransform>();
        clips = new List<TimelineClip>();
    }

    private void OnEnable()
    {
        EventSystem<int, float, float>.Subscribe(EventType.STOPPED_DRAWING, AddNewBrushClip);
        EventSystem<int>.Subscribe(EventType.REMOVE_CLIP, RemoveClip);
    }

    private void OnDisable()
    {
        EventSystem<int, float, float>.Unsubscribe(EventType.STOPPED_DRAWING, AddNewBrushClip);
        EventSystem<int>.Unsubscribe(EventType.REMOVE_CLIP, RemoveClip);
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
                EventSystem<int, float, float>.RaiseEvent(EventType.REDRAW_STROKE, clips[i].brushStrokeID, clips[i].leftSideScaled, clips[i].rightSideScaled);
                break;
            }
        }

        previousMousePos = Input.mousePosition;

        // if (Input.GetKeyDown(KeyCode.N))
        // {
        //     AddNewBrushClip(0);
        // }
    }

    public void ChangedInput(TMP_InputField input)
    {
        if (selectedTimelineClip != null)
        {
            if (selectedTimelineClip.mouseAction == MouseAction.Nothing)
            {
                float leftSide = float.Parse(clipLeftInput.text);
                float rightSide = float.Parse(clipRightInput.text);
                if (leftSide < rightSide)
                {
                    if (input == clipLeftInput)
                    {
                        selectedTimelineClip.leftSideScaled = Mathf.Clamp01(leftSide);
                    }
                    else
                    {
                        selectedTimelineClip.rightSideScaled = Mathf.Clamp01(rightSide);
                    }
                    EventSystem<int, float, float>.RaiseEvent(EventType.REDRAW_STROKE, selectedTimelineClip.brushStrokeID, 
                        selectedTimelineClip.leftSideScaled, selectedTimelineClip.rightSideScaled);
                }
            }
        }
    }

    private void AddNewBrushClip(int brushStrokeID, float lastTime, float currentTime)
    {
        RectTransform rect = Instantiate(timelineClipObject, timelineBars[0].transform).GetComponent<RectTransform>();
        TimelineClip timelineClip = new TimelineClip(brushStrokeID, rect, timelineBars[0].GetComponent<RectTransform>(), timelineRect)
        {
            leftSideScaled = lastTime,
            rightSideScaled = currentTime,
        };
        clips.Add(timelineClip);
    }

    private void RemoveClip(int brushStrokeID)
    {
        int timelineClipIndex = -1;

        for (int i = 0; i < clips.Count; i++)
        {
            if (clips[i].brushStrokeID == brushStrokeID)
            {
                timelineClipIndex = i;
            }
        }

        if (timelineClipIndex >= 0)
        {
            Destroy(clips[timelineClipIndex].rect.gameObject);
            clips.RemoveAt(timelineClipIndex);
        }
    }
}
