using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crosstales.FB;
using DataPersistence.Data;
using Drawing;
using UnityEngine.Networking;
using Dummiesman;
using Managers;
using Unity.VisualScripting;
using EventType = Managers.EventType;

public class ImportOBJ : MonoBehaviour, IDataPersistence
{
    [SerializeField] private float modelScale = 5;
    private GameObject modelHolder;
    private Mesh mesh;
    
    private string[] extensions = { "obj" };

#if UNITY_WEBGL && !UNITY_EDITOR
    // WebGL
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

    public void OnClickOpen() {
        UploadFile(gameObject.name, "OnFileUpload", ".obj", false);
    }

    // Called from browser
    public void OnFileUpload(string url) {
        StartCoroutine(OutputRoutineOpen(url));
    }
#else
    // Standalone platforms & editor
    public void OnClickOpen()
    {
        String path = FileBrowser.Instance.OpenSingleFile("Open file", "", "", extensions);
        StartCoroutine(OutputRoutineOpen(path));
    }
#endif

    private IEnumerator OutputRoutineOpen(string url)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("WWW ERROR: " + www.error);
        }
        else
        {
            //textMeshPro.text = www.downloadHandler.text;

            //Load OBJ Model
            MemoryStream textStream = new MemoryStream(Encoding.UTF8.GetBytes(www.downloadHandler.text));
            if (modelHolder != null)
            {
                Destroy(modelHolder);
            }
            modelHolder = new OBJLoader().Load(textStream);
            GameObject model = modelHolder.transform.GetChild(0).gameObject;
            model.AddComponent<MeshCollider>();
            MeshRenderer drawingRenderer = model.GetComponent<MeshRenderer>();
            mesh = model.GetComponent<MeshFilter>().sharedMesh;
            
            GameObject modelDisplay = Instantiate(model, modelHolder.transform);
            MeshRenderer displayRenderer = modelDisplay.GetComponent<MeshRenderer>();
            modelDisplay.layer = LayerMask.NameToLayer("display");
            EventSystem<Renderer, Renderer>.RaiseEvent(EventType.CHANGED_MODEL, drawingRenderer, displayRenderer);
            
            FitOnScreen(model, modelDisplay);
        }
    }

    private Bounds GetBound(GameObject gameObj)
    {
        Bounds bound = new Bounds(gameObj.transform.position, Vector3.zero);
        var rList = gameObj.GetComponentsInChildren(typeof(Renderer));
        foreach (Renderer r in rList)
        {
            bound.Encapsulate(r.bounds);
        }
        return bound;
    }

    private void FitOnScreen(GameObject model1, GameObject model2)
    {
        Bounds bound = GetBound(modelHolder);
        Vector3 boundSize = bound.size;
        float diagonal = Mathf.Sqrt((boundSize.x * boundSize.x) + (boundSize.y * boundSize.y) + (boundSize.z * boundSize.z)); //Get box diagonal
        float scale = 1 / (diagonal / modelScale);
        model1.transform.localScale = new Vector3(scale, scale, scale);
        model2.transform.localScale = new Vector3(scale, scale, scale);

        model1.transform.position -= bound.center * scale;
        model2.transform.position -= bound.center * scale;
    }

    private void SpawnMesh()
    {
        modelHolder = new GameObject();
        modelHolder.name = "ModelHolder";
        
        GameObject model = new GameObject();
        model.name = "DrawModel";
        model.transform.SetParent(modelHolder.transform);
        MeshFilter meshFilterModel = model.AddComponent<MeshFilter>();
        meshFilterModel.sharedMesh = mesh;
        MeshRenderer drawingRenderer = model.AddComponent<MeshRenderer>();
        model.AddComponent<MeshCollider>();
        
        GameObject modelDisplay = Instantiate(model, modelHolder.transform);
        modelDisplay.name = "ModelDisplay";
        MeshRenderer displayRenderer = modelDisplay.GetComponent<MeshRenderer>();
        modelDisplay.layer = LayerMask.NameToLayer("display");
        StartCoroutine(WaitAndPrint(drawingRenderer, displayRenderer));
        
        FitOnScreen(model, modelDisplay);
    }
    
    private IEnumerator WaitAndPrint(Renderer _drawingRenderer, Renderer _displayRenderer)
    {
        yield return new WaitForEndOfFrameUnit();
        yield return new WaitForEndOfFrameUnit();
        EventSystem<Renderer, Renderer>.RaiseEvent(EventType.CHANGED_MODEL, _drawingRenderer, _displayRenderer);
    }

    public void LoadData(ToolData _data, ToolMetaData _metaData)
    {
        if (_data is ToolData3D)
        {
            ToolData3D toolData3D = (ToolData3D)_data;
            if (toolData3D.meshLoaded)
            {
                mesh = toolData3D.LoadMesh();
                SpawnMesh();
            }
        }
        else
        {
            Debug.LogError($"Data is not ToolData3D");
        }
    }

    public void SaveData(ToolData _data, ToolMetaData _metaData)
    {
        if (_data is ToolData3D)
        {
            ToolData3D toolData3D = (ToolData3D)_data;
            toolData3D.SaveMesh(mesh);
        }
        else
        {
            Debug.LogError($"Data is not ToolData3D");
        }
    }
}