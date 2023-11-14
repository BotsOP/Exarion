using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Drawing
{
    public class BrushStrokeID
    {
        public List<UInt16[]> pixels;
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
        
        [JsonConstructor]
        public BrushStrokeID(List<UInt16[]> _pixels, PaintType _paintType, float _startTime, float _endTime, Vector4 _collisionBox, int _indexWhenDrawn, Vector3 _avgPos, float _angle = 0, float _scale = 1)
        {
            pixels = _pixels;
            paintType = _paintType;
            startTime = _startTime;
            endTime = _endTime;
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
        
        public BrushStrokeID(List<UInt16[]> _pixels, PaintType _paintType, float _startTime, float _endTime, Vector3 _collisionBoxMin, Vector3 _collisionBoxMax, int _indexWhenDrawn, Vector3 _avgPos, float _angle = 0, float _scale = 1)
        {
            pixels = _pixels;
            paintType = _paintType;
            startTime = _startTime;
            endTime = _endTime;
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
            pixels = new List<UInt16[]>(_brushStrokeID.pixels);
            paintType = _brushStrokeID.paintType;
            startTime = _brushStrokeID.startTime;
            endTime = _brushStrokeID.endTime;
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

        public void RecalculateAvgPos()
        {
            // Vector3 avgPos = Vector2.zero;
            // for (var i = 1; i < brushStrokes.Count; i++)
            // {
            //     var brushStroke = brushStrokes[i];
            //     avgPos += brushStroke.GetEndPos();
            // }
            //
            // avgPos /= brushStrokes.Count - 1;
            // avgPosX = avgPos.x;
            // avgPosY = avgPos.y;
        }

        public void RecalculateCollisionBox()
        {
            // Vector4 collisionBox = new Vector4(Mathf.Infinity, Mathf.Infinity, 0, 0);
            //
            // foreach (var brushStroke in brushStrokes)
            // {
            //     if (collisionBox.x > brushStroke.endPosX - brushStroke.brushSize) { collisionBox.x = brushStroke.endPosX - brushStroke.brushSize; }
            //     if (collisionBox.y > brushStroke.endPosY - brushStroke.brushSize) { collisionBox.y = brushStroke.endPosY - brushStroke.brushSize; }
            //     if (collisionBox.z < brushStroke.endPosX + brushStroke.brushSize) { collisionBox.z = brushStroke.endPosX + brushStroke.brushSize; }
            //     if (collisionBox.w < brushStroke.endPosY + brushStroke.brushSize) { collisionBox.w = brushStroke.endPosY + brushStroke.brushSize; }
            //     
            //     if (collisionBox.x > brushStroke.startPosX - brushStroke.brushSize) { collisionBox.x = brushStroke.startPosX - brushStroke.brushSize; }
            //     if (collisionBox.y > brushStroke.startPosY - brushStroke.brushSize) { collisionBox.y = brushStroke.startPosY - brushStroke.brushSize; }
            //     if (collisionBox.z < brushStroke.startPosX + brushStroke.brushSize) { collisionBox.z = brushStroke.startPosX + brushStroke.brushSize; }
            //     if (collisionBox.w < brushStroke.startPosY + brushStroke.brushSize) { collisionBox.w = brushStroke.startPosY + brushStroke.brushSize; }
            // }
            //
            // collisionBoxMinX = collisionBox.x;
            // collisionBoxMinY = collisionBox.y;
            // collisionBoxMaxX = collisionBox.z;
            // collisionBoxMaxY = collisionBox.w;
        }

        public void RecalculateCollisionBoxAndAvgPos()
        {
            // Vector4 collisionBox = new Vector4(Mathf.Infinity, Mathf.Infinity, 0, 0);
            // Vector3 avgPos = Vector2.zero;
            //
            // foreach (var brushStroke in brushStrokes)
            // {
            //     if (collisionBox.x > brushStroke.endPosX - brushStroke.brushSize) { collisionBox.x = brushStroke.endPosX - brushStroke.brushSize; }
            //     if (collisionBox.y > brushStroke.endPosY - brushStroke.brushSize) { collisionBox.y = brushStroke.endPosY - brushStroke.brushSize; }
            //     if (collisionBox.z < brushStroke.endPosX + brushStroke.brushSize) { collisionBox.z = brushStroke.endPosX + brushStroke.brushSize; }
            //     if (collisionBox.w < brushStroke.endPosY + brushStroke.brushSize) { collisionBox.w = brushStroke.endPosY + brushStroke.brushSize; }
            //     
            //     if (collisionBox.x > brushStroke.startPosX - brushStroke.brushSize) { collisionBox.x = brushStroke.startPosX - brushStroke.brushSize; }
            //     if (collisionBox.y > brushStroke.startPosY - brushStroke.brushSize) { collisionBox.y = brushStroke.startPosY - brushStroke.brushSize; }
            //     if (collisionBox.z < brushStroke.startPosX + brushStroke.brushSize) { collisionBox.z = brushStroke.startPosX + brushStroke.brushSize; }
            //     if (collisionBox.w < brushStroke.startPosY + brushStroke.brushSize) { collisionBox.w = brushStroke.startPosY + brushStroke.brushSize; }
            //     
            //     if(brushStroke.GetStartPos() == brushStroke.GetEndPos())
            //         continue;
            //     
            //     avgPos += brushStroke.GetEndPos();
            // }
            //
            // collisionBoxMinX = collisionBox.x;
            // collisionBoxMinY = collisionBox.y;
            // collisionBoxMaxX = collisionBox.z;
            // collisionBoxMaxY = collisionBox.w;
            //
            // avgPos /= brushStrokes.Count - 1;
            // avgPosX = avgPos.x;
            // avgPosY = avgPos.y;
        }

        public float GetAverageBrushSize()
        {
            // float avgBrushSize = 0;
            // foreach (var brushStroke in brushStrokes)
            // {
            //     avgBrushSize += brushStroke.brushSize;
            // }
            // return avgBrushSize / brushStrokes.Count;
            return 0;
        }

        public void SetBrushSize(float _amount)
        {
            // for (var i = 0; i < brushStrokes.Count; i++)
            // {
            //     var brushStroke = brushStrokes[i];
            //     brushStroke.brushSize = Mathf.Clamp(_amount, 1, float.MaxValue);
            //     brushStrokes[i] = brushStroke;
            // }
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

    // public struct BrushStroke
    // {
    //     public float startPosX;
    //     public float startPosY;
    //     public float startPosZ;
    //     public float endPosX;
    //     public float endPosY;
    //     public float endPosZ;
    //     public float brushSize;
    //     public float endTime;
    //     public float startTime;
    //
    //     public BrushStroke(Vector2 _startPos, Vector2 _endPos, float _brushSize, float _endTime, float _startTime)
    //     {
    //         startPosX = _startPos.x;
    //         startPosY = _startPos.y;
    //         endPosX = _endPos.x;
    //         endPosY = _endPos.y;
    //         brushSize = _brushSize;
    //         endTime = _endTime;
    //         startTime = _startTime;
    //         startPosZ = 0;
    //         endPosZ = 0;
    //     }
    //     
    //     public BrushStroke(Vector3 _startPos, Vector3 _endPos, float _brushSize, float _endTime, float _startTime)
    //     {
    //         startPosX = _startPos.x;
    //         startPosY = _startPos.y;
    //         endPosX = _endPos.x;
    //         endPosY = _endPos.y;
    //         startPosZ = _startPos.z;
    //         endPosZ = _endPos.z;
    //         brushSize = _brushSize;
    //         endTime = _endTime;
    //         startTime = _startTime;
    //     }
    //
    //     public Vector3 GetStartPos()
    //     {
    //         return new Vector3(startPosX, startPosY, startPosZ);
    //     }
    //     public Vector3 GetEndPos()
    //     {
    //         return new Vector3(endPosX, endPosY, endPosZ);
    //     }
    //     public Vector4 GetCollisionBox()
    //     {
    //         return new Vector4(startPosX, startPosY, endPosX, endPosY);
    //     }
    // }
}
