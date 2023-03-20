using UnityEngine;

namespace Drawing
{
    public class BrushStrokeID
    {
        public int startID;
        public int endID;
        public float collisionBoxX;
        public float collisionBoxY;
        public float collisionBoxZ;
        public float collisionBoxW;
        public PaintType paintType;
        public float lastTime;
        public float currentTime;
        public int indexWhenDrawn;

        public BrushStrokeID(int _startID, int _endID, PaintType _paintType, float _lastTime, float _currentTime, Vector4 _collisionBox, int _indexWhenDrawn)
        {
            startID = _startID;
            endID = _endID;
            paintType = _paintType;
            lastTime = _lastTime;
            currentTime = _currentTime;
            collisionBoxX = _collisionBox.x;
            collisionBoxY = _collisionBox.y;
            collisionBoxZ = _collisionBox.z;
            collisionBoxW = _collisionBox.w;
            indexWhenDrawn = _indexWhenDrawn;
        }

        public Vector4 GetCollisionBox()
        {
            return new Vector4(collisionBoxX, collisionBoxY, collisionBoxZ, collisionBoxW);
        }
    }
}
