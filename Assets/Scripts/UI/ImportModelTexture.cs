using System;
using System.Collections;
using System.Collections.Generic;
using Crosstales.FB;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using EventType = Managers.EventType;

public class ImportModelTexture : MonoBehaviour
{
    [Header("Import texture menu")]
    [SerializeField] private TMP_Dropdown dropdown;
    private Texture2D importedTexture;
    private byte[] imgData;
    private string[] extensions = { "png,jpeg,jpe,jfif,jpg" };

    public void OnEnable()
    {
        EventSystem<int>.Subscribe(EventType.UPDATE_SUBMESH_COUNT, UpdateDropdown);
    }
    
    public void OnDisable()
    {
        EventSystem<int>.Unsubscribe(EventType.UPDATE_SUBMESH_COUNT, UpdateDropdown);
    }

    private void UpdateDropdown(int _subMeshCount)
    {
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < _subMeshCount; i++)
        {
            options.Add(new TMP_Dropdown.OptionData("UV" + i));
        }

        dropdown.options = options;
    }
    
    public void FinishImporting()
    {
        int uvChannel = dropdown.value;
        EventSystem<int, Texture2D>.RaiseEvent(EventType.IMPORT_MODEL_TEXTURE, uvChannel, importedTexture);
    }
    
    public void OpenFileBrowser()
    {
        String path = FileBrowser.Instance.OpenSingleFile("Open file", "", "", extensions);
        StartCoroutine(LoadImage(path));
    }

    private IEnumerator LoadImage(string _path)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(_path))
        {
            yield return uwr.SendWebRequest();

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                var uwrTexture = DownloadHandlerTexture.GetContent(uwr);
                
                imgData = uwrTexture.EncodeToPNG();
                if (!LoadImage(out importedTexture))
                {
                    Debug.LogError($"Image is null or holds no data");
                }
            }
        }
    }

    private bool LoadImage(out Texture2D _texture)
    {
        _texture = null;
        if (imgData is null) return false;
        if (imgData.Length <= 0) return false;
        
        //Size doesnt matter ;)
        _texture = new Texture2D(1, 1);
        _texture.LoadImage(imgData);
        return true;
    }
}
