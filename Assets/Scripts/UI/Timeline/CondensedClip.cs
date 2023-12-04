using System.Collections.Generic;
using Drawing;

namespace UI
{
    public struct CondensedClip
    {
        public float startTime;
        public float endTime;
        public int currentBar;
        public List<BrushStrokeID> brushStrokeIDs;
        public List<CondensedClip> childClips;

        public CondensedClip(float _startTime, float _endTime, int _currentBar, List<BrushStrokeID> _brushStrokeIDs, List<CondensedClip> _childClips)
        {
            startTime = _startTime;
            endTime = _endTime;
            currentBar = _currentBar;
            brushStrokeIDs = _brushStrokeIDs;
            childClips = _childClips;
        }
    }
}
