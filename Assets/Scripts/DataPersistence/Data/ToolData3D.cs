using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ToolData3D : ToolData
{
    //Mesh textures
    public List<byte[]> overlayImg = new List<byte[]>();
    public List<byte[]> maskImg = new List<byte[]>();
    
    //Mesh
    public bool meshLoaded;
    public string meshName;
    public List<List<int>> indices = new List<List<int>>();
    public List<List<JsonVector2>> uvs = new List<List<JsonVector2>>();
    public List<JsonVector3> vertexPos = new List<JsonVector3>();
    public List<JsonVector3> vertexNormal = new List<JsonVector3>();

    public Mesh LoadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = meshName;
        
        mesh.subMeshCount = indices.Count;
        
        mesh.vertices = vertexPos.Select(pos => new Vector3(pos.x, pos.y, pos.z)).ToArray();
        mesh.normals = vertexNormal.Select(normal => new Vector3(normal.x, normal.y, normal.z)).ToArray();
        
        for (int i = 0; i < indices.Count; i++)
        {
            mesh.SetIndices(indices[i], MeshTopology.Triangles, i);
            mesh.SetUVs(i, uvs[i].Select(uv => new Vector2(uv.x, uv.y)).ToArray());
        }
        mesh.RecalculateBounds();
        return mesh;
    }

    public void SaveMesh(Mesh _mesh)
    {
        if (_mesh == null)
        {
            Debug.Log($"Mesh is null");
            return;
        }
        meshName = _mesh.name;
        
        List<Vector3> vertexPosUnity = new List<Vector3>();
        _mesh.GetVertices(vertexPosUnity);
        vertexPos = vertexPosUnity.Select(pos => new JsonVector3(pos.x, pos.y, pos.z)).ToList();
        
        List<Vector3> vertexNormalUnity = new List<Vector3>();
        _mesh.GetNormals(vertexNormalUnity);
        vertexNormal = vertexNormalUnity.Select(normal => new JsonVector3(normal.x, normal.y, normal.z)).ToList();
        
        indices.Clear();
        uvs.Clear();
        for (int i = 0; i < _mesh.subMeshCount; i++)
        {
            List<int> indices = new List<int>();
            _mesh.GetIndices(indices, i);
            this.indices.Add(indices);
            
            List<Vector2> uvsUnity = new List<Vector2>();
            _mesh.GetUVs(i, uvsUnity);
            List<JsonVector2> uvsJson = uvsUnity.Select(uv => new JsonVector2(uv.x, uv.y)).ToList();
            uvs.Add(uvsJson);
        }

        meshLoaded = true;
    }

    
}


