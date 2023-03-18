using System.Collections.Generic;
using Managers;
using TMPro;
using Undo;
using UnityEngine;

namespace UI
{
    public class Timeline : MonoBehaviour
    {
        [SerializeField] private GameObject timelineBarObject;
        [SerializeField] private GameObject timelineClipObject;
        [SerializeField] private GameObject timelineScrollBar;
        [SerializeField] private List<GameObject> timelineBars;
        [SerializeField] private TMP_InputField clipLeftInput;
        [SerializeField] private TMP_InputField clipRightInput;
        [SerializeField] private List<TimelineClip> clips;
        private RectTransform timelineRect;
        private RectTransform timelineAreaRect;
        private RectTransform timelineScrollRect;
        private bool selectedInput;
        private bool startTimelineClip;
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

        private void SetTime(float time)
        {
            this.time = time;
        }

        private void Update()
        {
            timelineRect.GetWorldCorners(corners);
            float xPos = time.Remap(0, 1, corners[0].x, corners[2].x);
            var position = timelineScrollRect.position;
            position = new Vector3(xPos, position.y, position.z);
            timelineScrollRect.position = position;

            //If you are already interacting with a timelineclip check that one first
            if (Input.GetMouseButton(0) && selectedTimelineClip != null)
            {
                if (startTimelineClip)
                {
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
            //Reset once 
            else if (Input.GetMouseButtonUp(0))
            {
                // foreach (TimelineClip clip in clips)
                // {
                //     clip.mouseAction = MouseAction.Nothing;
                // }
                startTimelineClip = true;
                if (selectedTimelineClip != null)
                {
                    ICommand clipCommand = new RedrawCommand(selectedTimelineClip.brushStrokeID, selectedTimelineClip.leftSideScaled, 
                                                             selectedTimelineClip.rightSideScaled, oldClipTime.x, oldClipTime.y);
                    commandManager.Execute(clipCommand);
                    
                    oldClipTime.x = -1;
                    oldClipTime.y = -1;
                    selectedTimelineClip.mouseAction = MouseAction.Nothing;
                    
                    EventSystem.RaiseEvent(EventType.CLEAR_HIGHLIGHT);
                }
                selectedTimelineClip = null;
            }
            
            previousMousePos = Input.mousePosition;
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
            Destroy(clips[brushStrokeID].rect.gameObject);
            clips.RemoveAt(brushStrokeID);
        }

        private void UpdateClip(int brushStrokeID, float lastTime, float currentTime)
        {
            TimelineClip timelineClip = clips[brushStrokeID];
            timelineClip.leftSideScaled = lastTime;
            timelineClip.rightSideScaled = currentTime;
        }
    }
}
