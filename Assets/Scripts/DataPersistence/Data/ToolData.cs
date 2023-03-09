using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ToolData
{
    //Data not for us
    public long lastUpdated;

    //Data for us
    //Variables per project
    public string projectName;
    public int pixelateAmount;

    public byte[] imgBytes;

    // the values defined in this constructor will be the default values
    // the tool starts with when there's no data to load
    public ToolData() 
    {
        projectName = "";
        pixelateAmount = 0;
        imgBytes = null;
    }

    public string GetProjectName() 
    {
        return projectName;
    }
}
