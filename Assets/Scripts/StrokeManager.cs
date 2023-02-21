using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Random = System.Random;

public class StrokeManager : MonoBehaviour
{
    [SerializeField] private ComputeShader paintShader;
    [SerializeField] private Camera cam;
    [SerializeField] private Material drawingMat;
    [SerializeField] private Material displayMat;
    [SerializeField] private Material strokeIDMat;
    [SerializeField] private int imageWidth;
    [SerializeField] private int imageHeight;
    [SerializeField] private int brushSize;
    
    [SerializeField] private int brushStrokeIDToRedraw;
    [SerializeField] private float brushStartTime;
    [SerializeField] private float brushEndTime;
    public Transform ball1;
    public Transform ball2;
    
    private RenderTexture drawingRenderTexture;
    private RenderTexture strokeIDRenderTexture;
    private int kernelID;
    private Vector2 threadGroupSizeOut;
    private Vector2 threadGroupSize;
    private Vector2 lastCursorPos;
    private bool firstUse = true;
    private List<BrushStroke> brushStrokes;
    private List<BrushStrokeID> brushStrokesID;
    private int currentID = 1;
    private int lastID;
    private float strokeStartTime;
    private float cachedTime;
    private float strokeID;
    private Vector4 tempBox;

    private ComputeBuffer debugBuffer;
    private Random random;

    private float time => Time.time / 10;

    void OnEnable()
    {
        random = new Random();
        
        kernelID = 0;
        paintShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        threadGroupSizeOut.x = threadGroupSizeX;
        threadGroupSizeOut.y = threadGroupSizeY;

        drawingRenderTexture = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        drawingRenderTexture.enableRandomWrite = true;
        
        strokeIDRenderTexture = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        strokeIDRenderTexture.enableRandomWrite = true;
        
        threadGroupSize.x = Mathf.CeilToInt(imageWidth / threadGroupSizeOut.x);
        threadGroupSize.y = Mathf.CeilToInt(imageHeight / threadGroupSizeOut.y);
        
        paintShader.SetTexture(1, "result", drawingRenderTexture);
        paintShader.Dispatch(1, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
        
        drawingMat.SetTexture("_MainTex", drawingRenderTexture);
        displayMat.SetTexture("_MainTex", drawingRenderTexture);
        strokeIDMat.SetTexture("_MainTex", strokeIDRenderTexture);

        brushStrokes = new List<BrushStroke>();
        brushStrokesID = new List<BrushStrokeID>();

        //debugBuffer = new ComputeBuffer(100000, sizeof(float) * 4, ComputeBufferType.Append);
        //brushStrokesID.Add(new BrushStrokeID(0, currentID, 0, 0, tempBox));

        tempBox = ResetTempBox(tempBox);
    }

    private void OnDisable()
    {
        drawingRenderTexture.Release();
        drawingRenderTexture = null;
    }

    void Update()
    {
        if(Input.GetMouseButton(0))
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                Vector2 cursorPos = new Vector2(hit.point.x * imageWidth, hit.point.y * imageHeight);

                if (firstUse)
                {
                    lastID = currentID;
                    strokeStartTime = time;
                    lastCursorPos = cursorPos;
                    strokeID = NextFloat(random);
                    firstUse = false;
                }

                bool firstStroke = lastID == currentID;

                DrawStroke(lastCursorPos, cursorPos, brushSize, cachedTime, time, strokeID, firstStroke);

                brushStrokes.Add(new BrushStroke(lastCursorPos, cursorPos, brushSize, time, cachedTime));
                lastCursorPos = cursorPos;
                currentID++;
                
                if (tempBox.x > cursorPos.x) { tempBox.x = cursorPos.x; }
                if (tempBox.y > cursorPos.y) { tempBox.y = cursorPos.y; }
                if (tempBox.z < cursorPos.x) { tempBox.z = cursorPos.x; }
                if (tempBox.w < cursorPos.y) { tempBox.w = cursorPos.y; }
                // ball1.position = new Vector3(tempBox.x / imageWidth, tempBox.y / imageHeight, 0);
                // ball2.position = new Vector3(tempBox.z / imageWidth, tempBox.w / imageHeight, 0);
            }
        }
        else
        {
            //runs once after mouse is not being clicked anymore
            if (!firstUse)
            {
                brushStrokesID.Add(new BrushStrokeID(lastID, currentID, strokeID, strokeStartTime, time, tempBox));
                tempBox = ResetTempBox(tempBox);
                firstUse = true;
            }
        }

