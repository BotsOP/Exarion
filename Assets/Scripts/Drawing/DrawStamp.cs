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

        // public BrushStrokeID Circle(Vector2 _middlePos, float _circleRadius, int _indexWhenDrawn, 
        //     float _brushSize = 50, float _startTime = 0, float _endTime = 0)
        // {
        //     int amountCircleLines = 360;
        //     List<BrushStroke> brushStrokes = new List<BrushStroke>();
        //     
        //     float collisionBoxX = _middlePos.x - _circleRadius - _brushSize;
        //     float collisionBoxY = _middlePos.y - _circleRadius - _brushSize;
        //     float collisionBoxZ = _middlePos.x + _circleRadius + _brushSize;
        //     float collisionBoxW = _middlePos.y + _circleRadius + _brushSize;
        //     Vector4 collisionBox = new Vector4(collisionBoxX, collisionBoxY, collisionBoxZ, collisionBoxW);
        //     
        //     Vector2 lastPos = Vector2.up * _circleRadius + _middlePos;
        //     for (int i = 0; i < amountCircleLines; i++)
        //     {
        //         float angleRad = Mathf.Deg2Rad * i;
        //         float cosTheta = Mathf.Cos(angleRad);
        //         float sinTheta = Mathf.Sin(angleRad);
        //         float rotatedX = -1 * sinTheta;
        //         float rotatedY = 1 * cosTheta;
        //         Vector2 rotatedVector = new Vector2(rotatedX, rotatedY) * _circleRadius + _middlePos;
        //
        //         BrushStroke brushStroke = new BrushStroke(lastPos, rotatedVector, _brushSize, 0, 0);
        //         brushStrokes.Add(brushStroke);
        //
        //         lastPos = rotatedVector;
        //     }
        //
        //     {
        //         float angleRad = Mathf.Deg2Rad * 0;
        //         float cosTheta = Mathf.Cos(angleRad);
        //         float sinTheta = Mathf.Sin(angleRad);
        //         float rotatedX = -1 * sinTheta;
        //         float rotatedY = 1 * cosTheta;
        //         Vector2 rotatedVector = new Vector2(rotatedX, rotatedY) * _circleRadius + _middlePos;
        //         Debug.Log(rotatedVector);
        //
        //         BrushStroke brushStroke = new BrushStroke(lastPos, rotatedVector, _brushSize, 0, 0);
        //         brushStrokes.Add(brushStroke);
        //     }
        //
        //     return new BrushStrokeID(brushStrokes, PaintType.PaintUnderEverything, _startTime, 
        //         _endTime, collisionBox, _indexWhenDrawn, _middlePos);
        // }
        //
        // public BrushStrokeID Square(Vector2 _middlePos, float _squareWidth, int _indexWhenDrawn, 
        //     float _brushSize = 50, float _startTime = 0, float _endTime = 0)
        // {
        //     _squareWidth /= 2;
        //     float collisionBoxX = _middlePos.x - _squareWidth - _brushSize;
        //     float collisionBoxY = _middlePos.y - _squareWidth - _brushSize;
        //     float collisionBoxZ = _middlePos.x + _squareWidth + _brushSize;
        //     float collisionBoxW = _middlePos.y + _squareWidth + _brushSize;
        //     Vector4 collisionBox = new Vector4(collisionBoxX, collisionBoxY, collisionBoxZ, collisionBoxW);
        //     
        //     List<BrushStroke> brushStrokes = new List<BrushStroke>();
        //     Vector2 topLeft = _middlePos + new Vector2(-_squareWidth, _squareWidth);
        //     Vector2 bottomLeft = _middlePos + new Vector2(-_squareWidth, -_squareWidth);
        //     Vector2 bottomRight = _middlePos + new Vector2(_squareWidth, -_squareWidth);
        //     Vector2 topRight = _middlePos + new Vector2(_squareWidth, _squareWidth);
        //     
        //     BrushStroke topLeftStroke = new BrushStroke(topLeft, topLeft, _brushSize, 0, 0);
        //     BrushStroke bottomLeftStroke = new BrushStroke(topLeft, bottomLeft, _brushSize, 0, 0);
        //     BrushStroke bottomRightStroke = new BrushStroke(bottomLeft, bottomRight, _brushSize, 0, 0);
        //     BrushStroke topRightStroke = new BrushStroke(bottomRight, topRight, _brushSize, 0, 0);
        //     BrushStroke topLeftStroke2 = new BrushStroke(topRight, topLeft, _brushSize, 0, 0);
        //     brushStrokes.Add(topLeftStroke);
        //     brushStrokes.Add(bottomLeftStroke);
        //     brushStrokes.Add(bottomRightStroke);
        //     brushStrokes.Add(topRightStroke);
        //     brushStrokes.Add(topLeftStroke2);
        //
        //     return new BrushStrokeID(brushStrokes, PaintType.PaintUnderEverything, _startTime, 
        //         _endTime, collisionBox, _indexWhenDrawn, _middlePos);
        // }
        //
        // public BrushStrokeID Hexagon(Vector2 _middlePos, float _squareWidth, int _indexWhenDrawn, 
        //     float _brushSize = 50, float _startTime = 0, float _endTime = 0)
        // {
        //     _squareWidth /= 2;
        //     float collisionBoxX = _middlePos.x - _squareWidth - _brushSize;
        //     float collisionBoxY = _middlePos.y - _squareWidth - _brushSize;
        //     float collisionBoxZ = _middlePos.x + _squareWidth + _brushSize;
        //     float collisionBoxW = _middlePos.y + _squareWidth + _brushSize;
        //     Vector4 collisionBox = new Vector4(collisionBoxX, collisionBoxY, collisionBoxZ, collisionBoxW);
        //     
        //     List<BrushStroke> brushStrokes = new List<BrushStroke>();
        //     Vector2 topLeft = _middlePos + new Vector2(-_squareWidth / 2, _squareWidth * 0.875f);
        //     Vector2 bottomLeft = _middlePos + new Vector2(-_squareWidth / 2, -_squareWidth * 0.875f);
        //     Vector2 bottomRight = _middlePos + new Vector2(_squareWidth / 2, -_squareWidth * 0.875f);
        //     Vector2 topRight = _middlePos + new Vector2(_squareWidth / 2, _squareWidth * 0.875f);
        //     Vector2 left = _middlePos + new Vector2(-_squareWidth, 0);
        //     Vector2 right = _middlePos + new Vector2(_squareWidth, 0);
        //     
        //     BrushStroke topLeftStroke = new BrushStroke(topLeft, topLeft, _brushSize, 0, 0);
        //     BrushStroke leftStroke = new BrushStroke(topLeft, left, _brushSize, 0, 0);
        //     BrushStroke bottomLeftStroke = new BrushStroke(left, bottomLeft, _brushSize, 0, 0);
        //     BrushStroke bottomRightStroke = new BrushStroke(bottomLeft, bottomRight, _brushSize, 0, 0);
        //     BrushStroke rightStroke = new BrushStroke(bottomRight, right, _brushSize, 0, 0);
        //     BrushStroke topRightStroke = new BrushStroke(right, topRight, _brushSize, 0, 0);
        //     BrushStroke topRightStroke2 = new BrushStroke(topRight, topLeft, _brushSize, 0, 0);
        //     
        //     brushStrokes.Add(topLeftStroke);
        //     brushStrokes.Add(leftStroke);
        //     brushStrokes.Add(bottomLeftStroke);
        //     brushStrokes.Add(bottomRightStroke);
        //     brushStrokes.Add(rightStroke);
        //     brushStrokes.Add(topRightStroke);
        //     brushStrokes.Add(topRightStroke2);
        //
        //     return new BrushStrokeID(brushStrokes, PaintType.PaintUnderEverything, _startTime, 
        //         _endTime, collisionBox, _indexWhenDrawn, _middlePos);
        // }
        //
        // public BrushStrokeID Polygon(Vector2 _middlePos, int _amountAngles, float _size, int _indexWhenDrawn, 
        //     float _brushSize = 50, float _startTime = 0, float _endTime = 0)
        // {
        //     _size /= 2;
        //     float collisionBoxX = _middlePos.x - _size - _brushSize;
        //     float collisionBoxY = _middlePos.y - _size - _brushSize;
        //     float collisionBoxZ = _middlePos.x + _size + _brushSize;
        //     float collisionBoxW = _middlePos.y + _size + _brushSize;
        //     Vector4 collisionBox = new Vector4(collisionBoxX, collisionBoxY, collisionBoxZ, collisionBoxW);
        //     
        //     List<BrushStroke> brushStrokes = new List<BrushStroke>();
        //     
        //     float angleStep = 2 * Mathf.PI / _amountAngles;
        //     Vector2 lastPos = new Vector2(Mathf.Cos(0), Mathf.Sin(0)) * _size + _middlePos;
        //     for (int i = 0; i < _amountAngles; i++)
        //     {
        //         float angle = i * angleStep;
        //         Vector2 localPos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _size + _middlePos;
        //         BrushStroke brushStroke = new BrushStroke(lastPos, localPos, _brushSize, 0, 0);
        //         brushStrokes.Add(brushStroke);
        //         lastPos = localPos;
        //     }
        //
        //     {
        //         float angle = 0 * angleStep;
        //         Vector2 localPos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _size + _middlePos;
        //         BrushStroke brushStroke = new BrushStroke(lastPos, localPos, _brushSize, 0, 0);
        //         brushStrokes.Add(brushStroke);
        //     }
        //
        //     return new BrushStrokeID(brushStrokes, PaintType.PaintUnderEverything, _startTime, 
        //                              _endTime, collisionBox, _indexWhenDrawn, _middlePos);
        // }

        // public BrushStrokeID GetStamp(string key, Vector2 _middlePos, float _squareWidth, int _indexWhenDrawn, 
        //     float _brushSize = 50, float _startTime = 0, float _endTime = 0)
        // {
        //     switch (key)
        //     {
        //         case "circle":
        //             return Circle(_middlePos, _squareWidth, _indexWhenDrawn, _brushSize, _startTime, _endTime);
        //         case "square":
        //             return Square(_middlePos, _squareWidth, _indexWhenDrawn, _brushSize, _startTime, _endTime);
        //         // case "hexagon":
        //         //     return Hexagon(_middlePos, _squareWidth, _indexWhenDrawn, _brushSize, _startTime, _endTime);
        //         case "hexagon":
        //             return Polygon(_middlePos, 8, _squareWidth, _indexWhenDrawn, _brushSize, _startTime, _endTime);
        //         default:
        //             Debug.Log($"failed to find string");
        //             return stamps[key];
        //     }
        // }
        //
        // public BrushStrokeID GetPolygon(int _sides, Vector2 _middlePos, float _squareWidth, int _indexWhenDrawn, 
        //     float _brushSize = 50, float _startTime = 0, float _endTime = 0)
        // {
        //     return Polygon(_middlePos, _sides, _squareWidth, _indexWhenDrawn, _brushSize, _startTime, _endTime);
        // }

    }
}
