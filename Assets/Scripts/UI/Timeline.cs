using System;
using System.Collections.Generic;
using System.Linq;
using Drawing;
using Managers;
using TMPro;
using Undo;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using EventType = Managers.EventType;

namespace UI
{
    public class Timeline : MonoBehaviour
    {
        [Header("Timeline objects")]
        [SerializeField] private RectTransform timelineBarObject;
        [SerializeField] private List<RectTransform> timelineBarObjects;
        [SerializeField] private GameObject timelineClipObject;
        [SerializeField] private GameObject timelineScrollBar;
        [SerializeField] private GameObject timelineObject;
        
        [Header("Timeline UI")]
        [SerializeField] private TMP_InputField clipLeftInput;
        [SerializeField] private TMP_InputField clipRightInput;
        [SerializeField] private Slider speedSliderTimeline;
        
        [Header("Select Deselect")]
        [SerializeField] private Color notSelectedColor;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite playSprite;

        [Header("Timeline Input")]
        [SerializeField] private float timelineScaleSensitivity = 10;
        [SerializeField] private float timelineMaxScaleMultiplier = 20;
        
        private List<List<TimelineClip>> clipsOrdered;
        private List<TimelineClip> selectedClips;
        private TimelineClip lastSelectedClip;
        private RectTransform timelineRect;
        private RectTransform timelineAreaRect;
        private RectTransform timelineScrollRect;
        private bool selectedInput;
        private int amountTimelineBars;
        private float minTimelineSize;
        private float maxTimelineSize;
        private float time;
        private float lastTimelineLeft;
        private float lastTimelineRight;
        private Vector2 previousMousePos;
        private Vector3[] corners;
        private CommandManager commandManager;
        private float timeIncrease;
        private bool shouldTimelinePause;
        private bool timelinePauseButton;
        private bool firstTimeSelected;
        private bool shouldMoveTimeline;
        private MouseAction lastMouseAction;
        private float startClickPos;
        private bool isInteracting;

        private Vector3[] Corners
        {
            get {
                timelineRect.GetWorldCorners(corners);
                return corners;
            }
        }
        private bool isMouseInsideTimeline => IsMouseOver(Corners);

        private void Awake()
        {
            amountTimelineBars = timelineObject.transform.childCount;
            commandManager = FindObjectOfType<CommandManager>();
            timelineRect = timelineObject.GetComponent<RectTransform>();
            timelineScrollRect = timelineScrollBar.GetComponent<RectTransform>();
            minTimelineSize = timelineBarObject.sizeDelta.x;
            maxTimelineSize = minTimelineSize * timelineMaxScaleMultiplier;

            selectedClips = new List<TimelineClip>();
            
            corners = new Vector3[4];
            
            clipsOrdered = new List<List<TimelineClip>>();
            for (int i = 0; i < amountTimelineBars; i++)
            {
                clipsOrdered.Add(new List<TimelineClip>());
            }
            
            amountTimelineBars = clipsOrdered.Count - 1;
        }

        private void OnEnable()
        {
            EventSystem.Subscribe(EventType.CLEAR_SELECT, ClearSelected);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_STROKE, RemoveClip);
            EventSystem<bool>.Subscribe(EventType.DRAW, SetTimlinePause);
            EventSystem<bool>.Subscribe(EventType.FINISHED_STROKE, SetTimlinePause);
            EventSystem<BrushStrokeID>.Subscribe(EventType.FINISHED_STROKE, AddNewBrushClip);
            EventSystem<TimelineClip>.Subscribe(EventType.ADD_STROKE, AddNewBrushClip);
            EventSystem<TimelineClip, int>.Subscribe(EventType.UPDATE_CLIP, UpdateClip);
            EventSystem<List<TimelineClip>>.Subscribe(EventType.REMOVE_STROKE, RemoveClip);
            EventSystem<BrushStrokeID>.Subscribe(EventType.SELECT_TIMELINECLIP, SelectClip);
            EventSystem<BrushStrokeID>.Subscribe(EventType.REMOVE_SELECT, RemoveSelectedClip);
            EventSystem<float>.Subscribe(EventType.ADD_TIME, AddTime);
        }

