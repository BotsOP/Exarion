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
    public float timeSpentMinutes;

    //Data for us
    //Variables per project
    public string projectName;
    public List<CondensedClip> timelineClips;
    public int extraTimelineBars;

    public byte[] overlayImg;
    public byte[] displayImg;

    public int imageWidth;
    public int imageHeight;

    // the values defined in this constructor will be the default values
    // the tool starts with when there's no data to load
    public ToolData()
    {
        imageWidth = 1024;
        imageHeight = 1024;
        projectName = "";
        overlayImg = null;
        displayImg = null;
        timelineClips = new List<CondensedClip>();
    }

    public string GetProjectName() 
    {
        return projectName;
    }
}

