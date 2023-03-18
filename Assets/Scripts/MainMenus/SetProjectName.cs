using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SetProjectName : MonoBehaviour, IDataPersistence
{
    public string projectName ="";
    public GameObject inputField;

    public void LoadData(ToolData _data)
    {
        this.projectName = _data.projectName;
    }

    public void SaveData(ToolData _data)
    {
        _data.projectName = this.projectName;
    }

    public void StoreProjectName()
    {
        projectName = inputField.GetComponent<TMP_Text>().text.ToString();

    }
}
