using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AABB
{
    public Vector3 Min { get; private set; }
    public Vector3 Max { get; private set; }
    public float Height => Max.y - Min.y;

    public AABB()
    {

    }

    public AABB(List<Vector3> vertices)
    {
        Refresh(vertices);
    }

    public void Refresh(List<Vector3> vertices)
    {
        if (vertices != null && vertices.Count > 0)
        {
            Min = vertices[0];
            Max = vertices[0];
            for (int i = 1; i < vertices.Count; i++)
            {
                Vector3 vertex = vertices[i];
                Min = Vector3.Min(Min, vertex);
                Max = Vector3.Max(Max, vertex);
            }
        }
    }

    public bool Overlap(AABB other)
    {
        for (int i = 0; i < 3; i++)
        {
            if (Min[i] > other.Max[i] || Max[i] < other.Min[i])
            {
                return false;
            }
        }

        return true;
    }
}