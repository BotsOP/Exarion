using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetProjectName : MonoBehaviour, IDataPersistence
{
    public string projectName ="";
    public GameObject inputField;

    public void LoadData(ToolData data)
    {
        this.projectName = data.projectName;
    }

    public void SaveData(ToolData data)
    {
        data.projectName = this.projectName;
    }

    public void StoreProjectName()
    {
        projectName = inputField.GetComponent<TMP_Text>().text.ToString();

    }
}
