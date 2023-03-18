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
        private ClipIndex selectedClipIndex;
        private TimelineClip selectedTimelineClip;
        private CommandManager commandManager;
        private Drawing.Drawing drawer;

        private void Awake()
        {
            commandManager = FindObjectOfType<CommandManager>();
            corners = new Vector3[4];
            timelineRect = GetComponent<RectTransform>();
            timelineScrollRect = timelineScrollBar.GetComponent<RectTransform>();
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
                if (Input.GetMouseButtonDown(0))
                {
                    selectedClipIndex = new ClipIndex(selectedTimelineClip.currentBar, clipsOrderderd[selectedTimelineClip.currentBar].Count - 1);
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
                for (int i = 0; i < clipsOrderderd.Count; i++)
                {
                    
                    for (int j = 0; j < clipsOrderderd[i].Count; j++)
                    {
                        clipsOrderderd[i][j].UpdateUI(Input.mousePosition, previousMousePos);
                        if (clipsOrderderd[i][j].mouseAction == MouseAction.Nothing)
                        {
                            continue;
                        }

                        if (oldClipTime.x < 0)
                        {
                            oldClipTime.x = clipsOrderderd[i][j].leftSideScaled;
                        }
                        if (oldClipTime.y < 0)
                        {
                            oldClipTime.y = clipsOrderderd[i][j].rightSideScaled;
                        }

                        clipsOrderderd[i][j].rect.GetComponent<RawImage>().color = selectedClipColor;
                        EventSystem<int>.RaiseEvent(EventType.HIGHLIGHT, clipsOrderderd[i][j].brushStrokeID);
                        
                        selectedClipIndex = new ClipIndex(i, j);
                        selectedTimelineClip = clipsOrderderd[i][j];
                        clipLeftInput.text = clipsOrderderd[i][j].leftSideScaled.ToString("0.###");
                        clipRightInput.text = clipsOrderderd[i][j].rightSideScaled.ToString("0.###");
                        EventSystem<int, float, float>.RaiseEvent(EventType.REDRAW_STROKE, clipsOrderderd[i][j].brushStrokeID, 
                                                                  clipsOrderderd[i][j].leftSideScaled, clipsOrderderd[i][j].rightSideScaled);
                        break;
                    }
                }
            }
            //Once you are done making changes 
            else if (Input.GetMouseButtonUp(0))
            {
                if (selectedTimelineClip is not null)
                {
                    ICommand clipCommand = new RedrawCommand(
                        selectedTimelineClip.brushStrokeID, selectedTimelineClip.leftSideScaled,
                        selectedTimelineClip.rightSideScaled, oldClipTime.x, oldClipTime.y);
                    commandManager.Execute(clipCommand);
                    
                    selectedTimelineClip.mouseAction = MouseAction.Nothing;
                    if (selectedClipIndex.bar != selectedTimelineClip.currentBar)
                    {
                        Debug.Log(IsClipCollidingInBar(GetClip(selectedClipIndex), selectedTimelineClip.currentBar));
                        clipsOrderderd[selectedTimelineClip.currentBar].Add(GetClip(selectedClipIndex));
                        clipsOrderderd[selectedClipIndex.bar].RemoveAt(selectedClipIndex.barIndex);
                    }

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

        private TimelineClip GetClip(ClipIndex _clipIndex)
        {
            return clipsOrderderd[_clipIndex.bar][_clipIndex.barIndex];
        }

        private bool IsClipCollidingInBar(TimelineClip _clip, int bar)
        {
            foreach (var otherClip in clipsOrderderd[bar])
            {
                if(otherClip == _clip) { continue;}
                if (IsClipColliding(_clip, otherClip))
                {
                    return true;
                }
            }
            return false;
        }
        private bool IsClipColliding(TimelineClip _clip, TimelineClip _clip2)
        {
            Vector3[] clipCorners = new Vector3[4];
            _clip.rect.GetWorldCorners(clipCorners);
            Vector4 collisionBox = new Vector4(clipCorners[0].x, clipCorners[0].y, clipCorners[2].x, clipCorners[2].y);
            _clip2.rect.GetWorldCorners(clipCorners);
            Vector4 collisionBox2 = new Vector4(clipCorners[0].x, clipCorners[0].y, clipCorners[2].x, clipCorners[2].y);
            
            return collisionBox.x <= collisionBox2.z && collisionBox.z >= collisionBox2.x && collisionBox.y <= collisionBox2.w && collisionBox.w >= collisionBox2.y;
        }

        public void ChangedInput(TMP_InputField _input)
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
                        if (_input == clipLeftInput)
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

        public void SetSelect(bool _select)
        {
            selectedInput = _select;
        }

        private void AddNewBrushClip(int _brushStrokeID, float _lastTime, float _currentTime)
        {
            RectTransform rect = Instantiate(timelineClipObject, timelineBarObject.transform).GetComponent<RectTransform>();
            rect.GetComponent<RawImage>().color = clipColor;
            TimelineClip timelineClip = new TimelineClip(_brushStrokeID, rect, timelineBarObject.GetComponent<RectTransform>(), timelineRect)
            {
                leftSideScaled = _lastTime,
                rightSideScaled = _currentTime,
            };
            clipsOrderderd[0].Add(timelineClip);
        }

        private void RemoveClip(int _brushStrokeID)
        {
            for (int i = 0; i < clipsOrderderd.Count; i++)
            {
                for (int j = 0; j < clipsOrderderd[i].Count; j++)
                {
                    if (clipsOrderderd[i][j].brushStrokeID == _brushStrokeID)
                    {
                        Destroy(clipsOrderderd[i][j].rect.gameObject);
                        clipsOrderderd[i].RemoveAt(j);
                    }
                }
            }
        }

        private void UpdateClip(int _brushStrokeID, float _lastTime, float _currentTime)
        {
            // TimelineClip timelineClip = clips[brushStrokeID];
            // timelineClip.leftSideScaled = lastTime;
            // timelineClip.rightSideScaled = currentTime;
        }
        private bool IsMouseOver(Vector3[] _corners)
        {
            return Input.mousePosition.x > _corners[0].x && Input.mousePosition.x < _corners[2].x && 
                   Input.mousePosition.y > _corners[0].y && Input.mousePosition.y < _corners[2].y;
        }
    }

    internal struct ClipIndex
    {
        public int bar;
        public int barIndex;

        public ClipIndex(int _bar, int _barIndex)
        {
            bar = _bar;
            barIndex = _barIndex;
        }
    }
}
