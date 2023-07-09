﻿using System.Collections.Generic;
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
        public float lastTime;
        public float currentTime;
        public int indexWhenDrawn;
        public float avgPosX;
        public float avgPosY;
        public float angle;
        public float size;

        public BrushStrokeID(List<BrushStroke> _brushStrokes, PaintType _paintType, float _lastTime, float _currentTime, Vector4 _collisionBox, int _indexWhenDrawn, Vector2 _avgPos, float _angle = 0, float _size = 1)
        {
            brushStrokes = _brushStrokes;
            paintType = _paintType;
            lastTime = _lastTime;
            currentTime = _currentTime;
            collisionBoxX = _collisionBox.x;
            collisionBoxY = _collisionBox.y;
            collisionBoxZ = _collisionBox.z;
            collisionBoxW = _collisionBox.w;
            indexWhenDrawn = _indexWhenDrawn;
            avgPosX = _avgPos.x;
            avgPosY = _avgPos.y;
            angle = _angle;
            size = _size;
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
                if (collisionBox.x > brushStroke.currentPosX - brushStroke.strokeBrushSize) { collisionBox.x = brushStroke.currentPosX - brushStroke.strokeBrushSize; }
                if (collisionBox.y > brushStroke.currentPosY - brushStroke.strokeBrushSize) { collisionBox.y = brushStroke.currentPosY - brushStroke.strokeBrushSize; }
                if (collisionBox.z < brushStroke.currentPosX + brushStroke.strokeBrushSize) { collisionBox.z = brushStroke.currentPosX + brushStroke.strokeBrushSize; }
                if (collisionBox.w < brushStroke.currentPosY + brushStroke.strokeBrushSize) { collisionBox.w = brushStroke.currentPosY + brushStroke.strokeBrushSize; }
                
                if (collisionBox.x > brushStroke.lastPosX - brushStroke.strokeBrushSize) { collisionBox.x = brushStroke.lastPosX - brushStroke.strokeBrushSize; }
                if (collisionBox.y > brushStroke.lastPosY - brushStroke.strokeBrushSize) { collisionBox.y = brushStroke.lastPosY - brushStroke.strokeBrushSize; }
                if (collisionBox.z < brushStroke.lastPosX + brushStroke.strokeBrushSize) { collisionBox.z = brushStroke.lastPosX + brushStroke.strokeBrushSize; }
                if (collisionBox.w < brushStroke.lastPosY + brushStroke.strokeBrushSize) { collisionBox.w = brushStroke.lastPosY + brushStroke.strokeBrushSize; }
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
                if (collisionBox.x > brushStroke.currentPosX - brushStroke.strokeBrushSize) { collisionBox.x = brushStroke.currentPosX - brushStroke.strokeBrushSize; }
                if (collisionBox.y > brushStroke.currentPosY - brushStroke.strokeBrushSize) { collisionBox.y = brushStroke.currentPosY - brushStroke.strokeBrushSize; }
                if (collisionBox.z < brushStroke.currentPosX + brushStroke.strokeBrushSize) { collisionBox.z = brushStroke.currentPosX + brushStroke.strokeBrushSize; }
                if (collisionBox.w < brushStroke.currentPosY + brushStroke.strokeBrushSize) { collisionBox.w = brushStroke.currentPosY + brushStroke.strokeBrushSize; }
                
                if (collisionBox.x > brushStroke.lastPosX - brushStroke.strokeBrushSize) { collisionBox.x = brushStroke.lastPosX - brushStroke.strokeBrushSize; }
                if (collisionBox.y > brushStroke.lastPosY - brushStroke.strokeBrushSize) { collisionBox.y = brushStroke.lastPosY - brushStroke.strokeBrushSize; }
                if (collisionBox.z < brushStroke.lastPosX + brushStroke.strokeBrushSize) { collisionBox.z = brushStroke.lastPosX + brushStroke.strokeBrushSize; }
                if (collisionBox.w < brushStroke.lastPosY + brushStroke.strokeBrushSize) { collisionBox.w = brushStroke.lastPosY + brushStroke.strokeBrushSize; }
                
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
    }

    public struct BrushStroke
    {
        public float lastPosX;
        public float lastPosY;
        public float currentPosX;
        public float currentPosY;
        public float strokeBrushSize;
        public float currentTime;
        public float lastTime;

        public BrushStroke(Vector2 _lastPos, Vector2 _currentPos, float _strokeBrushSize, float _currentTime, float _lastTime)
        {
            lastPosX = _lastPos.x;
            lastPosY = _lastPos.y;
            currentPosX = _currentPos.x;
            currentPosY = _currentPos.y;
            strokeBrushSize = _strokeBrushSize;
            currentTime = _currentTime;
            lastTime = _lastTime;
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
