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

    private SaveSlotSlim saveSlot;
    private float time;

    public void UpdateSaveSlot(SaveSlotSlim _saveSlot)
    {
        gameObject.SetActive(true);
        saveSlot = _saveSlot;
        DataPersistenceManager.instance.ChangeSelectedProfileId(_saveSlot.projectName);
        projectNameText.text = saveSlot.projectName;
    }

    public void LoadProject()
    {
        DataPersistenceManager.instance.SaveTool();
        ProjectType projectType = DataPersistenceManager.instance.LoadProject();
        switch (projectType)
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
               DataPersistenceManager.instance.DeleteProfileData(saveSlot.projectName);
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
