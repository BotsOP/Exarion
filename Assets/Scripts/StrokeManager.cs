using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class StrokeManager : MonoBehaviour
{
    [SerializeField]
    private ComputeShader paintShader;
    [SerializeField]
    private Camera cam;
    [SerializeField]
    private RenderTexture rt;
    [SerializeField]
    private RenderTexture strokeIDTex;
    [SerializeField]
    private Material mat;
    [SerializeField]
    private Material mat2;
    [SerializeField]
    private int imageWidth;
    [SerializeField]
    private int imageHeight;
    [SerializeField]
    private int brushSize;
    public Transform ball1;
    public Transform ball2;
    
    private Vector3 lastPos;
    private int kernelID;
    private Vector2 threadGroupSizeOut;
    private Vector2 threadGroupSize;
    private Vector2 lastCursorPos;
    private bool firstUse;
    private List<BrushStroke> brushStrokes;
    private List<BrushStrokeID> brushStrokesID;
    private int currentID = 1;
    private int lastID;
    private float lastTime;
    private Vector4 tempBox;

    private float time => Time.time / 10;

    void Start()
    {
        kernelID = 0;
        paintShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        threadGroupSizeOut.x = threadGroupSizeX;
        threadGroupSizeOut.y = threadGroupSizeY;

        rt = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        rt.enableRandomWrite = true;
        mat.SetTexture("_MainTex", rt);
        strokeIDTex = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        strokeIDTex.enableRandomWrite = true;
        mat2.SetTexture("_MainTex", strokeIDTex);

        brushStrokes = new List<BrushStroke>();
        brushStrokesID = new List<BrushStrokeID>();
        //brushStrokesID.Add(new BrushStrokeID(0, currentID, 0, 0, tempBox));

        tempBox = ResetTempBox(tempBox);
    }

    private void OnDisable()
    {
        strokeIDTex.Release();
        strokeIDTex = null;
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
                if (!firstUse)
                {
                    lastID = currentID;
                    lastTime = time;
                    lastCursorPos = cursorPos;
                    firstUse = true;
                }
                
                threadGroupSize.x = Mathf.CeilToInt((math.abs(lastCursorPos.x - cursorPos.x) + brushSize * 2) / threadGroupSizeOut.x);
                threadGroupSize.y = Mathf.CeilToInt((math.abs(lastCursorPos.y - cursorPos.y) + brushSize * 2) / threadGroupSizeOut.y);

                float lowestX = (cursorPos.x < lastCursorPos.x ? cursorPos.x : lastCursorPos.x) - brushSize;
                float lowestY = (cursorPos.y < lastCursorPos.y ? cursorPos.y : lastCursorPos.y) - brushSize;
                Vector2 startPos = new Vector2(lowestX, lowestY);

                paintShader.SetVector("cursorPos", cursorPos);
                paintShader.SetVector("lastCursorPos", lastCursorPos);
                paintShader.SetVector("startPos", startPos);
                paintShader.SetFloat("brushSize", brushSize);
                paintShader.SetFloat("timeColor", time);
                paintShader.SetFloat("strokeID", lastID);
                paintShader.SetTexture(kernelID, "result", rt);
                paintShader.SetTexture(kernelID, "strokeIDs", strokeIDTex);

                paintShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);

                brushStrokes.Add(new BrushStroke(lastCursorPos, cursorPos, startPos, brushSize, time));
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
            if (firstUse)
            {
                brushStrokesID.Add(new BrushStrokeID(lastID, currentID, lastTime, time, tempBox));
                tempBox = ResetTempBox(tempBox);
                firstUse = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            Redraw(0, 0.5f, 1);
        }
    }

    private void Redraw(int brushstrokStartID, float startTime, float endTime)
    {
        for (int i = brushstrokStartID; i < brushStrokesID.Count; i++)
        {
            Debug.Log($"{brushStrokesID.Count} {i}");
            BrushStrokeID strokeID = brushStrokesID[i];
            int startID = strokeID.startID;
            int endID = strokeID.endID;
            
            for (int j = startID; j < endID - 1; j++)
            {
                Debug.Log($"{brushStrokes.Count} {j}");
                BrushStroke stroke = brushStrokes[j];
            
                threadGroupSize.x = Mathf.CeilToInt((math.abs(stroke.lastPos.x - stroke.currentPos.x) + stroke.brushSize * 2) / threadGroupSizeOut.x);
                threadGroupSize.y = Mathf.CeilToInt((math.abs(stroke.lastPos.y - stroke.currentPos.y) + stroke.brushSize * 2) / threadGroupSizeOut.y);

                float currentTime = stroke.time;
                if (i == 0)
                {
                    float idPercentage = ExtensionMethods.Remap(j, startID, endID, 0, 1);
                    currentTime = (endTime - startTime) * idPercentage + startTime;
                }

                paintShader.SetVector("cursorPos", stroke.currentPos);
                paintShader.SetVector("lastCursorPos", stroke.lastPos);
                paintShader.SetVector("startPos", stroke.startPos);
                paintShader.SetFloat("brushSize", stroke.brushSize);
                paintShader.SetFloat("timeColor",  currentTime);
                paintShader.SetFloat("strokeID", startID);
                paintShader.SetTexture(kernelID, "result", rt);
                paintShader.SetTexture(kernelID, "strokeIDs", strokeIDTex);

                paintShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
            }
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
    
    private bool CheckCollision(Vector4 box1, Vector4 box2)
    {
        return box1.x <= box2.z && box1.z >= box2.x && box1.y <= box2.w && box1.w >= box2.y;
    }
}


struct BrushStroke
{
    public Vector2 lastPos;
    public Vector2 currentPos;
    public Vector2 startPos;
    public float brushSize;
    public float time;

    public BrushStroke(Vector2 lastPos, Vector2 currentPos, Vector2 startPos, float brushSize, float time)
    {
        this.lastPos = lastPos;
        this.currentPos = currentPos;
        this.startPos = startPos;
        this.brushSize = brushSize;
        this.time = time;
    }
}

struct BrushStrokeID
{
    public int startID;
    public int endID;
    public float startTime;
    public float endTime;
    public Vector4 box;

    public BrushStrokeID(int startID, int endID, float startTime, float endTime, Vector4 box)
    {
        this.startID = startID;
        this.endID = endID;
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
