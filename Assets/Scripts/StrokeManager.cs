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
    private Material mat;
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
    private int currentID;
    private int lastID;
    private float lastTime;

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

        brushStrokes = new List<BrushStroke>();
        brushStrokesID = new List<BrushStrokeID>();
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
                paintShader.SetTexture(kernelID, "Result", rt);

                paintShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);

                brushStrokes.Add(new BrushStroke(lastCursorPos, cursorPos, startPos, brushSize, time));
                lastCursorPos = cursorPos;
                currentID++;
            }
        }
        else
        {
            if (firstUse)
            {
                Debug.Log($"{lastID} {currentID}");
                brushStrokesID.Add(new BrushStrokeID(lastID, currentID, lastTime, time));
                firstUse = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log($"test");
            BrushStrokeID strokeID = brushStrokesID[0];
            Redraw(strokeID.startID, strokeID.endID, 0.5f, 1);
        }
    }

    private void Redraw(int startID, int endID, float startTime, float endTime)
    {
        int amountStrokes = endID - startID;
        for (int i = startID; i < endID; i++)
        {
            BrushStroke stroke = brushStrokes[i];
            
            threadGroupSize.x = Mathf.CeilToInt((math.abs(stroke.lastPos.x - stroke.currentPos.x) + stroke.brushSize * 2) / threadGroupSizeOut.x);
            threadGroupSize.y = Mathf.CeilToInt((math.abs(stroke.lastPos.y - stroke.currentPos.y) + stroke.brushSize * 2) / threadGroupSizeOut.y);

            float idPercentage = ExtensionMethods.Remap(i, startID, endID, 0, 1);
            float currentTime = (endTime - startTime) * idPercentage + startTime;

            paintShader.SetVector("cursorPos", stroke.currentPos);
            paintShader.SetVector("lastCursorPos", stroke.lastPos);
            paintShader.SetVector("startPos", stroke.startPos);
            paintShader.SetFloat("brushSize", stroke.brushSize);
            paintShader.SetFloat("timeColor",  currentTime);
            paintShader.SetTexture(kernelID, "Result", rt);

            paintShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
        }
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

    public BrushStrokeID(int startID, int endID, float startTime, float endTime)
    {
        this.startID = startID;
        this.endID = endID;
        this.startTime = startTime;
        this.endTime = endTime;
    }
}

public static class ExtensionMethods {
    public static float Remap (this float value, float from1, float to1, float from2, float to2) {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
   
}
