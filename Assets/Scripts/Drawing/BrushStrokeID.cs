using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Drawing
{
    public class BrushStrokeID
    {
        public List<BrushStroke> brushStrokes;
        public float collisionBoxX;
        public float collisionBoxY;
        public float collisionBoxZ;
        public float collisionBoxW;
        public PaintType paintType;
        public float startTime;
        public float endTime;
        public int indexWhenDrawn;
        public float avgPosX;
        public float avgPosY;
        public float angle;
        public float scale;
        
        [JsonConstructor]
        public BrushStrokeID(List<BrushStroke> _brushStrokes, PaintType _paintType, float _startTime, float _endTime, Vector4 _collisionBox, int _indexWhenDrawn, Vector2 _avgPos, float _angle = 0, float _scale = 1)
        {
            brushStrokes = _brushStrokes;
            paintType = _paintType;
            startTime = _startTime;
            endTime = _endTime;
            collisionBoxX = _collisionBox.x;
            collisionBoxY = _collisionBox.y;
            collisionBoxZ = _collisionBox.z;
            collisionBoxW = _collisionBox.w;
            indexWhenDrawn = _indexWhenDrawn;
            avgPosX = _avgPos.x;
            avgPosY = _avgPos.y;
            angle = _angle;
            scale = _scale;
        }
        
        public BrushStrokeID(BrushStrokeID _brushStrokeID, int _indexWhenDrawn)
        {
            brushStrokes = new List<BrushStroke>(_brushStrokeID.brushStrokes);
            paintType = _brushStrokeID.paintType;
            startTime = _brushStrokeID.startTime;
            endTime = _brushStrokeID.endTime;
            collisionBoxX = _brushStrokeID.collisionBoxX;
            collisionBoxY = _brushStrokeID.collisionBoxY;
            collisionBoxZ = _brushStrokeID.collisionBoxZ;
            collisionBoxW = _brushStrokeID.collisionBoxW;
            avgPosX = _brushStrokeID.avgPosX;
            avgPosY = _brushStrokeID.avgPosY;
            angle = _brushStrokeID.angle;
            scale = _brushStrokeID.scale;
            indexWhenDrawn = _indexWhenDrawn;
        }

        public Vector4 GetCollisionBox()
        {
            return new Vector4(collisionBoxX, collisionBoxY, collisionBoxZ, collisionBoxW);
        }

        public Vector2 GetAvgPos()
        {
            return new Vector2(avgPosX, avgPosY);
        }

        public void RecalculateAvgPos()
        {
            Vector2 avgPos = Vector2.zero;
            for (var i = 1; i < brushStrokes.Count; i++)
            {
                var brushStroke = brushStrokes[i];
                avgPos += brushStroke.GetCurrentPos();
            }

            avgPos /= brushStrokes.Count - 1;
            avgPosX = avgPos.x;
            avgPosY = avgPos.y;
        }

        public void RecalculateCollisionBox()
        {
            Vector4 collisionBox = new Vector4(Mathf.Infinity, Mathf.Infinity, 0, 0);
            
            foreach (var brushStroke in brushStrokes)
            {
                if (collisionBox.x > brushStroke.currentPosX - brushStroke.brushSize) { collisionBox.x = brushStroke.currentPosX - brushStroke.brushSize; }
                if (collisionBox.y > brushStroke.currentPosY - brushStroke.brushSize) { collisionBox.y = brushStroke.currentPosY - brushStroke.brushSize; }
                if (collisionBox.z < brushStroke.currentPosX + brushStroke.brushSize) { collisionBox.z = brushStroke.currentPosX + brushStroke.brushSize; }
                if (collisionBox.w < brushStroke.currentPosY + brushStroke.brushSize) { collisionBox.w = brushStroke.currentPosY + brushStroke.brushSize; }
                
                if (collisionBox.x > brushStroke.lastPosX - brushStroke.brushSize) { collisionBox.x = brushStroke.lastPosX - brushStroke.brushSize; }
                if (collisionBox.y > brushStroke.lastPosY - brushStroke.brushSize) { collisionBox.y = brushStroke.lastPosY - brushStroke.brushSize; }
                if (collisionBox.z < brushStroke.lastPosX + brushStroke.brushSize) { collisionBox.z = brushStroke.lastPosX + brushStroke.brushSize; }
                if (collisionBox.w < brushStroke.lastPosY + brushStroke.brushSize) { collisionBox.w = brushStroke.lastPosY + brushStroke.brushSize; }
            }
            
            collisionBoxX = collisionBox.x;
            collisionBoxY = collisionBox.y;
            collisionBoxZ = collisionBox.z;
            collisionBoxW = collisionBox.w;
        }

        public void RecalculateCollisionBoxAndAvgPos()
        {
            Vector4 collisionBox = new Vector4(Mathf.Infinity, Mathf.Infinity, 0, 0);
            Vector2 avgPos = Vector2.zero;

            foreach (var brushStroke in brushStrokes)
            {
                if (collisionBox.x > brushStroke.currentPosX - brushStroke.brushSize) { collisionBox.x = brushStroke.currentPosX - brushStroke.brushSize; }
                if (collisionBox.y > brushStroke.currentPosY - brushStroke.brushSize) { collisionBox.y = brushStroke.currentPosY - brushStroke.brushSize; }
                if (collisionBox.z < brushStroke.currentPosX + brushStroke.brushSize) { collisionBox.z = brushStroke.currentPosX + brushStroke.brushSize; }
                if (collisionBox.w < brushStroke.currentPosY + brushStroke.brushSize) { collisionBox.w = brushStroke.currentPosY + brushStroke.brushSize; }
                
                if (collisionBox.x > brushStroke.lastPosX - brushStroke.brushSize) { collisionBox.x = brushStroke.lastPosX - brushStroke.brushSize; }
                if (collisionBox.y > brushStroke.lastPosY - brushStroke.brushSize) { collisionBox.y = brushStroke.lastPosY - brushStroke.brushSize; }
                if (collisionBox.z < brushStroke.lastPosX + brushStroke.brushSize) { collisionBox.z = brushStroke.lastPosX + brushStroke.brushSize; }
                if (collisionBox.w < brushStroke.lastPosY + brushStroke.brushSize) { collisionBox.w = brushStroke.lastPosY + brushStroke.brushSize; }
                
                if(brushStroke.GetLastPos() == brushStroke.GetCurrentPos())
                    continue;
                
                avgPos += brushStroke.GetCurrentPos();
            }
            
            collisionBoxX = collisionBox.x;
            collisionBoxY = collisionBox.y;
            collisionBoxZ = collisionBox.z;
            collisionBoxW = collisionBox.w;
            
            avgPos /= brushStrokes.Count - 1;
            avgPosX = avgPos.x;
            avgPosY = avgPos.y;
        }

        public float GetAverageBrushSize()
        {
            float avgBrushSize = 0;
            foreach (var brushStroke in brushStrokes)
            {
                avgBrushSize += brushStroke.brushSize;
            }
            return avgBrushSize / brushStrokes.Count;
        }

        public void SetBrushSize(float _amount)
        {
            for (var i = 0; i < brushStrokes.Count; i++)
            {
                var brushStroke = brushStrokes[i];
                brushStroke.brushSize = Mathf.Clamp(_amount, 1, float.MaxValue);
                brushStrokes[i] = brushStroke;
            }
        }
    }

    public struct BrushStroke
    {
        public float lastPosX;
        public float lastPosY;
        public float currentPosX;
        public float currentPosY;
        public float brushSize;
        public float endTime;
        public float startTime;

        public BrushStroke(Vector2 _lastPos, Vector2 _currentPos, float _brushSize, float _endTime, float _startTime)
        {
            lastPosX = _lastPos.x;
            lastPosY = _lastPos.y;
            currentPosX = _currentPos.x;
            currentPosY = _currentPos.y;
            brushSize = _brushSize;
            endTime = _endTime;
            startTime = _startTime;
        }

        public Vector2 GetLastPos()
        {
            return new Vector2(lastPosX, lastPosY);
        }
        public Vector2 GetCurrentPos()
        {
            return new Vector2(currentPosX, currentPosY);
        }
        public Vector4 GetCollisionBox()
        {
            return new Vector4(lastPosX, lastPosY, currentPosX, currentPosY);
        }
        
        public Vector2 GetStartPos()
        {
            return new Vector2(lastPosX, lastPosY);
        }
        public Vector2 GetEndPos()
        {
            return new Vector2(currentPosX, currentPosY);
        }
    }
}