        private void OnDisable()
        {
            EventSystem.Unsubscribe(EventType.CLEAR_SELECT, ClearSelected);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_STROKE, RemoveClip);
            EventSystem<bool>.Unsubscribe(EventType.DRAW, SetTimlinePause);
            EventSystem<bool>.Unsubscribe(EventType.FINISHED_STROKE, SetTimlinePause);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.FINISHED_STROKE, AddNewBrushClip);
            EventSystem<TimelineClip>.Unsubscribe(EventType.ADD_STROKE, AddNewBrushClip);
            EventSystem<TimelineClip, int>.Unsubscribe(EventType.UPDATE_CLIP, UpdateClip);
            EventSystem<List<TimelineClip>>.Unsubscribe(EventType.REMOVE_STROKE, RemoveClip);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.SELECT_TIMELINECLIP, SelectClip);
            EventSystem<BrushStrokeID>.Unsubscribe(EventType.REMOVE_SELECT, RemoveSelectedClip);
            EventSystem<float>.Unsubscribe(EventType.ADD_TIME, AddTime);
        }

        private void Update()
        {
            if (Input.GetMouseButtonUp(0))
            {
                isInteracting = false;
                EventSystem<bool>.RaiseEvent(EventType.IS_INTERACTING, false);
            }
            
            MoveTimelineIndicator();
            
            if (UIManager.isFullView)
            {
                TimelineClipsInput();
            }
            
            previousMousePos = Input.mousePosition;
        }

        private void AddTime(float _addTime)
        {
            time += _addTime;
            if (time > 1)
            {
                EventSystem.RaiseEvent(EventType.RESET_TIME);
                time = 0;
            }
            EventSystem<float>.RaiseEvent(EventType.TIME, time);
            MoveTimelineTimIndicator(time);
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
            if (!UIManager.isInteracting)
            {
                if (PlaceTimelineIndicator())
                    return;
            }
            
            if (ScaleTimeline())
                return;

            if (shouldTimelinePause)
            {
                timeIncrease = Time.timeSinceLevelLoad;
                return;
            }
            
            timeIncrease = (Time.timeSinceLevelLoad - timeIncrease) / Mathf.Pow(speedSliderTimeline.value, 8f);
            time += timeIncrease;
            
            MoveTimelineTimIndicator(time);
            EventSystem<float>.RaiseEvent(EventType.TIME, time);
            
            if (time > 1)
            {
                EventSystem.RaiseEvent(EventType.RESET_TIME);
                time = 0;
            }
            
            timeIncrease = Time.timeSinceLevelLoad;
        }
        private bool PlaceTimelineIndicator()
        {
            if (Input.GetMouseButtonDown(1) && isMouseInsideTimeline)
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
            float mousePosX = Mathf.Clamp(Input.mousePosition.x, Corners[0].x, Corners[2].x);
            timelineScrollRect.position = new Vector3(mousePosX, position.y, position.z);
            time = Input.mousePosition.x.Remap(Corners[0].x, Corners[2].x, 0, 1);
            timeIncrease = Time.timeSinceLevelLoad;
            EventSystem<float>.RaiseEvent(EventType.TIME, time);
            return true;
        }
        private void MoveTimelineTimIndicator(float _time)
        {
            float xPos = _time.Remap(0, 1, Corners[0].x, Corners[2].x);
            var position = timelineScrollRect.position;
            position = new Vector3(xPos, position.y, position.z);
            timelineScrollRect.position = position;
        }

        private bool ScaleTimeline()
        {
            if (isMouseInsideTimeline && Input.mouseScrollDelta.y != 0 && Input.GetKey(KeyCode.LeftShift))
            {
                List<Vector2> timelineValues = clipsOrdered.SelectMany(_list => _list.Select(_clip => _clip.ClipTime)).ToList();
                foreach (var timelineBar in timelineBarObjects)
                {
                    timelineBar.sizeDelta += new Vector2(Input.mouseScrollDelta.y * timelineScaleSensitivity, 0);
                    if (timelineBar.sizeDelta.x < minTimelineSize)
                    {
                        timelineBar.sizeDelta = new Vector2(minTimelineSize, timelineBar.sizeDelta.y);
                    }
                    if (timelineBar.sizeDelta.x > maxTimelineSize)
                    {
                        timelineBar.sizeDelta = new Vector2(maxTimelineSize, timelineBar.sizeDelta.y);
                    }
                }

                int index = 0;
                for (int i = 0; i < clipsOrdered.Count; i++)
                {
                    for (int j = 0; j < clipsOrdered[i].Count; j++)
                    {
                        clipsOrdered[i][j].ClipTime = timelineValues[index + j];
                    }
                    index += clipsOrdered[i].Count;
                }
                return true;
            }
            return false;
        }

        #region ClipInput
        private void TimelineClipsInput()
        {
            if (!UIManager.isInteracting || isInteracting)
            {
                //If you are already interacting with a timelineclip check that one first
                if ((Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)) && selectedClips.Count > 0 && !Input.GetKey(KeyCode.LeftShift))
                {
                    ChangeSelectedTimelineClips();
                }
            
                //Otherwise check if you are interacting with any other timeline clips
                if (Input.GetMouseButton(0) && isMouseInsideTimeline)
                {
                    if (ClickedTimelineClip())
                        return;
                }
            }
            
            if (Input.GetMouseButtonUp(0))
            {
                lastSelectedClip = null;
            }

            if (lastSelectedClip != null)
            {
                if (!lastSelectedClip.IsMouseOver())
                {
                    lastSelectedClip = null;
                }
            }
            
            //Once you are done making changes 
            if (Input.GetMouseButtonUp(0) && selectedClips.Count > 0)
            {
                StoppedMakingChanges();
            }

            if (selectedClips.Count > 0)
            {
                //once you have clicked somewhere else unselect everything
                if (ClickedAway())
                    return;

                //If you press backspace delete all selected timelineclips
                if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete))
                {
                    DeleteAllSelectedClips();
                    return;
                }
            }
            
            if (Input.GetMouseButton(0) && selectedClips.Count > 0)
            {
                isInteracting = true;
                EventSystem<bool>.RaiseEvent(EventType.IS_INTERACTING, true);
            }
        }
        private void ChangeSelectedTimelineClips()
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
                float leftMostPos = Mathf.Infinity;
                float rightMostPos = 0;
                if (lastMouseAction is MouseAction.ResizeClipRight or MouseAction.ResizeClipLeft)
                {
                    Vector3[] clipCorners = new Vector3[4];
                    foreach (var clip in selectedClips)
                    {
                        clip.rect.GetWorldCorners(clipCorners);
                        if (clipCorners[0].x < leftMostPos)
                            leftMostPos = clipCorners[0].x;
                        if (clipCorners[2].x > rightMostPos)
                            rightMostPos = clipCorners[2].x;
                    }
                }
                
                if (firstTimeSelected)
                {
                    foreach (TimelineClip clip in selectedClips)
                    {
                        clip.previousBar = clip.currentBar;
                        clip.clipTimeOld = clip.ClipTime;

                        clip.barOffset = clip.currentBar - selectedClip.currentBar;
                        clip.SetupMovement(lastMouseAction, leftMostPos, rightMostPos);
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

                    if (Math.Abs(selectedClips[i].clipTimeOld.x - selectedClips[i].ClipTime.x) > 0.001 ||
                        Math.Abs(selectedClips[i].clipTimeOld.y - selectedClips[i].ClipTime.y) > 0.001)
                    {
                        clip.brushStrokeID.lastTime = clip.ClipTime.x;
                        clip.brushStrokeID.currentTime = clip.ClipTime.y;
                        brushStrokeIDs.Add(clip.brushStrokeID);
                    }
                }
                if (brushStrokeIDs.Count > 0)
                {
                    EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.REDRAW_STROKES, brushStrokeIDs);
                }
            }
        }
        private bool ClickedTimelineClip()
        {
            for (int i = 0; i < clipsOrdered.Count; i++)
            {
                for (int j = 0; j < clipsOrdered[i].Count; j++)
                {
                    var clip = clipsOrdered[i][j];
                    if (!clip.IsMouseOver())
                    {
                        continue;
                    }

                    if (!selectedClips.Contains(clip))
                    {
                        if (Input.GetKey(KeyCode.LeftShift) && (clip != lastSelectedClip))
                        {
                            firstTimeSelected = true;
                            lastSelectedClip = clip;
                            selectedClips.Add(clip);
                            clip.rawImage.color = selectedColor;
                            EventSystem<BrushStrokeID>.RaiseEvent(EventType.ADD_SELECT, clip.brushStrokeID);
                            return true;
                        }
                        if (Input.GetMouseButtonDown(0) && clip != lastSelectedClip)
                        {
                            foreach (var _clip in selectedClips)
                            {
                                _clip.rawImage.color = notSelectedColor;
                            }

                            firstTimeSelected = true;
                            EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
                            
                            clip.rawImage.color = selectedColor;
                            selectedClips.Add(clip);
                            EventSystem<BrushStrokeID>.RaiseEvent(EventType.ADD_SELECT, clip.brushStrokeID);
                            return true;
                        }
                    }
                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButton(0) && (clip != lastSelectedClip && clip.previousBar == clip.currentBar 
                            && Math.Abs(clip.clipTimeOld.x - clip.ClipTime.x) < 0.001f && Math.Abs(clip.clipTimeOld.y - clip.ClipTime.y) < 0.001f))
                    {
                        clip.mouseAction = MouseAction.Nothing;
                        lastSelectedClip = clip;
                        selectedClips.Remove(clip);
                        clip.rawImage.color = notSelectedColor;
                        firstTimeSelected = true;
                        EventSystem<BrushStrokeID>.RaiseEvent(EventType.REMOVE_SELECT, clip.brushStrokeID);
                        return true;
                    }
                }
            }
            
            return false;
        }
        private void StoppedMakingChanges()
        {
            lastMouseAction = MouseAction.Nothing;
            firstTimeSelected = true;
            
            if (selectedClips.Count > 0)
            {
                List<RedrawCommand> redraws = new List<RedrawCommand>();
                for (int i = 0; i < selectedClips.Count; i++)
                {
                    var clip = selectedClips[i];
                    if (Math.Abs(clip.clipTimeOld.x - clip.ClipTime.x) < 0.001f &&
                        Math.Abs(clip.clipTimeOld.y - clip.ClipTime.y) < 0.001f &&
                        clip.previousBar == clip.currentBar)
                    {
                        continue;
                    }
                    
                    clip.mouseAction = MouseAction.Nothing;
                    CheckClipCollisions(clip);

                    RedrawCommand clipCommand = new RedrawCommand(clip);
                    redraws.Add(clipCommand);
                }

                if (redraws.Count > 0)
                {
                    ICommand redrawCommand = new RedrawMultipleCommand(redraws);
                    EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, redrawCommand);
                }
            }
        }
        private void DeleteAllSelectedClips()
        {
            EventSystem<List<BrushStrokeID>>.RaiseEvent(EventType.REMOVE_STROKE, selectedClips.Select(_clip => _clip.brushStrokeID).ToList());

            List<TimelineClip> timelineClips = new List<TimelineClip>(selectedClips);
            ICommand deleteMultiple = new DeleteClipMultipleCommand(timelineClips);
            EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, deleteMultiple);

            RemoveClip(selectedClips);
            selectedClips.Clear();
            EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
        }
        private bool ClickedAway()
        {
            if (Input.GetMouseButtonDown(0) && selectedClips.Count > 0 && !UIManager.isInteracting && !(Input.GetKey(
                    KeyCode.LeftShift) && isMouseInsideTimeline))
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
                {
                    return true;
                }

                EventSystem.RaiseEvent(EventType.CLEAR_SELECT);
                return true;
            }
            return false;
        }

        private void CheckClipCollisions(TimelineClip _clip)
        {
            int currentBar = _clip.currentBar;
        
            //Check if there is a collision on its current bar
            if (!IsClipCollidingInBar(_clip, currentBar))
            {
                foreach (var timelineList in clipsOrdered)
                {
                    timelineList.Remove(_clip);
                }
                clipsOrdered[currentBar].Add(_clip);
                _clip.previousBar = currentBar;
                _clip.currentBar = currentBar;

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
                        foreach (var timelineList in clipsOrdered)
                        {
                            timelineList.Remove(_clip);
                        }
                        _clip.SetBar(lowerBar);
                        clipsOrdered[lowerBar].Add(_clip);
                        _clip.previousBar = lowerBar;

                        return;
                    }
                }
        
                if (upperBar >= 0)
                {
                    if (!IsClipCollidingInBar(_clip, upperBar))
                    {
                        foreach (var timelineList in clipsOrdered)
                        {
                            timelineList.Remove(_clip);
                        }
                        _clip.SetBar(upperBar);
                        clipsOrdered[upperBar].Add(_clip);
                        _clip.previousBar = upperBar;
                        Debug.Log($"{clipsOrdered[0].Count} {clipsOrdered[1].Count} {clipsOrdered[2].Count} {clipsOrdered[3].Count} " +
                                  $"{clipsOrdered[4].Count} {clipsOrdered[5].Count} {clipsOrdered[6].Count}");
                        return;
                    }
                }
            }
            
            //There is no space anywhere create a new one timeline bar
            amountTimelineBars++;
            RectTransform timelineBar = Instantiate(timelineBarObjects[0], timelineRect);
            timelineBar.transform.SetAsFirstSibling();
            timelineBarObjects.Add(timelineBar);
            clipsOrdered.Add(new List<TimelineClip>());
            
            foreach (var timelineList in clipsOrdered)
            {
                timelineList.Remove(_clip);
            }
            int lowestBar = clipsOrdered.Count - 1;
            _clip.SetBar(lowestBar);
            clipsOrdered[lowestBar].Add(_clip);
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
            if (_clip.ClipTime.x > _clip2.ClipTime.x && _clip.ClipTime.x < _clip2.ClipTime.y ||
                _clip.ClipTime.y > _clip2.ClipTime.x && _clip.ClipTime.y < _clip2.ClipTime.y
                )
            {
                return true;
            }
            if (_clip2.ClipTime.x > _clip.ClipTime.x && _clip2.ClipTime.x < _clip.ClipTime.y ||
                _clip2.ClipTime.y > _clip.ClipTime.x && _clip2.ClipTime.y < _clip.ClipTime.y
               )
            {
                return true;
            }

            if (Math.Abs(_clip.ClipTime.x - _clip2.ClipTime.x) < 0.0001f && Math.Abs(_clip.ClipTime.y - _clip2.ClipTime.y) < 0.0001f)
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
                if (selectedClips[0].mouseAction == MouseAction.Nothing)
                {
                    float leftSide = Mathf.Clamp01(float.Parse(clipLeftInput.text));
                    float rightSide = Mathf.Clamp01(float.Parse(clipRightInput.text));
                    if (leftSide < rightSide)
                    {
                        selectedClips[0].ClipTime = new Vector2(leftSide, rightSide);
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
            RectTransform rect = Instantiate(timelineClipObject, timelineBarObject).GetComponent<RectTransform>();
            RawImage clipImage = rect.GetComponent<RawImage>();
            clipImage.color = notSelectedColor;
            TimelineClip timelineClip = new TimelineClip(_brushStrokeID, rect, timelineBarObject, timelineRect, clipImage)
            {
                ClipTime = new Vector2(_brushStrokeID.lastTime, _brushStrokeID.currentTime),
                clipTimeOld = new Vector2(_brushStrokeID.lastTime, _brushStrokeID.currentTime),
            };
            clipsOrdered[0].Add(timelineClip);
            CheckClipCollisions(timelineClip);
            
            ICommand draw = new DrawCommand(timelineClip);
            EventSystem<ICommand>.RaiseEvent(EventType.ADD_COMMAND, draw);
        }
        private void AddNewBrushClip(TimelineClip _timelineClip)
        {
            int currentBar = _timelineClip.currentBar;
            _timelineClip.previousBar = currentBar;
            RectTransform rect = Instantiate(timelineClipObject, timelineBarObject).GetComponent<RectTransform>();
            RawImage clipImage = rect.GetComponent<RawImage>();
            _timelineClip.rawImage = clipImage;
            _timelineClip.rect = rect;
            _timelineClip.currentBar = 0;
            _timelineClip.ClipTime = new Vector2(_timelineClip.brushStrokeID.lastTime, _timelineClip.brushStrokeID.currentTime);
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
        private void SelectClip(BrushStrokeID _brushStrokeIDs)
        {
            List<TimelineClip> clips = clipsOrdered.SelectMany(_timeBar => _timeBar.Where(_clip => _clip.brushStrokeID == _brushStrokeIDs)).ToList();
            foreach (var clip in clips)
            {
                clip.rawImage.color = selectedColor;
                selectedClips.Add(clip);
            }
        }

        private void ClearSelected()
        {
            foreach (var clip in selectedClips)
            {
                clip.rawImage.color = notSelectedColor;
            }
            selectedClips.Clear();
        }
        private void RemoveSelectedClip(BrushStrokeID _brushStrokeID)
        {
            List<TimelineClip> clips = clipsOrdered.SelectMany(_timeBar => _timeBar.Where(_clip => _clip.brushStrokeID == _brushStrokeID)).ToList();
            foreach (var clip in clips)
            {
                clip.rawImage.color = notSelectedColor;
                selectedClips.Remove(clip);
            }
        }
        private void RemoveClip(List<TimelineClip> _timelineClips)
        {
            foreach (var clip in _timelineClips)
            {
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
