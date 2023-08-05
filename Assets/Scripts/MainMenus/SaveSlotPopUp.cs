using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DataPersistence;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveSlotPopUp : MonoBehaviour
{
    [NonSerialized] private ToolData currentToolData;

    [Header("Settings")]
    [SerializeField] private TMP_Text projectNameText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text timeSpentText;
    [SerializeField] private RawImage displayImage;
    [SerializeField] private RawImage showcaseImage;
    [SerializeField] private Material showcaseMat;
    [SerializeField] private float timeIncrease = 0.01f;

    private float time;

    public void UpdateSaveSlot(ToolData _data, Texture2D _displayImage)
    {
        time = 0;
        gameObject.SetActive(true);
        DataPersistenceManager.instance.ChangeSelectedProfileId(_data.projectName);
        currentToolData = _data;
        projectNameText.text = _data.projectName;
        dateText.text = "Last save: " + DateTime.FromBinary(_data.lastUpdated).ToString(CultureInfo.InvariantCulture);
        displayImage.texture = _displayImage;
        showcaseImage.texture = _displayImage;
    }

    private void Update()
    {
        time = (time + timeIncrease) % 1;
        showcaseMat.SetFloat("_CustomTime", time);
    }

    public void LoadProject()
    {
        DataPersistenceManager.instance.SaveTool();
        SceneManager.LoadSceneAsync("DrawScene");
    }

    public void Cancel()
    {
        gameObject.SetActive(false);
    }
}
