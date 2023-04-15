using System.Collections.Generic;
using Managers;
using UnityEngine;

namespace Drawing
{
    public class DrawStamp
    {
        private Dictionary<string, BrushStrokeID> stamps;

        public DrawStamp()
        {
            stamps = new Dictionary<string, BrushStrokeID>();
        }

        public BrushStrokeID Circle(Vector2 _middlePos, float _circleRadius, int _indexWhenDrawn, 
            float _brushSize = 50, float _startTime = 0, float _endTime = 0)
        {
            int amountCircleLines = 360;
            List<BrushStroke> brushStrokes = new List<BrushStroke>();
            
            float collisionBoxX = _middlePos.x - _circleRadius - _brushSize;
            float collisionBoxY = _middlePos.y - _circleRadius - _brushSize;
            float collisionBoxZ = _middlePos.x + _circleRadius + _brushSize;
            float collisionBoxW = _middlePos.y + _circleRadius + _brushSize;
            Vector4 collisionBox = new Vector4(collisionBoxX, collisionBoxY, collisionBoxZ, collisionBoxW);
            
            Vector2 lastPos = Vector2.up * _circleRadius + _middlePos;
            for (int i = 0; i < amountCircleLines; i++)
            {
                float angleRad = Mathf.Deg2Rad * i;
                float cosTheta = Mathf.Cos(angleRad);
                float sinTheta = Mathf.Sin(angleRad);
                float rotatedX = -1 * sinTheta;
                float rotatedY = 1 * cosTheta;
                Vector2 rotatedVector = new Vector2(rotatedX, rotatedY) * _circleRadius + _middlePos;
                Debug.Log(rotatedVector);

                BrushStroke brushStroke = new BrushStroke(lastPos, rotatedVector, 50f, 0, 0);
                brushStrokes.Add(brushStroke);

                lastPos = rotatedVector;
            }

            return new BrushStrokeID(brushStrokes, PaintType.PaintUnderEverything, _startTime, 
                _endTime, collisionBox, _indexWhenDrawn, _middlePos);
        }

        public BrushStrokeID GetStamp(string key)
        {
            return stamps[key];
        }
        
        
    }
}
