using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;
using SFB;
using TMPro;
using UnityEngine.Networking;
using Dummiesman;
using UnityEngine.Serialization; //Load OBJ Model

public class ImportOBJ : MonoBehaviour
{
    [SerializeField] private GameObject modelHolder; //Load OBJ Model
    [SerializeField] private Material drawMat;
    [SerializeField] private Material displayMat;

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
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", "obj", false);
        if (paths.Length > 0)
        {
            StartCoroutine(OutputRoutineOpen(new System.Uri(paths[0]).AbsoluteUri));
        }
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
            MeshRenderer meshRenderer = model.GetComponent<MeshRenderer>();
            meshRenderer.material = drawMat;

            GameObject modelDisplay = Instantiate(model, modelHolder.transform);
            meshRenderer = modelDisplay.GetComponent<MeshRenderer>();
            meshRenderer.material = displayMat;
            modelDisplay.layer = LayerMask.NameToLayer("display");

            //FitOnScreen();
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

    private void FitOnScreen()
    {
        Bounds bound = GetBound(modelHolder);
        Vector3 boundSize = bound.size;
        float diagonal = Mathf.Sqrt((boundSize.x * boundSize.x) + (boundSize.y * boundSize.y) + (boundSize.z * boundSize.z)); //Get box diagonal
        Camera.main.orthographicSize = diagonal / 2.0f;
        Camera.main.transform.position = bound.center;
    }

}