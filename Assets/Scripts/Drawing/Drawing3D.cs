using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using UI;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Drawing
{
    public class Drawing3D : DrawingLib
    {
        private CustomRenderTexture rtIndividualTemp;

        private Material paintMaterial;
        private CommandBuffer commandBuffer;
        public Renderer rend;
        
        private readonly int FirstStroke = Shader.PropertyToID("_FirstStroke");
        private readonly int LastCursorPos = Shader.PropertyToID("_LastCursorPos");
        private readonly int BrushSize = Shader.PropertyToID("_BrushSize");
        private readonly int TimeColor = Shader.PropertyToID("_TimeColor");
        private readonly int PreviousTimeColor = Shader.PropertyToID("_PreviousTimeColor");
        private readonly int StrokeID = Shader.PropertyToID("_StrokeID");
        private readonly int CursorPos = Shader.PropertyToID("_CursorPos");
        private readonly int IDTex = Shader.PropertyToID("_IDTex");


        public Drawing3D(int _imageWidth, int _imageHeight, Transform _sphere1) : base(_imageWidth, _imageHeight)
        {
            paintMaterial = new Material(Resources.Load<Shader>("DrawPainter"));
            
            commandBuffer = new CommandBuffer();
            commandBuffer.name = "3DUVTimePainter";
            
            rtIndividualTemp = new CustomRenderTexture(_imageWidth, _imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                enableRandomWrite = true,
                useMipMap = false,
                name = "rtIndividualTemp",
            };
        }

        public virtual void Draw(Vector3 _startPos, Vector3 _endPos, float _strokeBrushSize, PaintType _paintType, float _startTime = 0, float _endTime = 0, bool _firstStroke = false, float _strokeID = 0)
        {
            switch (_paintType)
            {
                case PaintType.PaintUnderEverything:
                    paintMaterial.SetInt(FirstStroke, _firstStroke ? 1 : 0);
                    paintMaterial.SetVector(CursorPos, _endPos);
                    paintMaterial.SetVector(LastCursorPos, _startPos);
                    paintMaterial.SetFloat(BrushSize, _strokeBrushSize);
                    paintMaterial.SetFloat(TimeColor, _endTime);
                    paintMaterial.SetFloat(PreviousTimeColor, _startTime);
                    paintMaterial.SetFloat(StrokeID, _strokeID);
                    for (int i = 0; i < subMeshCount; i++)
                    {
                        paintMaterial.SetTexture(IDTex, rtIDs[i]);
                    
                        commandBuffer.SetRenderTarget(rtIndividualTemp);
                        commandBuffer.DrawRenderer(rend, paintMaterial, i);
                        
                        commandBuffer.SetComputeTextureParam(textureHelperShader, copyKernelID, "_OrgTexColor", rtIndividualTemp);
                        commandBuffer.SetComputeTextureParam(textureHelperShader, copyKernelID, "_FinalTexColor", rtWholeTemps[i]);
                        commandBuffer.SetComputeTextureParam(textureHelperShader, copyKernelID, "_TempTexID", rtWholeIDTemps[i]);
                        commandBuffer.SetComputeFloatParam(textureHelperShader, "_StrokeID", _strokeID);
                        commandBuffer.DispatchCompute(textureHelperShader, copyKernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
                        ExecuteBuffer();
                    }
                    break;
            }
        }

        private void ExecuteBuffer()
        {
            Graphics.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }
        
        public void SetupDrawBrushStroke(BrushStrokeID _brushStrokeID)
        {
            float oldStartTime = _brushStrokeID.brushStrokes[0].startTime;
            float oldEndTime = _brushStrokeID.brushStrokes[^1].startTime;
            
            bool firstStroke = true;
            foreach (var brushStroke in _brushStrokeID.brushStrokes)
            {
                float startTime = brushStroke.startTime.Remap(oldStartTime, oldEndTime, _brushStrokeID.startTime, _brushStrokeID.endTime);
                float endTime = brushStroke.endTime.Remap(oldStartTime, oldEndTime, _brushStrokeID.startTime, _brushStrokeID.endTime);
                
                Draw(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, _brushStrokeID.paintType, 
                     startTime, endTime, firstStroke, _brushStrokeID.indexWhenDrawn);
                firstStroke = false;
            }
            
            (List<BrushStrokePixel[]>, List<uint[]>) result = FinishDrawing(_brushStrokeID.indexWhenDrawn);
            _brushStrokeID.pixels = result.Item1;
            _brushStrokeID.bounds = result.Item2;
        }
        public void SetupDrawBrushStroke(List<BrushStrokeID> _brushStrokeIDs)
        {
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                float oldStartTime = brushStrokeID.brushStrokes[0].startTime;
                float oldEndTime = brushStrokeID.brushStrokes[^1].startTime;
            
                bool firstStroke = true;
                foreach (var brushStroke in brushStrokeID.brushStrokes)
                {
                    float startTime = brushStroke.startTime.Remap(oldStartTime, oldEndTime, brushStrokeID.startTime, brushStrokeID.endTime);
                    float endTime = brushStroke.endTime.Remap(oldStartTime, oldEndTime, brushStrokeID.startTime, brushStrokeID.endTime);
                
                    Draw(brushStroke.GetStartPos(), brushStroke.GetEndPos(), brushStroke.brushSize, brushStrokeID.paintType, 
                        startTime, endTime, firstStroke, brushStrokeID.indexWhenDrawn);
                    firstStroke = false;
                }
            
                (List<BrushStrokePixel[]>, List<uint[]>) result = FinishDrawing(brushStrokeID.indexWhenDrawn);
                brushStrokeID.pixels = result.Item1;
                brushStrokeID.bounds = result.Item2;
            }
        }
        
        public bool IsMouseOverBrushStroke(BrushStrokeID _brushStrokeID, Vector3 _worldPos)
        {
            Vector3 minCorner = _brushStrokeID.GetMinCorner();
            Vector3 maxCorner = _brushStrokeID.GetMaxCorner();
            if (CheckCollision(minCorner, maxCorner, _worldPos))
            {
                foreach (var brushStroke in _brushStrokeID.brushStrokes)
                {
                    float distLine = DistancePointToLine(brushStroke.GetStartPos(), brushStroke.GetEndPos(), _worldPos);
                    if (distLine < brushStroke.brushSize)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private uint[] CombineBounds(List<BrushStrokeID> _brushStrokeIDs, int _subMeshIndex)
        {
            uint[] combinedBounds = new uint[4];
            combinedBounds[0] = uint.MaxValue;
            combinedBounds[1] = uint.MaxValue;
            
            foreach (var brushStrokeID in _brushStrokeIDs)
            {
                uint tempLowX = brushStrokeID.bounds[_subMeshIndex][0];
                uint lowestX = combinedBounds[0];
                combinedBounds[0] = lowestX > tempLowX ? tempLowX : lowestX;
                
                uint tempLowY = brushStrokeID.bounds[_subMeshIndex][1];
                uint lowestY = combinedBounds[1];
                combinedBounds[1] = lowestY > tempLowY ? tempLowY : lowestY;
                
                uint tempHighestX = brushStrokeID.bounds[_subMeshIndex][2];
                uint highestX = combinedBounds[2];
                combinedBounds[2] = highestX < tempHighestX ? tempHighestX : highestX;
                
                uint tempHighestY = brushStrokeID.bounds[_subMeshIndex][3];
                uint highestY = combinedBounds[3];
                combinedBounds[3] = highestY < tempHighestY ? tempHighestY : highestY;
            }

            return combinedBounds;
        }
        
        private Vector3 CombineMinCorner(Vector3[] _collisionBoxes)
        {
            Vector3 collisionBox = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            foreach (Vector3 tempCollisionBox in _collisionBoxes)
            {
                if (collisionBox.x > tempCollisionBox.x) { collisionBox.x = tempCollisionBox.x; }
                if (collisionBox.y > tempCollisionBox.y) { collisionBox.y = tempCollisionBox.y; }
                if (collisionBox.z > tempCollisionBox.z) { collisionBox.z = tempCollisionBox.z; }
            }

            return collisionBox;
        }
        
        private Vector3 CombineMaxCorner(Vector3[] _collisionBoxes)
        {
            Vector3 collisionBox = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

            foreach (Vector3 tempCollisionBox in _collisionBoxes)
            {
                if (collisionBox.x < tempCollisionBox.x) { collisionBox.x = tempCollisionBox.x; }
                if (collisionBox.y < tempCollisionBox.y) { collisionBox.y = tempCollisionBox.y; }
                if (collisionBox.z < tempCollisionBox.z) { collisionBox.z = tempCollisionBox.z; }
            }

            return collisionBox;
        }

        private bool CheckCollision(Vector3 _minCorner1, Vector3 _maxCorner1, Vector3 _minCorner2, Vector3 _maxCorner2)
        {
            return (_minCorner1.x <= _maxCorner2.x && _maxCorner1.x >= _minCorner2.x) &&
                   (_minCorner1.y <= _maxCorner2.y && _maxCorner1.y >= _minCorner2.y) &&
                   (_minCorner1.z <= _maxCorner2.z && _maxCorner1.z >= _minCorner2.z);
        }
        private bool CheckCollision(Vector3 _minCorner, Vector3 _maxCorner, Vector3 _point)
        {
            return (_point.x > _minCorner.x && _point.x < _maxCorner.x) &&
                   (_point.y > _minCorner.y && _point.y < _maxCorner.y) &&
                   (_point.z > _minCorner.z && _point.z < _maxCorner.z);
        }

        private float DistancePointToLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
        {
            //Get heading
            Vector3 heading = (lineEnd - lineStart);
            float magnitudeMax = heading.magnitude;
            heading.Normalize();

            //Do projection from the point but clamp it
            Vector3 lhs = point - lineStart;
            float dotP = Vector3.Dot(lhs, heading);
            dotP = Mathf.Clamp(dotP, 0f, magnitudeMax);
            return Vector3.Distance(lineStart + heading * dotP, point);
        }
    }
}