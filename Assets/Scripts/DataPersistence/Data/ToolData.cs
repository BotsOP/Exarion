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

public abstract class ToolData
{
    //Variables per project
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
        timelineClips = new List<CondensedClip>();
    }
}

public struct JsonVector2
{
    public float x;
    public float y;

    public JsonVector2(float _x, float _y)
    {
        x = _x;
        y = _y;
    }
}

public struct JsonVector3
{
    public float x;
    public float y;
    public float z;

    public JsonVector3(float _x, float _y, float _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }
}

public struct JsonVector4
{
    public float x;
    public float y;
    public float z;
    public float w;

    public JsonVector4(float _x, float _y, float _z, float _w)
    {
        x = _x;
        y = _y;
        z = _z;
        w = _w;
    }
}

