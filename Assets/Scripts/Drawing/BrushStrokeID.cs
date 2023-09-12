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
                avgPos += brushStroke.GetEndPos();
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
                if (collisionBox.x > brushStroke.endPosX - brushStroke.brushSize) { collisionBox.x = brushStroke.endPosX - brushStroke.brushSize; }
                if (collisionBox.y > brushStroke.endPosY - brushStroke.brushSize) { collisionBox.y = brushStroke.endPosY - brushStroke.brushSize; }
                if (collisionBox.z < brushStroke.endPosX + brushStroke.brushSize) { collisionBox.z = brushStroke.endPosX + brushStroke.brushSize; }
                if (collisionBox.w < brushStroke.endPosY + brushStroke.brushSize) { collisionBox.w = brushStroke.endPosY + brushStroke.brushSize; }
                
                if (collisionBox.x > brushStroke.startPosX - brushStroke.brushSize) { collisionBox.x = brushStroke.startPosX - brushStroke.brushSize; }
                if (collisionBox.y > brushStroke.startPosY - brushStroke.brushSize) { collisionBox.y = brushStroke.startPosY - brushStroke.brushSize; }
                if (collisionBox.z < brushStroke.startPosX + brushStroke.brushSize) { collisionBox.z = brushStroke.startPosX + brushStroke.brushSize; }
                if (collisionBox.w < brushStroke.startPosY + brushStroke.brushSize) { collisionBox.w = brushStroke.startPosY + brushStroke.brushSize; }
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
                if (collisionBox.x > brushStroke.endPosX - brushStroke.brushSize) { collisionBox.x = brushStroke.endPosX - brushStroke.brushSize; }
                if (collisionBox.y > brushStroke.endPosY - brushStroke.brushSize) { collisionBox.y = brushStroke.endPosY - brushStroke.brushSize; }
                if (collisionBox.z < brushStroke.endPosX + brushStroke.brushSize) { collisionBox.z = brushStroke.endPosX + brushStroke.brushSize; }
                if (collisionBox.w < brushStroke.endPosY + brushStroke.brushSize) { collisionBox.w = brushStroke.endPosY + brushStroke.brushSize; }
                
                if (collisionBox.x > brushStroke.startPosX - brushStroke.brushSize) { collisionBox.x = brushStroke.startPosX - brushStroke.brushSize; }
                if (collisionBox.y > brushStroke.startPosY - brushStroke.brushSize) { collisionBox.y = brushStroke.startPosY - brushStroke.brushSize; }
                if (collisionBox.z < brushStroke.startPosX + brushStroke.brushSize) { collisionBox.z = brushStroke.startPosX + brushStroke.brushSize; }
                if (collisionBox.w < brushStroke.startPosY + brushStroke.brushSize) { collisionBox.w = brushStroke.startPosY + brushStroke.brushSize; }
                
                if(brushStroke.GetStartPos() == brushStroke.GetEndPos())
                    continue;
                
                avgPos += brushStroke.GetEndPos();
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

        public void Reverse()
        {
            if (brushStrokes.Count < 2)
            {
                return;
            }
            
            brushStrokes.RemoveAt(0);
            brushStrokes.Add(brushStrokes[^1]);
            brushStrokes.Reverse();

            for (int i = 0; i < brushStrokes.Count; i++)
            {
                var brushStroke = brushStrokes[i];
                float newStartTime = brushStroke.endTime;
                float newEndTime = brushStroke.startTime;
                Vector2 newStartPos = brushStroke.GetEndPos();
                Vector2 newEndPos = brushStroke.GetStartPos();
                
                brushStroke.startTime = newStartTime;
                brushStroke.endTime = newEndTime;

                brushStroke.startPosX = newStartPos.x;
                brushStroke.startPosY = newStartPos.y;
                brushStroke.endPosX = newEndPos.x;
                brushStroke.endPosY = newEndPos.y;
            
                brushStrokes[i] = brushStroke;
            }

            BrushStroke startBrushStroke = brushStrokes[0];
            startBrushStroke.endPosX = startBrushStroke.startPosX;
            startBrushStroke.endPosY = startBrushStroke.startPosY;
            brushStrokes[0] = startBrushStroke;
        }
    }

    public struct BrushStroke
    {
        public float startPosX;
        public float startPosY;
        public float endPosX;
        public float endPosY;
        public float brushSize;
        public float endTime;
        public float startTime;

        public BrushStroke(Vector2 _lastPos, Vector2 _currentPos, float _brushSize, float _endTime, float _startTime)
        {
            startPosX = _lastPos.x;
            startPosY = _lastPos.y;
            endPosX = _currentPos.x;
            endPosY = _currentPos.y;
            brushSize = _brushSize;
            endTime = _endTime;
            startTime = _startTime;
        }

        public Vector2 GetStartPos()
        {
            return new Vector2(startPosX, startPosY);
        }
        public Vector2 GetEndPos()
        {
            return new Vector2(endPosX, endPosY);
        }
        public Vector4 GetCollisionBox()
        {
            return new Vector4(startPosX, startPosY, endPosX, endPosY);
        }
    }
}