        cachedTime = time;

        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log($"hoi");
            Redraw(brushStrokeIDToRedraw, brushStartTime, brushEndTime);
        }
    }

    private void DrawStroke(Vector2 lastPos, Vector2 currentPos, float strokeBrushSize, float lastTime, float brushTime, float brushStrokeID, bool firstStroke, bool drawOverOwnLine = false)
    {
        Debug.Log($"{brushStrokeID}");
        threadGroupSize.x = Mathf.CeilToInt((math.abs(lastPos.x - currentPos.x) + strokeBrushSize * 2) / threadGroupSizeOut.x);
        threadGroupSize.y = Mathf.CeilToInt((math.abs(lastPos.y - currentPos.y) + strokeBrushSize * 2) / threadGroupSizeOut.y);
        
        Vector2 startPos = GetStartPos(lastPos, currentPos);

        paintShader.SetBool("firstStroke", firstStroke);
        paintShader.SetBool("drawOverOwnLine", drawOverOwnLine);
        paintShader.SetVector("cursorPos", currentPos);
        paintShader.SetVector("lastCursorPos", lastPos);
        paintShader.SetVector("startPos", startPos);
        paintShader.SetFloat("brushSize", strokeBrushSize);
        paintShader.SetFloat("timeColor", brushTime);
        paintShader.SetFloat("previousTimeColor", lastTime);
        paintShader.SetFloat("strokeID", brushStrokeID);
        paintShader.SetTexture(kernelID, "result", drawingRenderTexture);
        paintShader.SetTexture(kernelID, "strokeIDTexture", strokeIDRenderTexture);

        paintShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
    }

    private void Redraw(int brushstrokStartID, float startTime, float endTime)
    {
        RedrawStroke(brushstrokStartID, startTime, endTime);
        
        int amountStrokesToRun = brushStrokesID.Count - brushstrokStartID;
        for (int i = brushstrokStartID + 1; i < amountStrokesToRun; i++)
        {
            RedrawStroke(i);
        }
    }

    private Vector2 GetStartPos(Vector2 a, Vector2 b)
    {
        float lowestX = (a.x < b.x ? a.x : b.x) - brushSize;
        float lowestY = (a.y < b.y ? a.y : b.y) - brushSize;
        return new Vector2(lowestX, lowestY);
    }

    private void RedrawStroke(int brushstrokStartID)
    {
        BrushStrokeID strokeID = brushStrokesID[brushstrokStartID];
        float brushStrokeID = strokeID.strokeID;
        int startID = strokeID.startID;
        int endID = strokeID.endID;
        bool firstLoop = true;
        for (int i = startID; i < endID - 1; i++)
        {
            BrushStroke stroke = brushStrokes[i];
        
            DrawStroke(stroke.lastPos, stroke.currentPos, stroke.strokeBrushSize, stroke.lastTime, stroke.brushTime, brushStrokeID, firstLoop);

            firstLoop = false;
        }
    }
    
    private void RedrawStroke(int brushstrokStartID, float startTime, float endTime)
    {
        BrushStrokeID strokeID = brushStrokesID[brushstrokStartID];
        float brushStrokeID = 1;
        int startID = strokeID.startID;
        int endID = strokeID.endID;
        float previousTime = startTime;
        bool firstLoop = true;
        
        for (int i = startID; i < endID - 1; i++)
        {
            BrushStroke stroke = brushStrokes[i];
        
            float currentTime = stroke.brushTime;
            if (startTime >= 0 && endTime >= 0)
            {
                float idPercentage = ExtensionMethods.Remap(i, startID, endID, 0, 1);
                currentTime = (endTime - startTime) * idPercentage + startTime;
                Debug.Log($"{currentTime} {previousTime}");
            }

            stroke.brushTime = currentTime;
            stroke.lastTime = previousTime;

            brushStrokes[i] = stroke;
            
            DrawStroke(stroke.lastPos, stroke.currentPos, stroke.strokeBrushSize, stroke.lastTime, stroke.brushTime, brushStrokeID, firstLoop);

            firstLoop = false;
            previousTime = currentTime;
        }
    }

    private Vector4 ResetTempBox(Vector4 box)
    {
        box.x = imageWidth;
        box.y = imageHeight;
        box.z = 0;
        box.w = 0;
        return box;
    }
    
    public float NextFloat(Random random)
    {
        double mantissa = (random.NextDouble() * 2.0) - 1.0;
        // choose -149 instead of -126 to also generate subnormal floats (*)
        double exponent = Math.Pow(2.0, random.Next(-126, 128));
        return (float)(mantissa * exponent);
    }
    
    private bool CheckCollision(Vector4 box1, Vector4 box2)
    {
        return box1.x <= box2.z && box1.z >= box2.x && box1.y <= box2.w && box1.w >= box2.y;
    }
}


struct BrushStroke
{
    public Vector2 lastPos;
    public Vector2 currentPos;
    public float strokeBrushSize;
    public float brushTime;
    public float lastTime;

    public BrushStroke(Vector2 lastPos, Vector2 currentPos, float strokeBrushSize, float brushTime, float lastTime)
    {
        this.lastPos = lastPos;
        this.currentPos = currentPos;
        this.strokeBrushSize = strokeBrushSize;
        this.brushTime = brushTime;
        this.lastTime = lastTime;
    }

    public Vector2 GetStartPos()
    {
        float lowestX = (lastPos.x < currentPos.x ? lastPos.x : currentPos.x) - brushTime;
        float lowestY = (lastPos.y < currentPos.y ? lastPos.y : currentPos.y) - brushTime;
        return new Vector2(lowestX, lowestY);
    }
}

struct BrushStrokeID
{
    public int startID;
    public int endID;
    public float strokeID;
    public float startTime;
    public float endTime;
    public Vector4 box;

    public BrushStrokeID(int startID, int endID, float strokeID, float startTime, float endTime, Vector4 box)
    {
        this.startID = startID;
        this.endID = endID;
        this.strokeID = strokeID;
        this.startTime = startTime;
        this.endTime = endTime;
        this.box = box;
    }
}

public static class ExtensionMethods {
    public static float Remap (this float value, float from1, float to1, float from2, float to2) {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}
