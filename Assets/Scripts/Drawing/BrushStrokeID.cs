using System;
using System.Collections.Generic;
using Managers;
using Newtonsoft.Json;
using UnityEngine;

namespace Drawing
{
    public class BrushStrokeID
    {
        [JsonIgnore]
        public List<BrushStrokePixel[]> pixels;
        [JsonIgnore]
        public List<uint[]> bounds;
        [JsonIgnore]
        public bool shouldDelete;
        public float startTimeWhenDrawn;
        public float endTimeWhenDrawn;
        public float startTimeOld;
        public float endTimeOld;
        
        public float collisionBoxMinX;
        public float collisionBoxMinY;
        public float collisionBoxMinZ;
        public float collisionBoxMaxX;
        public float collisionBoxMaxY;
        public float collisionBoxMaxZ;
        public PaintType paintType;
        public float startTime;
        public float endTime;
        public int indexWhenDrawn;
        public float avgPosX;
        public float avgPosY;
        public float avgPosZ;
        public float angle;
        public float scale;
        
        public List<BrushStroke> brushStrokes;

        [JsonConstructor]
        public BrushStrokeID(List<BrushStroke> _brushStrokes, List<uint[]> _bounds, PaintType _paintType, float _startTime, float _endTime, Vector3 _collisionBoxMin, Vector3 _collisionBoxMax, int _indexWhenDrawn, Vector3 _avgPos, float _angle = 0, float _scale = 1)
        {
            brushStrokes = _brushStrokes;
            bounds = _bounds;
            paintType = _paintType;
            startTime = _startTime;
            endTime = _endTime;
            startTimeWhenDrawn = _startTime;
            endTimeWhenDrawn = _endTime;
            startTimeOld = _startTime;
            endTimeOld = _endTime;
            collisionBoxMinX = _collisionBoxMin.x;
            collisionBoxMinY = _collisionBoxMin.y;
            collisionBoxMinZ = _collisionBoxMin.z;
            collisionBoxMaxX = _collisionBoxMax.x;
            collisionBoxMaxY = _collisionBoxMax.y;
            collisionBoxMaxZ = _collisionBoxMax.z;
            indexWhenDrawn = _indexWhenDrawn;
            avgPosX = _avgPos.x;
            avgPosY = _avgPos.y;
            avgPosZ = _avgPos.z;
            angle = _angle;
            scale = _scale;
        }
        public BrushStrokeID(List<BrushStrokePixel[]> _pixels, List<BrushStroke> _brushStrokes, List<uint[]> _bounds, PaintType _paintType, float _startTime, float _endTime, Vector3 _collisionBoxMin, Vector3 _collisionBoxMax, int _indexWhenDrawn, Vector3 _avgPos, float _angle = 0, float _scale = 1)
        {
            pixels = _pixels;
            brushStrokes = _brushStrokes;
            bounds = _bounds;
            paintType = _paintType;
            startTime = _startTime;
            endTime = _endTime;
            startTimeWhenDrawn = _startTime;
            endTimeWhenDrawn = _endTime;
            startTimeOld = _startTime;
            endTimeOld = _endTime;
            collisionBoxMinX = _collisionBoxMin.x;
            collisionBoxMinY = _collisionBoxMin.y;
            collisionBoxMinZ = _collisionBoxMin.z;
            collisionBoxMaxX = _collisionBoxMax.x;
            collisionBoxMaxY = _collisionBoxMax.y;
            collisionBoxMaxZ = _collisionBoxMax.z;
            indexWhenDrawn = _indexWhenDrawn;
            avgPosX = _avgPos.x;
            avgPosY = _avgPos.y;
            avgPosZ = _avgPos.z;
            angle = _angle;
            scale = _scale;
        }
        
        public BrushStrokeID(BrushStrokeID _brushStrokeID, int _indexWhenDrawn)
        {
            pixels = new List<BrushStrokePixel[]>(_brushStrokeID.pixels);
            brushStrokes = new List<BrushStroke>(_brushStrokeID.brushStrokes);
            bounds = new List<uint[]>(_brushStrokeID.bounds);
            paintType = _brushStrokeID.paintType;
            startTime = _brushStrokeID.startTime;
            endTime = _brushStrokeID.endTime;
            startTimeWhenDrawn = _brushStrokeID.startTime;
            endTimeWhenDrawn = _brushStrokeID.endTime;
            startTimeOld = _brushStrokeID.startTime;
            endTimeOld = _brushStrokeID.endTime;
            collisionBoxMinX = _brushStrokeID.collisionBoxMinX;
            collisionBoxMinY = _brushStrokeID.collisionBoxMinY;
            collisionBoxMaxX = _brushStrokeID.collisionBoxMaxX;
            collisionBoxMaxY = _brushStrokeID.collisionBoxMaxY;
            avgPosX = _brushStrokeID.avgPosX;
            avgPosY = _brushStrokeID.avgPosY;
            avgPosZ = _brushStrokeID.avgPosZ;
            angle = _brushStrokeID.angle;
            scale = _brushStrokeID.scale;
            indexWhenDrawn = _indexWhenDrawn;
        }
        
