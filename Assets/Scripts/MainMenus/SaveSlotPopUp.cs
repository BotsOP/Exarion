using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DataPersistence;
using MainMenus;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SaveSlotPopUp : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private TMP_Text projectNameText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text timeSpentText;
    [SerializeField] private RawImage displayImage;
    [SerializeField] private RawImage showcaseImage;
    [SerializeField] private Material showcaseMat;
    [SerializeField] private float timeIncrease = 0.01f;
    [SerializeField] private ConfirmationPopupMenu confirmationPopupMenu;

    private ToolData currentToolData;
    private SaveSlot saveSlot;
    private float time;

    public void UpdateSaveSlot(SaveSlot _saveSlot)
    {
        currentToolData = _saveSlot.toolData;
        saveSlot = _saveSlot;
        time = 0;
        gameObject.SetActive(true);
        DataPersistenceManager.instance.ChangeSelectedProfileId(currentToolData.projectName);
        projectNameText.text = currentToolData.projectName;
        dateText.text = "Last save: " + DateTime.FromBinary(currentToolData.lastUpdated).ToString(CultureInfo.InvariantCulture);
        displayImage.texture = _saveSlot.displayTexture;
        showcaseImage.texture = _saveSlot.displayTexture;
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

    public void Delete()
    {
        confirmationPopupMenu.ActivateMenu("This will delete the project and is irreversible",
           // 'yes'
           () => {
               Destroy(saveSlot.gameObject);
               DataPersistenceManager.instance.DeleteProfileData(currentToolData.projectName);
               gameObject.SetActive(false);
           },
           // 'cancel'
           () => {
               
           }
        );
    }

    public void Cancel()
    {
        gameObject.SetActive(false);
    }
}
