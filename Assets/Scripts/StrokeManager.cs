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
    private Vector2 threadGroupSize;
    private Vector2 lastCursorPos;
    private bool firstUse;

    void Start()
    {
        kernelID = 0;
        paintShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        threadGroupSize.x = Mathf.CeilToInt((float)(brushSize) / threadGroupSizeX);
        threadGroupSize.y = Mathf.CeilToInt((float)(brushSize) / threadGroupSizeY);

        rt = new CustomRenderTexture(imageWidth, imageHeight, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        rt.enableRandomWrite = true;
        mat.SetTexture("_MainTex", rt);
    }

    void Update()
    {
        if(Input.GetMouseButton(0))
        {
            RaycastHit hit;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                // ball2.position = lastPos;
                // ball1.position = hit.point;
                // lastPos = hit.point;
                Vector2 cursorPos = new Vector2(hit.point.x * imageWidth, hit.point.y * imageHeight);
                if (!firstUse)
                {
                    lastCursorPos = cursorPos;
                    firstUse = true;
                }
                
                paintShader.GetKernelThreadGroupSizes(kernelID, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
                threadGroupSize.x = Mathf.CeilToInt((math.abs(lastCursorPos.x - cursorPos.x) + brushSize * 2) / threadGroupSizeX);
                threadGroupSize.y = Mathf.CeilToInt((math.abs(lastCursorPos.y - cursorPos.y) + brushSize * 2) / threadGroupSizeY);

                float lowestX = (cursorPos.x < lastCursorPos.x ? cursorPos.x : lastCursorPos.x) - brushSize;
                float lowestY = (cursorPos.y < lastCursorPos.y ? cursorPos.y : lastCursorPos.y) - brushSize;
                Vector2 startPos = new Vector2(lowestX, lowestY);
                Vector2 endPos = new Vector2(startPos.x + (math.abs(lastCursorPos.x - cursorPos.x) + brushSize * 2), startPos.y + (math.abs(lastCursorPos.y - cursorPos.y) + brushSize * 2));
                ball1.position = new Vector3(startPos.x / imageWidth, startPos.y / imageHeight, 0);
                ball2.position = new Vector3(endPos.x / imageWidth, endPos.y / imageHeight, 0);
                
                paintShader.SetVector("cursorPos", cursorPos);
                paintShader.SetVector("lastCursorPos", lastCursorPos);
                paintShader.SetVector("startPos", startPos);
                paintShader.SetVector("endPos", endPos);
                paintShader.SetFloat("brushSize", brushSize);
                paintShader.SetTexture(kernelID, "Result", rt);

                paintShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);

                lastCursorPos = cursorPos;
            }
        }
    }
}
