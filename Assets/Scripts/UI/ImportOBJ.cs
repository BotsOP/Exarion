using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Crosstales.FB;
using UnityEngine.Networking;
using Dummiesman;
using Managers;
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

    public void LoadData(ToolData _data)
    {
        if (_data.projectType == ProjectType.PROJECT3D)
        {
            Debug.Log($"is 3d type");
        }
        if (_data is ToolData3D)
        {
            Debug.Log($"is 3d");
            ToolData3D toolData3D = (ToolData3D)_data;
            mesh = new Mesh();
            mesh.vertices = toolData3D.vertexPos.ToArray();
            mesh.normals = toolData3D.vertexNormal.ToArray();
            mesh.tangents = toolData3D.vertexTangents.ToArray();
            for (int i = 0; i < toolData3D.indices.Count; i++)
            {
                mesh.SetIndices(toolData3D.indices[i], MeshTopology.Triangles, i);
                mesh.SetUVs(i, toolData3D.uvs[i]);
            }
            mesh.RecalculateBounds();
        }
    }

    public void SaveData(ToolData _data)
    {
        ToolData3D toolData3D = (ToolData3D)_data;
        mesh.GetVertices(toolData3D.vertexPos);
        mesh.GetNormals(toolData3D.vertexNormal);
        mesh.GetTangents(toolData3D.vertexTangents);
        
        toolData3D.indices.Clear();
        toolData3D.uvs.Clear();
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            List<int> indices = new List<int>();
            mesh.GetIndices(indices, i);
            toolData3D.indices.Add(indices);
            
            List<Vector2> uvs = new List<Vector2>();
            mesh.GetUVs(i, uvs);
            toolData3D.uvs.Add(uvs);
        }
    }
}