        public BrushStrokeID(PaintType _paintType, float _startTime, float _endTime, Vector4 _collisionBox, int _indexWhenDrawn, Vector3 _avgPos, float _angle = 0, float _scale = 1)
        {
            paintType = _paintType;
            startTime = _startTime;
            endTime = _endTime;
            startTimeWhenDrawn = _startTime;
            endTimeWhenDrawn = _endTime;
            startTimeOld = _startTime;
            endTimeOld = _endTime;
            collisionBoxMinX = _collisionBox.x;
            collisionBoxMinY = _collisionBox.y;
            collisionBoxMaxX = _collisionBox.z;
            collisionBoxMaxY = _collisionBox.w;
            indexWhenDrawn = _indexWhenDrawn;
            avgPosX = _avgPos.x;
            avgPosY = _avgPos.y;
            avgPosZ = _avgPos.z;
            angle = _angle;
            scale = _scale;
        }

        public Vector2 GetTime()
        {
            return new Vector2(startTime, endTime);
        }

        public Vector4 GetCollisionBox()
        {
            return new Vector4(collisionBoxMinX, collisionBoxMinY, collisionBoxMaxX, collisionBoxMaxY);
        }

        public Vector3 GetMinCorner()
        {
            return new Vector3(collisionBoxMinX, collisionBoxMinY, collisionBoxMinZ);
        }
        public Vector3 GetMaxCorner()
        {
            return new Vector3(collisionBoxMaxX, collisionBoxMaxY, collisionBoxMaxZ);
        }

        public Vector2 GetAvgPos()
        {
            return new Vector2(avgPosX, avgPosY);
        }

        public void RecalculateCollisionBoxAndAvgPos()
        {
            Vector4 collisionBox = new Vector4(Mathf.Infinity, Mathf.Infinity, 0, 0);
            
            foreach (var brushStroke in brushStrokes)
            {
                if (collisionBox.x > brushStroke.posX - brushStroke.brushSize) { collisionBox.x = brushStroke.posX - brushStroke.brushSize; }
                if (collisionBox.y > brushStroke.posY - brushStroke.brushSize) { collisionBox.y = brushStroke.posY - brushStroke.brushSize; }
                if (collisionBox.z < brushStroke.posX + brushStroke.brushSize) { collisionBox.z = brushStroke.posX + brushStroke.brushSize; }
                if (collisionBox.w < brushStroke.posY + brushStroke.brushSize) { collisionBox.w = brushStroke.posY + brushStroke.brushSize; }
            }
            
            collisionBoxMinX = collisionBox.x;
            collisionBoxMinY = collisionBox.y;
            collisionBoxMaxX = collisionBox.z;
            collisionBoxMaxY = collisionBox.w;
            
            avgPosX = collisionBoxMinX + (collisionBoxMaxX - collisionBoxMinX) / 2;
            avgPosY = collisionBoxMinY + (collisionBoxMaxY - collisionBoxMinY) / 2;
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
                brushStroke.brushSize = Mathf.Clamp(_amount, 0.000000000001f, float.MaxValue);
                brushStrokes[i] = brushStroke;
            }
        }

        public void Reverse()
        {
            // if (brushStrokes.Count < 2)
            // {
            //     return;
            // }
            //
            // brushStrokes.RemoveAt(0);
            // brushStrokes.Add(brushStrokes[^1]);
            // brushStrokes.Reverse();
            //
            // for (int i = 0; i < brushStrokes.Count; i++)
            // {
            //     var brushStroke = brushStrokes[i];
            //     float newStartTime = brushStroke.endTime;
            //     float newEndTime = brushStroke.startTime;
            //     Vector2 newStartPos = brushStroke.GetEndPos();
            //     Vector2 newEndPos = brushStroke.GetStartPos();
            //     
            //     brushStroke.startTime = newStartTime;
            //     brushStroke.endTime = newEndTime;
            //
            //     brushStroke.startPosX = newStartPos.x;
            //     brushStroke.startPosY = newStartPos.y;
            //     brushStroke.endPosX = newEndPos.x;
            //     brushStroke.endPosY = newEndPos.y;
            //
            //     brushStrokes[i] = brushStroke;
            // }
            //
            // BrushStroke startBrushStroke = brushStrokes[0];
            // startBrushStroke.endPosX = startBrushStroke.startPosX;
            // startBrushStroke.endPosY = startBrushStroke.startPosY;
            // brushStrokes[0] = startBrushStroke;
        }
    }

    public struct BrushStrokePixel
    {
        public UInt16 x;
        public UInt16 y;
        public float color;
    }

    public struct BrushStroke
    {
        public float posX;
        public float posY;
        public float posZ;
        public float brushSize;
        public float colorTime;
    
        public BrushStroke(Vector2 _startPos, float _brushSize, float _colorTime)
        {
            posX = _startPos.x;
            posY = _startPos.y;
            brushSize = _brushSize;
            colorTime = _colorTime;
            posZ = 0;
        }
        
        public BrushStroke(Vector3 _startPos, float _brushSize, float _colorTime)
        {
            posX = _startPos.x;
            posY = _startPos.y;
            posZ = _startPos.z;
            brushSize = _brushSize;
            colorTime = _colorTime;
        }
    
        public Vector3 GetPos()
        {
            return new Vector3(posX, posY, posZ);
        }
    }
}
