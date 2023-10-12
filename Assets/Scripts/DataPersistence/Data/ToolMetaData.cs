using System.Collections.Generic;
using UnityEngine;

namespace DataPersistence.Data
{
    public class ToolMetaData
    {
        public long lastUpdated;
        public float timeSpentMinutes;

        //Variables per project
        public ProjectType projectType;
        public string projectName;
        
        public int imageWidth;
        public int imageHeight;
        
        //Mesh textures
        public List<byte[]> overlayImg = new List<byte[]>();
    
        //Mesh
        public List<List<int>> indices = new List<List<int>>();
        public List<List<Vector2>> uvs = new List<List<Vector2>>();
        public List<Vector3> vertexPos = new List<Vector3>();
        public List<Vector3> vertexNormal = new List<Vector3>();
        public List<Vector4> vertexTangents = new List<Vector4>();
    }
}