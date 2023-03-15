using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Drawing
{
    public class Drawing
    {
        public CustomRenderTexture rt;
        private CustomRenderTexture rtID;
        public List<BrushStrokeID> brushStrokesID = new List<BrushStrokeID>();
        public List<BrushStroke> brushStrokes = new List<BrushStroke>();

        public int brushDrawID => brushStrokes.Count;
        public int lastBrushDrawID
        {
            get {
                int lastID = 0;
                if (brushStrokesID.Count > 0)
                {
                    lastID = brushStrokesID[^1].endID + 1;
                }
                return lastID;
            }
        }

        private ComputeShader paintShader;
        private int paintUnderOwnLineKernelID;
        private int paintUnderEverythingKernelID;
        private int paintOverEverythingKernelID;
        private int paintOverOwnLineKernelID;
        private int eraseKernelID;
        private Vector3 threadGroupSizeOut;
        private Vector3 threadGroupSize;

        public int GetNewID()
        {
            return Random.Range(1, 2147483647);
        }
        public Drawing(int imageWidth, int imageHeight)
        {
            paintShader = Resources.Load<ComputeShader>("PaintShader");

            paintUnderOwnLineKernelID = paintShader.FindKernel("PaintUnderOwnLine");
            paintUnderEverythingKernelID = paintShader.FindKernel("PaintUnderEverything");
            paintOverEverythingKernelID = paintShader.FindKernel("PaintOverEverything");
            paintOverOwnLineKernelID = paintShader.FindKernel("PaintOverOwnLine");
            eraseKernelID = paintShader.FindKernel("Erase");
        
            paintShader.GetKernelThreadGroupSizes(paintUnderOwnLineKernelID, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
            threadGroupSizeOut.x = threadGroupSizeX;
            threadGroupSizeOut.y = threadGroupSizeY;

            rt = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true,
            };
            rtID = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true,
            };

            
            Graphics.SetRenderTarget(rt);
            GL.Clear(false, true, Color.white);
            
            Graphics.SetRenderTarget(rtID);
            Color idColor = new Color(-1, -1, -1);
            GL.Clear(false, true, idColor);
            
            Graphics.SetRenderTarget(null);

            threadGroupSize.x = Mathf.CeilToInt(imageWidth / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt(imageHeight / threadGroupSizeOut.y);
        }
    
        public void Draw(Vector2 lastPos, Vector2 currentPos, float strokeBrushSize, PaintType paintType, float lastTime = 0, float brushTime = 0, bool firstStroke = false, int strokeID = 0)
        {
            threadGroupSize.x = Mathf.CeilToInt((math.abs(lastPos.x - currentPos.x) + strokeBrushSize * 2) / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt((math.abs(lastPos.y - currentPos.y) + strokeBrushSize * 2) / threadGroupSizeOut.y);
        
            Vector2 startPos = GetStartPos(lastPos, currentPos, strokeBrushSize);

            int kernelID = 0;
            switch (paintType)
            {
                case PaintType.PaintUnderEverything:
                    kernelID = paintUnderEverythingKernelID;
                    break;
                case PaintType.PaintOverEverything:
                    kernelID = paintOverEverythingKernelID;
                    break;
                case PaintType.PaintOverOwnLine:
                    kernelID = paintOverOwnLineKernelID;
                    break;
                case PaintType.PaintUnderOwnLine:
                    kernelID = paintUnderOwnLineKernelID;
                    break;
                case PaintType.Erase:
                    strokeBrushSize += 1;
                    kernelID = eraseKernelID;
                    break;
            }

            paintShader.SetBool("_FirstStroke", firstStroke);
            paintShader.SetVector("_CursorPos", currentPos);
            paintShader.SetVector("_LastCursorPos", lastPos);
            paintShader.SetVector("_StartPos", startPos);
            paintShader.SetFloat("_BrushSize", strokeBrushSize);
            paintShader.SetFloat("_TimeColor", brushTime);
            paintShader.SetFloat("_PreviousTimeColor", lastTime);
            paintShader.SetInt("_StrokeID", strokeID);
            paintShader.SetTexture(kernelID, "_IdTex", rtID);
            paintShader.SetTexture(kernelID, "_Result", rt);

            paintShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
        }

        public void Redraw(int brushstrokStartID, float startTime, float endTime)
        {
            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                if (i == brushstrokStartID)
                {
                    RedrawStroke(brushstrokStartID, startTime, endTime);
                    continue;
                }
                RedrawStroke(i);
            }
        }

        public Vector2 GetStartPos(Vector2 a, Vector2 b, float brushSize)
        {
            float lowestX = (a.x < b.x ? a.x : b.x) - brushSize;
            float lowestY = (a.y < b.y ? a.y : b.y) - brushSize;
            return new Vector2(lowestX, lowestY);
        }

        public void RedrawStroke(int brushstrokStartID)
        {
            BrushStrokeID brushStrokeID = brushStrokesID[brushstrokStartID];
            int startID = brushStrokeID.startID;
            int endID = brushStrokeID.endID;
            int newStrokeID = GetNewID();
            bool firstLoop = true;
            PaintType paintType = brushStrokeID.paintType;
            
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = brushStrokes[i];
        
                Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, paintType, stroke.lastTime, stroke.brushTime, firstLoop, newStrokeID);

                firstLoop = false;
            }
        }
    
        public void RedrawStroke(int brushstrokStartID, float startTime, float endTime)
        {
            BrushStrokeID brushStrokeID = brushStrokesID[brushstrokStartID];
            int startID = brushStrokeID.startID;
            int endID = brushStrokeID.endID;
            int newStrokeID = GetNewID();
            float previousTime = startTime;
            bool firstLoop = true;
            PaintType paintType = brushStrokeID.paintType;
        
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = brushStrokes[i];
        
                float currentTime = stroke.brushTime;
                if (startTime >= 0 && endTime >= 0)
                {
                    float idPercentage = ExtensionMethods.Remap(i, startID, endID, 0, 1);
                    currentTime = (endTime - startTime) * idPercentage + startTime;
                }

                stroke.brushTime = currentTime;
                stroke.lastTime = previousTime;

                brushStrokes[i] = stroke;
            
                Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, paintType, stroke.lastTime, stroke.brushTime, firstLoop, newStrokeID);

                firstLoop = false;
                previousTime = currentTime;
            }
        }

        public void RemoveStroke(int brushstrokStartID)
        {
            BrushStrokeID brushStrokeID = brushStrokesID[brushstrokStartID];
            int startID = brushStrokeID.startID;
            int endID = brushStrokeID.endID;
            
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = brushStrokes[i];
        
                Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, PaintType.Erase);
            }

            int amountToRemove = endID - startID;
            if (brushstrokStartID + 1 < brushStrokesID.Count)
            {
                AdjustBrushStrokeIDs(brushstrokStartID, amountToRemove);
            }

            if (startID > 0)
            {
                brushStrokes.RemoveRange(startID - 1, amountToRemove + 1);
            }
            else
            {
                brushStrokes.Clear();
            }
            brushStrokesID.RemoveAt(brushstrokStartID);
            RedrawAll();
        }
        
        private void AdjustBrushStrokeIDs(int brushstrokStartID, int amountToRemove)
        {
            Debug.Log($"Adjusting brushstroke start and end IDs");
            for (int i = brushstrokStartID + 1; i < brushStrokesID.Count; i++)
            {
                BrushStrokeID brushStrokeID = brushStrokesID[brushstrokStartID];
                brushStrokeID.startID -= amountToRemove;
                brushStrokeID.endID -= amountToRemove;
            }
        }


        public void RedrawAll()
        {
            for (int i = 0; i < brushStrokesID.Count; i++)
            {
                RedrawStroke(i);
            }
        }

        public void AddBrushDraw(BrushStroke brushStroke)
        {
            brushStrokes.Add(brushStroke);
        }

        public void FinishedStroke(Vector4 collisionBox, PaintType paintType)
        {
            brushStrokesID.Add(new BrushStrokeID(lastBrushDrawID, brushDrawID, collisionBox, paintType));
        }
        
        private bool CheckCollision(Vector4 box1, Vector4 box2)
        {
            return box1.x <= box2.z && box1.z >= box2.x && box1.y <= box2.w && box1.w >= box2.y;
        }
    }

    public struct BrushStroke
    {
        private float lastPosX;
        private float lastPosY;
        private float currentPosX;
        private float currentPosY;
        public float strokeBrushSize;
        public float brushTime;
        public float lastTime;

        public BrushStroke(Vector2 lastPos, Vector2 currentPos, float strokeBrushSize, float brushTime, float lastTime)
        {
            lastPosX = lastPos.x;
            lastPosY = lastPos.y;
            currentPosX = currentPos.x;
            currentPosY = currentPos.y;
            this.strokeBrushSize = strokeBrushSize;
            this.brushTime = brushTime;
            this.lastTime = lastTime;
        }

        public Vector2 GetLastPos()
        {
            return new Vector2(lastPosX, lastPosY);
        }
        public Vector2 GetCurrentPos()
        {
            return new Vector2(currentPosX, currentPosY);
        }
    }

    public enum PaintType
    {
        PaintUnderOwnLine,
        PaintUnderEverything,
        PaintOverOwnLine,
        PaintOverEverything,
        Erase
    }

    public struct BrushStrokeID
    {
        public int startID;
        public int endID;
        private float collisionBoxX;
        private float collisionBoxY;
        private float collisionBoxZ;
        private float collisionBoxW;
        public PaintType paintType;

        public BrushStrokeID(int startID, int endID, Vector4 collisionBox, PaintType paintType)
        {
            this.startID = startID;
            this.endID = endID;
            this.paintType = paintType;
            collisionBoxX = collisionBox.x;
            collisionBoxY = collisionBox.y;
            collisionBoxZ = collisionBox.z;
            collisionBoxW = collisionBox.w;
        }

        public Vector4 GetCollisionBox()
        {
            return new Vector4(collisionBoxX, collisionBoxY, collisionBoxZ, collisionBoxW);
        }
    }
}