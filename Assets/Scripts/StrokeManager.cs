using System.Collections;
using System.Collections.Generic;
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
    
    private int kernelID;
    private Vector2 threadGroupSize;

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
        RaycastHit hit;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out hit))
        {
            Vector2 cursorPos = new Vector2(hit.point.x * imageWidth, hit.point.y * imageHeight);
            paintShader.SetVector("cursorPos", cursorPos);
            paintShader.SetTexture(kernelID, "Result", rt);
            Debug.Log($"{cursorPos}");
            
            paintShader.Dispatch(kernelID, (int)threadGroupSize.x, (int)threadGroupSize.y, 1);
        }
    }
}
