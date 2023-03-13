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
        public List<BrushStroke> BrushStrokes
        {
            get {
                //Debug.Log($"needed brushStrokes");
                return brushStrokes;
            }
            set {
                brushStrokes = value;
            }
        }
        
        public int brushDrawID => BrushStrokes.Count;
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
        private int paintKernelID;
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

            paintKernelID = paintShader.FindKernel("Paint");
            eraseKernelID = paintShader.FindKernel("Erase");
        
            paintShader.GetKernelThreadGroupSizes(paintKernelID, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
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

            Color idColor = new Color(-1, -1, -1);
            
            Graphics.SetRenderTarget(rt);
            GL.Clear(false, true, Color.white);
            Graphics.SetRenderTarget(rtID);
            GL.Clear(false, true, idColor);
            Graphics.SetRenderTarget(null);

            threadGroupSize.x = Mathf.CeilToInt(imageWidth / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt(imageHeight / threadGroupSizeOut.y);
        }
    
        public void Draw(Vector2 lastPos, Vector2 currentPos, float strokeBrushSize, float lastTime, float brushTime, bool firstStroke, int strokeID)
        {
            threadGroupSize.x = Mathf.CeilToInt((math.abs(lastPos.x - currentPos.x) + strokeBrushSize * 2) / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt((math.abs(lastPos.y - currentPos.y) + strokeBrushSize * 2) / threadGroupSizeOut.y);
        
            Vector2 startPos = GetStartPos(lastPos, currentPos, strokeBrushSize);

            paintShader.SetBool("_FirstStroke", firstStroke);
            paintShader.SetVector("_CursorPos", currentPos);
            paintShader.SetVector("_LastCursorPos", lastPos);
            paintShader.SetVector("_StartPos", startPos);
            paintShader.SetFloat("_BrushSize", strokeBrushSize);
            paintShader.SetFloat("_TimeColor", brushTime);
            paintShader.SetFloat("_PreviousTimeColor", lastTime);
            paintShader.SetInt("_StrokeID", strokeID);
            paintShader.SetTexture(paintKernelID, "_IdTex", rtID);
            paintShader.SetTexture(paintKernelID, "_Result", rt);

            paintShader.Dispatch(paintKernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
        }
        
        public void Erase(Vector2 lastPos, Vector2 currentPos, float strokeBrushSize)
        {
            threadGroupSize.x = Mathf.CeilToInt((math.abs(lastPos.x - currentPos.x) + strokeBrushSize * 2) / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt((math.abs(lastPos.y - currentPos.y) + strokeBrushSize * 2) / threadGroupSizeOut.y);
        
            Vector2 startPos = GetStartPos(lastPos, currentPos, strokeBrushSize);

            paintShader.SetVector("_CursorPos", currentPos);
            paintShader.SetVector("_LastCursorPos", lastPos);
            paintShader.SetVector("_StartPos", startPos);
            paintShader.SetFloat("_BrushSize", strokeBrushSize + 1);
            paintShader.SetTexture(eraseKernelID, "_IdTex", rtID);
            paintShader.SetTexture(eraseKernelID, "_Result", rt);

            paintShader.Dispatch(eraseKernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
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
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = BrushStrokes[i];
        
                Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, stroke.lastTime, stroke.brushTime, firstLoop, newStrokeID);

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
        
            for (int i = startID; i < endID; i++)
            {
                BrushStroke stroke = BrushStrokes[i];
        
                float currentTime = stroke.brushTime;
                if (startTime >= 0 && endTime >= 0)
                {
                    float idPercentage = ExtensionMethods.Remap(i, startID, endID, 0, 1);
                    currentTime = (endTime - startTime) * idPercentage + startTime;
                }

                stroke.brushTime = currentTime;
                stroke.lastTime = previousTime;

                BrushStrokes[i] = stroke;
            
                Draw(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize, stroke.lastTime, stroke.brushTime, firstLoop, newStrokeID);

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
                BrushStroke stroke = BrushStrokes[i];
        
                Erase(stroke.GetLastPos(), stroke.GetCurrentPos(), stroke.strokeBrushSize);
            }

            int amountToRemove = endID - startID;
            if (brushstrokStartID + 1 < brushStrokesID.Count)
            {
                AdjustBrushStrokeIDs(brushstrokStartID, amountToRemove);
            }

            if (startID > 0)
            {
                BrushStrokes.RemoveRange(startID - 1, amountToRemove + 1);
            }
            else
            {
                BrushStrokes.Clear();
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

        public void FinishedStroke(Vector4 collisionBox)
        {
            brushStrokesID.Add(new BrushStrokeID(lastBrushDrawID, brushDrawID, collisionBox));
        }
    }

    public struct BrushStroke
    {
        public float lastPosX;
        public float lastPosY;
        public float currentPosX;
        public float currentPosY;
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

        // public Vector2 GetStartPos()
        // {
        //     float lowestX = (lastPos.x < currentPos.x ? lastPos.x : currentPos.x) - brushTime;
        //     float lowestY = (lastPos.y < currentPos.y ? lastPos.y : currentPos.y) - brushTime;
        //     return new Vector2(lowestX, lowestY);
        // }
    }

    public struct BrushStrokeID
    {
        public int startID;
        public int endID;
        //public Vector4 box;

        public BrushStrokeID(int startID, int endID, Vector4 box)
        {
            this.startID = startID;
            this.endID = endID;
            //this.box = box;
        }
    }
}