using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private TMP_InputField clipLeftInput;
        [SerializeField] private TMP_InputField clipRightInput;
        [SerializeField] private Color notSelectedColor;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite playSprite;
        [SerializeField] private Slider speedSliderTimeline;
        
        private List<List<TimelineClip>> clipsOrdered;
        private List<TimelineClip> selectedClips;
        private RectTransform timelineRect;
        private RectTransform timelineAreaRect;
        private RectTransform timelineScrollRect;
        private bool selectedInput;
        private int amountTimelineBars;
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
        private bool firstTimeSelected;
        private bool shouldMoveTimeline;
        private MouseAction lastMouseAction;

        private bool isMouseInsideTimeline => IsMouseOver(corners);

        private void Awake()
        {
            amountTimelineBars = timelineObject.transform.childCount;
            commandManager = FindObjectOfType<CommandManager>();
            timelineRect = timelineObject.GetComponent<RectTransform>();
            timelineScrollRect = timelineScrollBar.GetComponent<RectTransform>();

            selectedClips = new List<TimelineClip>();
            
            corners = new Vector3[4];
            timelineRect.GetWorldCorners(corners);
            
            clipsOrdered = new List<List<TimelineClip>>();
            for (int i = 0; i < amountTimelineBars; i++)
            {
                clipsOrdered.Add(new List<TimelineClip>());
            }
            
            amountTimelineBars = clipsOrdered.Count - 1;
        }

        private void OnEnable()
        {
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_STROKE, RemoveClip);
            EventSystem<bool>.Subscribe(EventType.DRAW, SetTimlinePause);
            EventSystem<bool>.Subscribe(EventType.FINISHED_STROKE, SetTimlinePause);
            EventSystem<BrushStrokeID>.Subscribe(EventType.FINISHED_STROKE, AddNewBrushClip);
            EventSystem<TimelineClip>.Subscribe(EventType.ADD_STROKE, AddNewBrushClip);
            EventSystem<TimelineClip, int>.Subscribe(EventType.UPDATE_CLIP, UpdateClip);
            EventSystem<List<TimelineClip>>.Subscribe(EventType.REMOVE_STROKE, RemoveClip);
        }

        private void OnDisable()
        {
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_STROKE, RemoveClip);
            EventSystem<bool>.Unsubscribe(EventType.DRAW, SetTimlinePause);
            EventSystem<bool>.Unsubscribe(EventType.FINISHED_STROKE, SetTimlinePause);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.FINISHED_STROKE, AddNewBrushClip);
            EventSystem<TimelineClip>.Unsubscribe(EventType.ADD_STROKE, AddNewBrushClip);
            EventSystem<TimelineClip, int>.Unsubscribe(EventType.UPDATE_CLIP, UpdateClip);
            EventSystem<List<TimelineClip>>.Unsubscribe(EventType.REMOVE_STROKE, RemoveClip);
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
            if (Input.GetMouseButtonDown(1) && IsMouseOver(corners))
            {
                shouldMoveTimeline = true;
            }
            if (Input.GetMouseButtonUp(1))
            {
                shouldMoveTimeline = false;
            }
            
            if (!UIManager.isFullView || !shouldMoveTimeline)
                return false;
            
            var position = timelineScrollRect.position;
            timelineScrollRect.position = new Vector3(Input.mousePosition.x, position.y, position.z);
            time = Input.mousePosition.x.Remap(corners[0].x, corners[2].x, 0, 1);
            timeIncrease = Time.timeSinceLevelLoad;
            EventSystem<float>.RaiseEvent(EventType.TIME, time);
            return true;
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
                        for (int i = 0; i < selectedClips.Count; i++)
                        {
                            selectedClips[i].previousBar = selectedClips[i].currentBar;
                            selectedClips[i].lastLeftSideScaled = selectedClips[i].leftSideScaled;
                            selectedClips[i].lastRightSideScaled = selectedClips[i].rightSideScaled;
                            
                            var clip = selectedClips[i];
                            clip.barOffset = clip.currentBar - selectedClip.currentBar;
                            clip.SetupMovement(lastMouseAction);
                        }
                        firstTimeSelected = false;
                    }

                    List<BrushStrokeID> brushStrokeIDs = new List<BrushStrokeID>();
                    for (int i = 0; i < selectedClips.Count; i++)
                    {
                        var clip = selectedClips[i];
                        clip.mouseAction = lastMouseAction;
                        clip.UpdateTransform(previousMousePos);
                        // clipLeftInput.text = clip.leftSideScaled.ToString("0.###");
                        // clipRightInput.text = clip.rightSideScaled.ToString("0.###");

                        if (Math.Abs(selectedClips[i].lastLeftSideScaled - selectedClips[i].leftSideScaled) > 0.001 ||
                            Math.Abs(selectedClips[i].lastRightSideScaled - selectedClips[i].rightSideScaled) > 0.001)
                        {
                            clip.brushStrokeID.lastTime = clip.leftSideScaled;
                            clip.brushStrokeID.currentTime = clip.rightSideScaled;
                            brushStrokeIDs.Add(clip.brushStrokeID);
                        }
                    }
                    if (brushStrokeIDs.Count > 0) { EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.REDRAW_STROKES, brushStrokeIDs); }
                }
            }
            //Otherwise check if you are interacting with any other timeline clips
            timelineRect.GetWorldCorners(corners);
            if (Input.GetMouseButton(0) && isMouseInsideTimeline)
            {
                for (int i = 0; i < clipsOrdered.Count; i++)
                {
                    for (int j = 0; j < clipsOrdered[i].Count; j++)
                    {
                        if (selectedClips.Contains(clipsOrdered[i][j]))
                        {
                            continue;
                        }
                        
                        MouseAction mouseAction = clipsOrdered[i][j].GetMouseAction();
                        if (mouseAction == MouseAction.Nothing)
                        {
                            continue;
                        }

                        if (selectedClips.Count == 0 || Input.GetKey(KeyCode.LeftShift))
                        {
                            clipsOrdered[i][j].SetupMovement(mouseAction);
                            clipsOrdered[i][j].rawImage.color = selectedColor;
                            EventSystem<BrushStrokeID>.RaiseEvent(EventType.HIGHLIGHT, clipsOrdered[i][j].brushStrokeID);
                        
                            selectedClips.Add(clipsOrdered[i][j]);
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
                        CheckClipCollisions(selectedClips[i]);

                        RedrawCommand clipCommand = new RedrawCommand(selectedClips[i]);
                        redraws.Add(clipCommand);
                    }
                    if (redraws.Count > 0)
                    {
                        commandManager.AddCommand(new RedrawMultipleCommand(redraws));
                    }
                }
            }
            
            if (selectedClips.Count > 0 && !selectedInput)
            {
                //once you have clicked somewhere else unselect everything
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

                //If you press backspace delete all selected timelineclips
                if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
                {
                    EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.REMOVE_STROKE, selectedClips.Select(_clip => _clip.brushStrokeID).ToList());
                    
                    List<TimelineClip> timelineClips = new List<TimelineClip>(selectedClips);
                    ICommand deleteMultiple = new DeleteClipMultipleCommand(timelineClips);
                    commandManager.AddCommand(deleteMultiple);
                    
                    RemoveClip(selectedClips);
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
        }

        private void CheckClipCollisions(TimelineClip _clip)
        {
            int currentBar = _clip.currentBar;
            int previousBar = _clip.previousBar;
        
            //Check if there is a collision on its current bar
            if (!IsClipCollidingInBar(_clip, currentBar))
            {
                clipsOrdered[currentBar].Add(_clip);
                clipsOrdered[previousBar].Remove(_clip);
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
                        clipsOrdered[lowerBar].Add(_clip);
                        clipsOrdered[previousBar].Remove(_clip);
                        return;
                    }
                }
        
                if (upperBar >= 0)
                {
                    if (!IsClipCollidingInBar(_clip, upperBar))
                    {
                        _clip.SetBar(upperBar);
                        clipsOrdered[upperBar].Add(_clip);
                        clipsOrdered[previousBar].Remove(_clip);
                        return;
                    }
                }
            }
            
            //There is no space anywhere create a new one timeline bar
            amountTimelineBars++;
            Instantiate(timelineBarObject, timelineRect).transform.SetAsFirstSibling();
            clipsOrdered.Add(new List<TimelineClip>());
            
            int lowestBar = clipsOrdered.Count - 1;
            _clip.SetBar(lowestBar);
            clipsOrdered[lowestBar].Add(_clip);
            clipsOrdered[previousBar].Remove(_clip);
        }

        private bool IsClipCollidingInBar(TimelineClip _clip, int bar)
        {
            for (int i = 0; i < clipsOrdered[bar].Count; i++)
            {
                if (clipsOrdered[bar][i] == _clip)
                {
                    continue;
                }
                if (IsClipColliding(_clip, clipsOrdered[bar][i]))
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
                        EventSystem<BrushStrokeID>.RaiseEvent(EventType.REDRAW_STROKE, selectedClips[0].brushStrokeID);
                    }
                }
            }
        }

        public void SetSelect(bool _select)
        {
            selectedInput = _select;
        }

        private void AddNewBrushClip(BrushStrokeID _brushStrokeID)
        {
            RectTransform rect = Instantiate(timelineClipObject, timelineBarToInsantiateTo).GetComponent<RectTransform>();
            RawImage clipImage = rect.GetComponent<RawImage>();
            clipImage.color = notSelectedColor;
            TimelineClip timelineClip = new TimelineClip(_brushStrokeID, rect, timelineBarToInsantiateTo, timelineRect, clipImage)
            {
                leftSideScaled = _brushStrokeID.lastTime,
                rightSideScaled = _brushStrokeID.currentTime,
            };
            clipsOrdered[0].Add(timelineClip);
            CheckClipCollisions(timelineClip);
        }
        private void AddNewBrushClip(TimelineClip _timelineClip)
        {
            int currentBar = _timelineClip.currentBar;
            _timelineClip.previousBar = currentBar;
            RectTransform rect = Instantiate(timelineClipObject, timelineBarToInsantiateTo).GetComponent<RectTransform>();
            RawImage clipImage = rect.GetComponent<RawImage>();
            _timelineClip.rawImage = clipImage;
            _timelineClip.rect = rect;
            _timelineClip.currentBar = 0;
            _timelineClip.leftSideScaled = _timelineClip.brushStrokeID.lastTime;
            _timelineClip.rightSideScaled = _timelineClip.brushStrokeID.currentTime;
            _timelineClip.rawImage.color = notSelectedColor;
            clipsOrdered[currentBar].Add(_timelineClip);
            _timelineClip.SetBar(currentBar);
            CheckClipCollisions(_timelineClip);
        }

        private void RemoveClip(BrushStrokeID _brushStrokeID)
        {
            for (int i = 0; i < clipsOrdered.Count; i++)
            {
                for (int j = 0; j < clipsOrdered[i].Count; j++)
                {
                    if (clipsOrdered[i][j].brushStrokeID == _brushStrokeID)
                    {
                        if (selectedClips.Count > 0)
                        {
                            if (selectedClips.Contains(clipsOrdered[i][j]))
                            {
                                selectedClips.Remove(clipsOrdered[i][j]);
                            }
                        }
                        Destroy(clipsOrdered[i][j].rect.gameObject);
                        clipsOrdered[i].RemoveAt(j);
                        return;
                    }
                }
            }
        }
        private void RemoveClip(List<TimelineClip> _timelineClips)
        {
            foreach (var clip in _timelineClips)
            {
                //Add boolean to check if you should use previous or current timerline bar
                Destroy(clip.rect.gameObject);
                foreach (var timelineBar in clipsOrdered)
                {
                    timelineBar.Remove(clip);
                }
            }
        }
        private void UpdateClip(TimelineClip _timelineClip, int _setBar)
        {
            clipsOrdered[_timelineClip.currentBar].Remove(_timelineClip);
            clipsOrdered[_setBar].Add(_timelineClip);
            _timelineClip.SetBar(_setBar);
            _timelineClip.previousBar = _setBar;
            CheckClipCollisions(_timelineClip);
        }
        private bool IsMouseOver(Vector3[] _corners)
        {
            return Input.mousePosition.x > _corners[0].x && Input.mousePosition.x < _corners[2].x && 
                   Input.mousePosition.y > _corners[0].y && Input.mousePosition.y < _corners[2].y;
        }
    }
}
