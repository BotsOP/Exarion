using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDataPersistence
{
    void LoadData(ToolData data);
    void SaveData(ToolData data);
}
