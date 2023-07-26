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
        }

        public override List<BrushStrokeID> GetBrushStrokeIDs()
        {
            return new List<BrushStrokeID> { brushStrokeID };
        }

        public override void SetTime(Vector2 _time)
        {
            brushStrokeID.lastTime = _time.x;
            brushStrokeID.currentTime = _time.y;
        }

        public override Vector2 GetTime()
        {
            return new Vector2(brushStrokeID.lastTime, brushStrokeID.currentTime);
        }

        public override bool HoldingBrushStroke(BrushStrokeID _brushStrokeID)
        {
            return brushStrokeID == _brushStrokeID;
        }
    }
}
