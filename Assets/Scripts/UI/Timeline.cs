using System.Collections.Generic;
using Managers;
using TMPro;
using Undo;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class Timeline : MonoBehaviour
    {
        [SerializeField] private GameObject timelineBarObject;
        [SerializeField] private GameObject timelineClipObject;
        [SerializeField] private GameObject timelineScrollBar;
        [SerializeField] private int amountTimelineBars;
        [SerializeField] private TMP_InputField clipLeftInput;
        [SerializeField] private TMP_InputField clipRightInput;
        [SerializeField] private Color clipColor;
        [SerializeField] private Color selectedClipColor;
        
        //replace clips with clipsOrderderd
        private List<TimelineClip> clips;
        private List<List<TimelineClip>> clipsOrderderd;
        private RectTransform timelineRect;
        private RectTransform timelineAreaRect;
        private RectTransform timelineScrollRect;
        private bool selectedInput;
        private bool startTimelineClip = true;
        private float time;
        private float lastTimelineLeft;
        private float lastTimelineRight;
        private Vector2 previousMousePos;
        private Vector2 oldClipTime;
        private Vector3[] corners;
        private TimelineClip selectedTimelineClip;
        private CommandManager commandManager;
        private Drawing.Drawing drawer;

        private void Awake()
        {
            commandManager = FindObjectOfType<CommandManager>();
            corners = new Vector3[4];
            timelineRect = GetComponent<RectTransform>();
            timelineScrollRect = timelineScrollBar.GetComponent<RectTransform>();
            clips = new List<TimelineClip>();
            oldClipTime.x = -1;
            oldClipTime.y = -1;
            clipsOrderderd = new List<List<TimelineClip>>();
            for (int i = 0; i < amountTimelineBars; i++)
            {
                clipsOrderderd.Add(new List<TimelineClip>());
            }
        }

        private void OnEnable()
        {
            EventSystem<int>.Subscribe(EventType.REMOVE_CLIP, RemoveClip);
            EventSystem<int, float, float>.Subscribe(EventType.FINISHED_STROKE, AddNewBrushClip);
            EventSystem<int, float, float>.Subscribe(EventType.UPDATE_CLIP, UpdateClip);
            EventSystem<float>.Subscribe(EventType.TIME, SetTime);
        }

        private void OnDisable()
        {
            EventSystem<int>.Unsubscribe(EventType.REMOVE_CLIP, RemoveClip);
            EventSystem<float>.Unsubscribe(EventType.TIME, SetTime);
            EventSystem<int, float, float>.Unsubscribe(EventType.FINISHED_STROKE, AddNewBrushClip);
            EventSystem<int, float, float>.Unsubscribe(EventType.UPDATE_CLIP, UpdateClip);
        }

        private void SetTime(float _time)
        {
            time = _time;
        }

        private void Update()
        {
            MoveTimelineTimIndicator(time);

            TimelineClipsInput();

            previousMousePos = Input.mousePosition;
        }
        private void MoveTimelineTimIndicator(float _time)
        {
            timelineRect.GetWorldCorners(corners);
            float xPos = _time.Remap(0, 1, corners[0].x, corners[2].x);
            var position = timelineScrollRect.position;
            position = new Vector3(xPos, position.y, position.z);
            timelineScrollRect.position = position;
        }
        private void TimelineClipsInput()
        {
            //If you are already interacting with a timelineclip check that one first
            if (Input.GetMouseButton(0) && selectedTimelineClip is not null)
            {
                if (startTimelineClip)
                {
                    selectedTimelineClip.rect.GetComponent<RawImage>().color = selectedClipColor;
                    EventSystem<int>.RaiseEvent(EventType.HIGHLIGHT, selectedTimelineClip.brushStrokeID);
                    startTimelineClip = false;
                }
                selectedTimelineClip.UpdateUI(Input.mousePosition, previousMousePos);
                if (selectedTimelineClip.mouseAction != MouseAction.Nothing)
                {
                    if (oldClipTime.x < 0)
                    {
                        oldClipTime.x = selectedTimelineClip.leftSideScaled;
                    }
                    if (oldClipTime.y < 0)
                    {
                        oldClipTime.y = selectedTimelineClip.rightSideScaled;
                    }

                    clipLeftInput.text = selectedTimelineClip.leftSideScaled.ToString("0.###");
                    clipRightInput.text = selectedTimelineClip.rightSideScaled.ToString("0.###");
                    EventSystem<int, float, float>.RaiseEvent(
                        EventType.REDRAW_STROKE, selectedTimelineClip.brushStrokeID, selectedTimelineClip.leftSideScaled, selectedTimelineClip.rightSideScaled);
                }
            }
            //Otherwise check if you are interacting with any other timeline clips
            else if (Input.GetMouseButton(0))
            {
                foreach (TimelineClip clip in clips)
                {
                    clip.UpdateUI(Input.mousePosition, previousMousePos);
                    if (clip.mouseAction == MouseAction.Nothing)
                    {
                        continue;
                    }
                    
                    if (clip.mouseAction == MouseAction.GrabbedClip)
                    {
                        
                    }

                    if (oldClipTime.x < 0)
                    {
                        oldClipTime.x = clip.leftSideScaled;
                    }
                    if (oldClipTime.y < 0)
                    {
                        oldClipTime.y = clip.rightSideScaled;
                    }

                    selectedTimelineClip = clip;
                    clipLeftInput.text = clip.leftSideScaled.ToString("0.###");
                    clipRightInput.text = clip.rightSideScaled.ToString("0.###");
                    EventSystem<int, float, float>.RaiseEvent(EventType.REDRAW_STROKE, clip.brushStrokeID, clip.leftSideScaled, clip.rightSideScaled);
                    break;
                }
            }
            //Once you are not making changes 
            else if (Input.GetMouseButtonUp(0))
            {
                if (selectedTimelineClip is not null)
                {
                    ICommand clipCommand = new RedrawCommand(
                        selectedTimelineClip.brushStrokeID, selectedTimelineClip.leftSideScaled,
                        selectedTimelineClip.rightSideScaled, oldClipTime.x, oldClipTime.y);
                    commandManager.Execute(clipCommand);
                    selectedTimelineClip.mouseAction = MouseAction.Nothing;

                    oldClipTime.x = -1;
                    oldClipTime.y = -1;
                }
            }
            //once you have clicked somewhere else
            if (Input.GetMouseButtonDown(0) && selectedTimelineClip is not null)
            {
                Vector3[] clipCorners = new Vector3[4];
                selectedTimelineClip.rect.GetWorldCorners(clipCorners);
                if (!IsMouseOver(clipCorners))
                {
                    selectedTimelineClip.rect.GetComponent<RawImage>().color = clipColor;
                    selectedTimelineClip = null;
                    startTimelineClip = true;

                    clipLeftInput.text = "";
                    clipRightInput.text = "";

                    EventSystem.RaiseEvent(EventType.CLEAR_HIGHLIGHT);
                }
            }
        }

        public void ChangedInput(TMP_InputField input)
        {
            if (selectedTimelineClip != null && selectedInput)
            {
                Debug.Log($"changed input {selectedInput}");
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

        public void SetSelect(bool select)
        {
            selectedInput = select;
        }

        private void AddNewBrushClip(int brushStrokeID, float lastTime, float currentTime)
        {
            RectTransform rect = Instantiate(timelineClipObject, timelineBarObject.transform).GetComponent<RectTransform>();
            rect.GetComponent<RawImage>().color = clipColor;
            TimelineClip timelineClip = new TimelineClip(brushStrokeID, rect, timelineBarObject.GetComponent<RectTransform>(), timelineRect)
            {
                leftSideScaled = lastTime,
                rightSideScaled = currentTime,
            };
            clips.Add(timelineClip);
        }

        private void RemoveClip(int brushStrokeID)
        {
            Destroy(clips[brushStrokeID].rect.gameObject);
            clips.RemoveAt(brushStrokeID);
        }

        private void UpdateClip(int brushStrokeID, float lastTime, float currentTime)
        {
            TimelineClip timelineClip = clips[brushStrokeID];
            timelineClip.leftSideScaled = lastTime;
            timelineClip.rightSideScaled = currentTime;
        }
        private bool IsMouseOver(Vector3[] corners)
        {
            return Input.mousePosition.x > corners[0].x && Input.mousePosition.x < corners[2].x && 
                   Input.mousePosition.y > corners[0].y && Input.mousePosition.y < corners[2].y;
        }
    }
}
