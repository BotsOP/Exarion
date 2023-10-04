using System;
using System.Collections;
using System.Collections.Generic;
using Crosstales.FB;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ImportImage : MonoBehaviour, IDataPersistence
{
    [SerializeField] private Material overlayMat;
    [SerializeField] private Material displayMat;
    [SerializeField] private Slider alphaSlider;
    [SerializeField] private GameObject overlayShowcase;
    [SerializeField] private GameObject overlaySettings;
    
    [HideInInspector] public byte[] imgData;
    
    private string[] extensions = { "jpg, jpeg, jpe, jfif, png" };
    
    public void OpenFileBrowser()
    {
        String path = FileBrowser.Instance.OpenSingleFile("Open file", "", "", extensions);
        StartCoroutine(LoadImage(path));
    }

    public void RemoveImage()
    {
        imgData = null;
        overlayMat.SetTexture("_MainTex", null);
        overlayShowcase.SetActive(false);
        displayMat.SetInt("_UseTexture", 0);
        overlaySettings.SetActive(false);
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
                
                overlayMat.SetTexture("_MainTex", uwrTexture);
                displayMat.SetTexture("_OverlayTex", uwrTexture);
                displayMat.SetInt("_UseTexture", 1);
                overlayShowcase.SetActive(true);
                overlaySettings.SetActive(true);
                imgData = uwrTexture.EncodeToPNG();
            }
        }
    }

    private void Start()
    {
        alphaSlider.onValueChanged.AddListener(delegate {overlayMat.SetFloat("_Alpha", alphaSlider.value); });
        overlayMat.SetFloat("_Alpha", alphaSlider.value);
        LoadImage(); 
    }

    private void LoadImage()
    {
        if (imgData is null) return;
        if (imgData.Length <= 0) return;
        
        //Size doesnt matter ;)
        Texture2D tex = new Texture2D(1, 1);
        Debug.Log(imgData.Length);
        tex.LoadImage(imgData);
        overlayShowcase.SetActive(true);
        overlaySettings.SetActive(true);
        overlayMat.SetTexture("_MainTex", tex);
        displayMat.SetTexture("_OverlayTex", tex);
        displayMat.SetInt("_UseTexture", 1);
    }

    public void LoadData(ToolData _data)
    {
        imgData = _data.overlayImg;
    }

    public void SaveData(ToolData _data)
    {
        _data.overlayImg = imgData;
    }
}
