using System.Collections.Generic;
using Drawing;

namespace UI
{
    public struct CondensedClip
    {
        public float lastTime;
        public float currentTime;
        public int currentBar;
        public List<BrushStrokeID> brushStrokeIDs;
        public List<CondensedClip> childClips;

        public CondensedClip(float _lastTime, float _currentTime, int _currentBar, List<BrushStrokeID> _brushStrokeIDs, List<CondensedClip> _childClips)
        {
            lastTime = _lastTime;
            currentTime = _currentTime;
            currentBar = _currentBar;
            brushStrokeIDs = _brushStrokeIDs;
            childClips = _childClips;
        }
    }
}
