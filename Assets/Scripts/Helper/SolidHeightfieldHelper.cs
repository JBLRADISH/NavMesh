using System.Collections.Generic;
using UnityEngine;

public static class SolidHeightfieldHelper
{
    public static void Collect(out List<Vector3> vertices, out List<int> triangles)
    {
        vertices = new List<Vector3>();
        triangles = new List<int>();
        int offset = 0;
        MeshFilter[] filters = Object.FindObjectsOfType<MeshFilter>();
        foreach (var filter in filters)
        {
            if (filter.GetComponent<Agent>() != null)
            {
                continue;
            }

            var matrix = filter.transform.localToWorldMatrix;
            var tmpVertices = filter.sharedMesh.vertices;
            foreach (var vertex in tmpVertices)
            {
                vertices.Add(matrix.MultiplyPoint3x4(vertex));
            }

            var tmpTriangles = filter.sharedMesh.triangles;
            foreach (var triangle in tmpTriangles)
            {
                triangles.Add(triangle + offset);
            }

            offset += tmpVertices.Length;
        }
    }
}