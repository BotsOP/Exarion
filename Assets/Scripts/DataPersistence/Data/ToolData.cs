using System.Collections;
using System.Collections.Generic;
using Drawing;
using UI;
using UnityEngine;

[System.Serializable]
public class ToolData
{
    //Data not for us
    public long lastUpdated;

    //Data for us
    //Variables per project
    public string projectName;
    public List<BrushStrokeID> brushStrokesID;
    public List<CondensedClip> timelineClips;

    public byte[] overlayImg;

    // the values defined in this constructor will be the default values
    // the tool starts with when there's no data to load
    public ToolData() 
    {
        projectName = "";
        overlayImg = null;
        brushStrokesID = new List<BrushStrokeID>();
        timelineClips = new List<CondensedClip>();
    }

    public string GetProjectName() 
    {
        return projectName;
    }
}

