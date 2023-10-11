using System.Collections;
using System.Collections.Generic;
using Drawing;
using UI;
using UnityEngine;

public enum ProjectType
{
    PROJECT2D,
    PROJECT3D,
    FAILED,
}

[System.Serializable]
public abstract class ToolData
{
    //Data not for us
    public long lastUpdated;
    public float timeSpentMinutes;

    //Variables per project
    public ProjectType projectType;
    public string projectName;
    public List<CondensedClip> timelineClips;
    public int extraTimelineBars;
    
    public int imageWidth;
    public int imageHeight;

    // the values defined in this constructor will be the default values
    // the tool starts with when there's no data to load
    public ToolData()
    {
        imageWidth = 1024;
        imageHeight = 1024;
        projectName = "";
        timelineClips = new List<CondensedClip>();
    }

    public string GetProjectName() 
    {
        return projectName;
    }
}

