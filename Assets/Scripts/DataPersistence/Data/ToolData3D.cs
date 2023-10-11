using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ToolData3D : ToolData
{
    public List<byte[]> overlayImg = new List<byte[]>();
    public List<List<int>> indices = new List<List<int>>();
    public List<List<Vector2>> uvs = new List<List<Vector2>>();
    public List<Vector3> vertexPos = new List<Vector3>();
    public List<Vector3> vertexNormal = new List<Vector3>();
    public List<Vector4> vertexTangents = new List<Vector4>();
}
