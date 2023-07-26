using System.Collections.Generic;
using System.Linq;
using Drawing;
using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TimelineClipGroup : TimelineClip
    {
        private List<TimelineClip> clips;
        private List<BrushStrokeID> allBrushStrokes;
        private List<Vector2> oldBrushStrokeTimes;
        public TimelineClipGroup(List<TimelineClip> _clips, RectTransform _rect, RectTransform _timelineBarRect, RectTransform _timelineAreaRect, RawImage _rawImage) : base(_rect, _timelineBarRect, _timelineAreaRect, _rawImage)
        {
            clips = _clips;
            allBrushStrokes = clips.SelectMany(_clip => _clip.GetBrushStrokeIDs()).ToList();

            Vector2 brushStrokeTime = GetTime();
            ClipTime = brushStrokeTime;
            clipTimeOld = brushStrokeTime;
        }

        public override void SetupMovement(MouseAction _mouseAction, float _leftMostPos, float _rightMostPos)
        {
            base.SetupMovement(_mouseAction, _leftMostPos, _rightMostPos);
            oldBrushStrokeTimes = allBrushStrokes.Select(_brushStroke => new Vector2(_brushStroke.lastTime, _brushStroke.currentTime)).ToList();
        }

        public override List<BrushStrokeID> GetBrushStrokeIDs()
        {
            return allBrushStrokes;
        }
        
        public override List<TimelineClip> GetClips()
        {
            return clips;
        }

        public override void SetTime(Vector2 _time)
        {
            for (int i = 0; i < allBrushStrokes.Count; i++)
            {
                var brushStrokeID = allBrushStrokes[i];
                float lastTime = oldBrushStrokeTimes[i].x.Remap(clipTimeOld.x, clipTimeOld.y, _time.x, _time.y);
                float currentTime = oldBrushStrokeTimes[i].y.Remap(clipTimeOld.x, clipTimeOld.y, _time.x, _time.y);

                Debug.Log($"{lastTime}  {currentTime}      {clipTimeOld.x} {clipTimeOld.y}     {brushStrokeID.lastTime} {brushStrokeID.currentTime}");

                brushStrokeID.lastTime = lastTime;
                brushStrokeID.currentTime = currentTime;
            }
        }

        public override sealed Vector2 GetTime()
        {
            float smallest = float.MaxValue;
            float biggest = 0;
            
            foreach (var brushStrokeID in allBrushStrokes)
            {
                if (brushStrokeID.lastTime < smallest)
                {
                    smallest = brushStrokeID.lastTime;
                }
                if (brushStrokeID.currentTime > biggest)
                {
                    biggest = brushStrokeID.currentTime;
                }
            }
            
            return new Vector2(smallest, biggest);
        }

        public override bool HoldingBrushStroke(BrushStrokeID _brushStrokeID)
        {
            foreach (var brushStrokeID in allBrushStrokes)
            {
                if (brushStrokeID == _brushStrokeID)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
