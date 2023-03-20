using System.Collections.Generic;
using Drawing;
using Managers;
using TMPro;
using Undo;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class Timeline : MonoBehaviour
    {
        [SerializeField] private RectTransform timelineBarToInsantiateTo;
        [SerializeField] private GameObject timelineBarObject;
        [SerializeField] private GameObject timelineClipObject;
        [SerializeField] private GameObject timelineScrollBar;
        [SerializeField] private GameObject timelineObject;
        [SerializeField] private int amountTimelineBars;
        [SerializeField] private TMP_InputField clipLeftInput;
        [SerializeField] private TMP_InputField clipRightInput;
        [SerializeField] private Color notSelectedColor;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Slider speedSliderTimeline;
        
        private List<List<TimelineClip>> clipsOrderderd;
        private List<TimelineClip> selectedClips;
        private List<int> selectedClipPreviousBar;
        private List<Vector2> selectedClipsOriginalTime;
        private RectTransform timelineRect;
        private RectTransform timelineAreaRect;
        private RectTransform timelineScrollRect;
        private bool selectedInput;
        private float time;
        private float lastTimelineLeft;
        private float lastTimelineRight;
        private Vector2 previousMousePos;
        private Vector3[] corners;
        private CommandManager commandManager;
        private Drawing.Drawing drawer;
        private float timeIncrease;
        private bool shouldTimelinePause;
        private bool timelinePauseButton;
        private MouseAction lastMouseAction;
        private bool firstTimeSelected;

        private bool isMouseInsideTimeline => IsMouseOver(corners);

        private void Awake()
        {
            commandManager = FindObjectOfType<CommandManager>();
            timelineRect = timelineObject.GetComponent<RectTransform>();
            timelineScrollRect = timelineScrollBar.GetComponent<RectTransform>();

            selectedClips = new List<TimelineClip>();
            selectedClipPreviousBar = new List<int>();
            selectedClipsOriginalTime = new List<Vector2>();
            
            corners = new Vector3[4];
            timelineRect.GetWorldCorners(corners);
            
            clipsOrderderd = new List<List<TimelineClip>>();
            for (int i = 0; i < amountTimelineBars; i++)
            {
                clipsOrderderd.Add(new List<TimelineClip>());
            }
            
            amountTimelineBars = clipsOrderderd.Count - 1;
        }

        private void OnEnable()
        {
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_STROKE, RemoveClip);
            EventSystem<bool>.Subscribe(EventType.DRAW, SetTimlinePause);
            EventSystem<bool>.Subscribe(EventType.FINISHED_STROKE, SetTimlinePause);
            EventSystem<BrushStrokeID, float, float>.Subscribe(EventType.FINISHED_STROKE, AddNewBrushClip);
            EventSystem<BrushStrokeID, float, float, int>.Subscribe(EventType.UPDATE_CLIP, UpdateClip);
        }

        private void OnDisable()
        {
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_STROKE, RemoveClip);
            EventSystem<bool>.Unsubscribe(EventType.DRAW, SetTimlinePause);
            EventSystem<bool>.Unsubscribe(EventType.FINISHED_STROKE, SetTimlinePause);
            EventSystem<BrushStrokeID, float, float>.Unsubscribe(EventType.FINISHED_STROKE, AddNewBrushClip);
            EventSystem<BrushStrokeID, float, float, int>.Unsubscribe(EventType.UPDATE_CLIP, UpdateClip);
        }

        private void Update()
        {
            MoveTimelineIndicator();
            if (UIManager.isFullView)
            {
                TimelineClipsInput();
            }

            previousMousePos = Input.mousePosition;
        }
        public void SetTimelinePauseButton(Image _image)
        {
            if (timelinePauseButton)
            {
                shouldTimelinePause = false;
                _image.sprite = pauseSprite;
                _image.color = notSelectedColor;
            }
            else
            {
                shouldTimelinePause = true;
                _image.sprite = playSprite;
                _image.color = selectedColor;
            }
            
            timelinePauseButton = !timelinePauseButton;
        }
        private void SetTimlinePause(bool pause)
        {
            if (timelinePauseButton)
            {
                shouldTimelinePause = pause;
            }
        }
        private void MoveTimelineIndicator()
        {
            if (PlaceTimelineIndicator())
                return;

            if (shouldTimelinePause)
            {
                timeIncrease = Time.timeSinceLevelLoad;
                return;
            }
            
            timeIncrease = (Time.timeSinceLevelLoad - timeIncrease) / Mathf.Pow(speedSliderTimeline.value, 1.5f);
            time += timeIncrease;
            time %= 1.1f;
            MoveTimelineTimIndicator(time);
            EventSystem<float>.RaiseEvent(EventType.TIME, time);
            
            timeIncrease = Time.timeSinceLevelLoad;
        }
        private bool PlaceTimelineIndicator()
        {
            if (IsMouseOver(corners) && Input.GetMouseButton(1) && UIManager.isFullView)
            {
                var position = timelineScrollRect.position;
                timelineScrollRect.position = new Vector3(Input.mousePosition.x, position.y, position.z);
                time = Input.mousePosition.x.Remap(corners[0].x, corners[2].x, 0, 1);
                timeIncrease = Time.timeSinceLevelLoad;
                EventSystem<float>.RaiseEvent(EventType.TIME, time);
                return true;
            }
            return false;
        }
        private void MoveTimelineTimIndicator(float _time)
        {
            float xPos = _time.Remap(0, 1, corners[0].x, corners[2].x);
            var position = timelineScrollRect.position;
            position = new Vector3(xPos, position.y, position.z);
            timelineScrollRect.position = position;
        }

        #region ClipInput
        private void TimelineClipsInput()
        {
            //If you are already interacting with a timelineclip check that one first
            if (Input.GetMouseButton(0) && selectedClips.Count > 0)
            {
                TimelineClip selectedClip = null;
                foreach (var clip in selectedClips)
                {
                    MouseAction mouseAction = clip.GetMouseAction();
                    if (mouseAction != MouseAction.Nothing)
                    {
                        lastMouseAction = mouseAction;
                        selectedClip = clip;
                    }
                }
                if (selectedClip != null)
                {
                    if (firstTimeSelected)
                    {
                        selectedClipPreviousBar.Clear();
                        for (int index = 0; index < selectedClips.Count; index++)
                        {
                            var clip = selectedClips[index];
                            selectedClipPreviousBar.Add(clip.currentBar);
                            clip.barOffset = clip.currentBar - selectedClip.currentBar;
                            clip.SetupMovement(lastMouseAction);
                        }
                        firstTimeSelected = false;
                    }

                    foreach (var clip in selectedClips)
                    {
                        clip.mouseAction = lastMouseAction;
                        clip.UpdateTransform(previousMousePos);
                        // clipLeftInput.text = clip.leftSideScaled.ToString("0.###");
                        // clipRightInput.text = clip.rightSideScaled.ToString("0.###");
                        
                        //Look into doing this more efficiently
                        EventSystem<BrushStrokeID, float, float>.RaiseEvent(
                            EventType.REDRAW_STROKE, clip.brushStrokeID, clip.leftSideScaled, clip.rightSideScaled);
                    }
                }
            }
            //Otherwise check if you are interacting with any other timeline clips
            if (Input.GetMouseButton(0) && isMouseInsideTimeline)
            {
                for (int i = 0; i < clipsOrderderd.Count; i++)
                {
                    for (int j = 0; j < clipsOrderderd[i].Count; j++)
                    {
                        if (selectedClips.Contains(clipsOrderderd[i][j]))
                        {
                            continue;
                        }
                        
                        MouseAction mouseAction = clipsOrderderd[i][j].GetMouseAction();
                        if (mouseAction == MouseAction.Nothing)
                        {
                            continue;
                        }

                        if (selectedClips.Count == 0 || Input.GetKey(KeyCode.LeftShift))
                        {
                            clipsOrderderd[i][j].SetupMovement(mouseAction);
                            clipsOrderderd[i][j].rawImage.color = selectedColor;
                            EventSystem<BrushStrokeID>.RaiseEvent(EventType.HIGHLIGHT, clipsOrderderd[i][j].brushStrokeID);
                        
                            selectedClipPreviousBar.Add(0);
                            selectedClips.Add(clipsOrderderd[i][j]);
                            selectedClipsOriginalTime.Add(new Vector2(clipsOrderderd[i][j].leftSideScaled, clipsOrderderd[i][j].rightSideScaled));
                            return;
                        }
                    }
                }
            }
            //Once you are done making changes 
            else if (Input.GetMouseButtonUp(0))
            {
                lastMouseAction = MouseAction.Nothing;
                firstTimeSelected = true;
                if (selectedClips.Count > 0)
                {
                    List<RedrawCommand> redraws = new List<RedrawCommand>();
                    for (int i = 0; i < selectedClips.Count; i++)
                    {
                        selectedClips[i].mouseAction = MouseAction.Nothing;
                        CheckClipCollisions(selectedClips[i], selectedClipPreviousBar[i]);
                    
                        RedrawCommand clipCommand = new RedrawCommand(
                            selectedClips[i].brushStrokeID, selectedClips[i].leftSideScaled, selectedClips[i].rightSideScaled, 
                            selectedClipsOriginalTime[i].x, selectedClipsOriginalTime[i].y, selectedClipPreviousBar[i]);
                        redraws.Add(clipCommand);
                    }
                    ICommand redrawCommand = new RedrawMultipleCommand(redraws);
                    commandManager.AddCommand(redrawCommand);
                }
            }
            //once you have clicked somewhere else unselect everything
            if (selectedClips.Count > 0 && !selectedInput)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Vector3[] clipCorners = new Vector3[4];
                    bool clickedOnSelectedClip = false;
                
                    foreach (var clip in selectedClips)
                    {
                        clip.rect.GetWorldCorners(clipCorners);
                        if (IsMouseOver(clipCorners))
                        {
                            clickedOnSelectedClip = true;
                        }
                    }
                    if (clickedOnSelectedClip)
                        return;
                
                    
                    for (int i = 0; i < selectedClips.Count; i++)
                    {
                        selectedClips[i].rawImage.color = notSelectedColor;
                    }
                    
                    ClearSelectedClips();
                }

                if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
                {
                    foreach (var clip in selectedClips)
                    {
                        Destroy(clip.rect.gameObject);
                        clipsOrderderd[clip.currentBar].Remove(clip);
                    }
                    
                    EventSystem<BrushStrokeID>.RaiseEvent(EventType.DELETE_CLIP, selectedClips[0].brushStrokeID);

                    ClearSelectedClips();
                }
            }
        }
        private void ClearSelectedClips()
        {
            EventSystem.RaiseEvent(EventType.CLEAR_HIGHLIGHT);
            clipLeftInput.text = "";
            clipRightInput.text = "";

            selectedClips.Clear();
            selectedClipPreviousBar.Clear();
            selectedClipsOriginalTime.Clear();
        }

        private void CheckClipCollisions(TimelineClip _clip, int _previousBar)
        {
            int currentBar = _clip.currentBar;
        
            //Check if there is a collision on its current bar
            if (!IsClipCollidingInBar(_clip, currentBar))
            {
                clipsOrderderd[currentBar].Add(_clip);
                clipsOrderderd[_previousBar].Remove(_clip);
                return;
            }
            
            //Check other bars in an increasing alternating order if there is space
            for (int i = 1; i < Mathf.CeilToInt(amountTimelineBars) + 1; i++)
            {
                int upperBar = currentBar - i;
                int lowerBar = currentBar + i;
        
                if (lowerBar <= amountTimelineBars)
                {
                    if (!IsClipCollidingInBar(_clip, lowerBar))
                    {
                        _clip.SetBar(lowerBar);
                        clipsOrderderd[lowerBar].Add(_clip);
                        clipsOrderderd[_previousBar].Remove(_clip);
                        return;
                    }
                }
        
                if (upperBar >= 0)
                {
                    if (!IsClipCollidingInBar(_clip, upperBar))
                    {
                        _clip.SetBar(upperBar);
                        clipsOrderderd[upperBar].Add(_clip);
                        clipsOrderderd[_previousBar].Remove(_clip);
                        return;
                    }
                }
            }
            
            //There is no space anywhere create a new one timeline bar
            amountTimelineBars++;
            Instantiate(timelineBarObject, timelineRect).transform.SetAsFirstSibling();
            clipsOrderderd.Add(new List<TimelineClip>());
            
            int lowestBar = clipsOrderderd.Count - 1;
            _clip.SetBar(lowestBar);
            clipsOrderderd[lowestBar].Add(_clip);
            clipsOrderderd[_previousBar].Remove(_clip);
        }

        private bool IsClipCollidingInBar(TimelineClip _clip, int bar)
        {
            for (int i = 0; i < clipsOrderderd[bar].Count; i++)
            {
                if (clipsOrderderd[bar][i] == _clip)
                {
                    continue;
                }
                if (IsClipColliding(_clip, clipsOrderderd[bar][i]))
                {
                    return true;
                }
            }
            return false;
        }
        private bool IsClipColliding(TimelineClip _clip, TimelineClip _clip2)
        {
            if (_clip.leftSideScaled > _clip2.leftSideScaled && _clip.leftSideScaled < _clip2.rightSideScaled ||
                _clip.rightSideScaled > _clip2.leftSideScaled && _clip.rightSideScaled < _clip2.rightSideScaled
                )
            {
                return true;
            }
            if (_clip2.leftSideScaled > _clip.leftSideScaled && _clip2.leftSideScaled < _clip.rightSideScaled ||
                _clip2.rightSideScaled > _clip.leftSideScaled && _clip2.rightSideScaled < _clip.rightSideScaled
               )
            {
                return true;
            }
            return false;
        }
        #endregion
        
        public void ChangedInput(TMP_InputField _input)
        {
            if (selectedClips.Count == 1 && selectedInput)
            {
                Debug.Log($"changed input {selectedInput}");
                if (selectedClips[0].mouseAction == MouseAction.Nothing)
                {
                    float leftSide = float.Parse(clipLeftInput.text);
                    float rightSide = float.Parse(clipRightInput.text);
                    if (leftSide < rightSide)
                    {
                        if (_input == clipLeftInput)
                        {
                            selectedClips[0].leftSideScaled = Mathf.Clamp01(leftSide);
                        }
                        else
                        {
                            selectedClips[0].rightSideScaled = Mathf.Clamp01(rightSide);
                        }
                        EventSystem<BrushStrokeID, float, float>.RaiseEvent(EventType.REDRAW_STROKE, selectedClips[0].brushStrokeID, 
                                                                  selectedClips[0].leftSideScaled, selectedClips[0].rightSideScaled);
                    }
                }
            }
        }

        public void SetSelect(bool _select)
        {
            selectedInput = _select;
        }

        private void AddNewBrushClip(BrushStrokeID _brushStrokeID, float _lastTime, float _currentTime)
        {
            RectTransform rect = Instantiate(timelineClipObject, timelineBarToInsantiateTo).GetComponent<RectTransform>();
            RawImage clipImage = rect.GetComponent<RawImage>();
            clipImage.color = notSelectedColor;
            TimelineClip timelineClip = new TimelineClip(_brushStrokeID, rect, timelineBarToInsantiateTo, timelineRect, clipImage)
            {
                leftSideScaled = _lastTime,
                rightSideScaled = _currentTime,
            };
            clipsOrderderd[0].Add(timelineClip);
            CheckClipCollisions(timelineClip, 0);
        }

        private void RemoveClip(BrushStrokeID _brushStrokeID)
        {
            for (int i = 0; i < clipsOrderderd.Count; i++)
            {
                for (int j = 0; j < clipsOrderderd[i].Count; j++)
                {
                    if (clipsOrderderd[i][j].brushStrokeID == _brushStrokeID)
                    {
                        Destroy(clipsOrderderd[i][j].rect.gameObject);
                        clipsOrderderd[i].RemoveAt(j);
                        
                        if (selectedClips.Count > 0)
                        {
                            if (selectedClips.Contains(clipsOrderderd[i][j]))
                            {
                                selectedClips.Remove(clipsOrderderd[i][j]);
                            }
                        }
                        return;
                    }
                }
            }
        }

        private void UpdateClip(BrushStrokeID _brushStrokeID, float _lastTime, float _currentTime, int _setBar)
        {
            for (int i = 0; i < clipsOrderderd.Count; i++)
            {
                for (int j = 0; j < clipsOrderderd[i].Count; j++)
                {
                    if (clipsOrderderd[i][j].brushStrokeID == _brushStrokeID)
                    {
                        var clip = clipsOrderderd[i][j];
                        
                        clip.leftSideScaled = _lastTime;
                        clip.rightSideScaled = _currentTime;
                        
                        clipsOrderderd[clip.currentBar].Remove(clip);
                        clipsOrderderd[_setBar].Add(clip);
                        clip.SetBar(_setBar);
                        return;
                    }
                }
            }
        }
        private bool IsMouseOver(Vector3[] _corners)
        {
            return Input.mousePosition.x > _corners[0].x && Input.mousePosition.x < _corners[2].x && 
                   Input.mousePosition.y > _corners[0].y && Input.mousePosition.y < _corners[2].y;
        }
    }
}
