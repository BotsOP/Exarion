using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DataPersistence;
using DataPersistence.Data;
using MainMenus;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SaveSlotPopUp : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private TMP_Text projectNameText;
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text timeSpentText;
    [SerializeField] private RawImage displayImage;
    [SerializeField] private RawImage showcaseImage;
    [SerializeField] private Material drawMat;
    [SerializeField] private Material drawMat3D;
    [SerializeField] private Material displayMat;
    [SerializeField] private float timeIncrease = 0.01f;
    [SerializeField] private ConfirmationPopupMenu confirmationPopupMenu;

    private SaveSlot saveSlot;
    private ToolMetaData metaData;
    private float time;

    public void UpdateSaveSlot(SaveSlot _saveSlot)
    {
        metaData = _saveSlot.metaData;
        saveSlot = _saveSlot;
        time = 0;
        gameObject.SetActive(true);
        DataPersistenceManager.instance.ChangeSelectedProfileId(metaData.projectName);
        projectNameText.text = metaData.projectName;
        dateText.text = "Last save: " + DateTime.FromBinary(metaData.lastUpdated).ToString(CultureInfo.InvariantCulture);
        displayImage.texture = _saveSlot.displayTexture;
        showcaseImage.texture = _saveSlot.displayTexture;

        if (metaData.projectType == ProjectType.PROJECT2D)
        {
            displayImage.material = drawMat;
        }
        else if(metaData.projectType == ProjectType.PROJECT3D)
        {
            displayImage.material = drawMat3D;
        }
    }

    private void FixedUpdate()
    {
        time = (time + timeIncrease) % 1;
        displayMat.SetFloat("_CustomTime", time);
    }

    public void LoadProject()
    {
        switch (metaData.projectType)
        {
            case ProjectType.PROJECT2D:
                SceneManager.LoadSceneAsync("2DDrawScene");
                break;
            case ProjectType.PROJECT3D:
                SceneManager.LoadSceneAsync("3DDrawScene");
                break;
        }
    }

    public void Delete()
    {
        confirmationPopupMenu.ActivateMenu("This will delete the project and is irreversible",
            // 'yes'
            () => {
                Destroy(saveSlot.gameObject);
                DataPersistenceManager.instance.DeleteProfileData(metaData.projectName);
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
