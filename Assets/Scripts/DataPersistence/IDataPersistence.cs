using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDataPersistence
{
    void LoadData(ToolData _data);
    void SaveData(ToolData _data);
}
