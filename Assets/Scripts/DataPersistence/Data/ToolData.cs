using System.Collections;
using System.Collections.Generic;
using Drawing;
using UnityEngine;

[System.Serializable]
public class ToolData
{
    //Data not for us
    public long lastUpdated;

    //Data for us
    //Variables per project
    public string projectName;
    public List<BrushStroke> brushStrokes;
    public List<BrushStrokeID> brushStrokesID;
    public List<BrushStrokeID> lastDrawnStrokes;

    public byte[] overlayImg;

    // the values defined in this constructor will be the default values
    // the tool starts with when there's no data to load
    public ToolData() 
    {
        projectName = "";
        overlayImg = null;
        brushStrokes = new List<BrushStroke>();
        brushStrokesID = new List<BrushStrokeID>();
        lastDrawnStrokes = new List<BrushStrokeID>();
    }

    public string GetProjectName() 
    {
        return projectName;
    }
}

