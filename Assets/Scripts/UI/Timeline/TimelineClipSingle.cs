using System.Collections.Generic;
using Drawing;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TimelineClipSingle : TimelineClip
    {
        private BrushStrokeID brushStrokeID;
        public TimelineClipSingle(BrushStrokeID _brushStrokeID, RectTransform _rect, RectTransform _timelineBarRect, RectTransform _timelineAreaRect, RawImage _rawImage) : base(_rect, _timelineBarRect, _timelineAreaRect, _rawImage)
        {
            brushStrokeID = _brushStrokeID;
            
            Vector2 brushStrokeTime = GetTime();
            ClipTime = brushStrokeTime;
            clipTimeOld = brushStrokeTime;
        }
        
        public TimelineClipSingle(BrushStrokeID _brushStrokeID, RectTransform _timelineBarRect, RectTransform _timelineAreaRect) : base(_timelineBarRect, _timelineAreaRect)
        {
            brushStrokeID = _brushStrokeID;
        }

        public override Color GetNotSelectedColor()
        {
            return Timeline.notSelectedSingleColors;
        }

        public override ClipType GetClipType()
        {
            return ClipType.Single;
        }

        public override List<BrushStrokeID> GetBrushStrokeIDs()
        {
            return new List<BrushStrokeID> { brushStrokeID };
        }
        
        public override List<TimelineClip> GetClips()
        {
            return new List<TimelineClip> { this };
        }

        public override void SetTime(Vector2 _time)
        {
            brushStrokeID.startTime = _time.x;
            brushStrokeID.endTime = _time.y;
        }

        public override sealed Vector2 GetTime()
        {
            return new Vector2(brushStrokeID.startTime, brushStrokeID.endTime);
        }

        public override bool HoldingBrushStroke(BrushStrokeID _brushStrokeID)
        {
            return brushStrokeID == _brushStrokeID;
        }
    }
}
