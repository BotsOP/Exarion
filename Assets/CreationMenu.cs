using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using DataPersistence;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CreationMenu : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] 
    private GameObject projectExistsText;

    [Header("Input")]
    [SerializeField]
    private TMP_InputField projectName;

    private ToolData toolData;

    public void ResetprojectExistsText()
    {
        projectExistsText.SetActive(false);
    }

    public void OnCreate()
    {
        bool isProjectNameTaken = DataPersistenceManager.instance.IsProfileIDTaken(projectName.text);

        if (isProjectNameTaken)
        {
            projectExistsText.SetActive(true);
            return;
        }
        
        toolData = new ToolData
        {
            lastUpdated = System.DateTime.Now.ToBinary(),
            projectName = projectName.text,
        };

        DataPersistenceManager.instance.ChangeSelectedProfileId(projectName.text);
        DataPersistenceManager.instance.toolData = toolData;
        DataPersistenceManager.instance.SaveTool();
        SceneManager.LoadSceneAsync("DrawScene");
    }

    public void ActivateMenu()
    {
        gameObject.SetActive(true);
    }

    public void DeactivateMenu()
    {
        gameObject.SetActive(false);
    }
}
