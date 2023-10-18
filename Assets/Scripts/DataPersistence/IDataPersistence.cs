using System.Collections;
using System.Collections.Generic;
using DataPersistence.Data;
using UnityEngine;

public interface IDataPersistence
{
    void LoadData(ToolData _data, ToolMetaData _metaData);
    void SaveData(ToolData _data, ToolMetaData _metaData);
}
