using System.Collections.Generic;
using UnityEngine;

public struct AABB
{
    public Vector3 Min;
    public Vector3 Max;
    public float Height => Max.y - Min.y;

    public static AABB Default => new AABB {Min = Vector3.positiveInfinity, Max = Vector3.negativeInfinity};

    public AABB(List<Vector3> vertices) : this()
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

    public AABB(Vector4Int[] verts, int[] polys, int idx, int len) : this()
    {
        Refresh(verts, polys, idx, len);
    }

    public void Refresh(Vector4Int[] verts, int[] polys, int idx, int len)
    {
        Min = verts[polys[idx]].Vector3;
        Max = verts[polys[idx]].Vector3;
        for (int i = 1; i < len; i++)
        {
            int vertIdx = polys[idx + i];
            if (vertIdx == -1)
            {
                break;
            }

            Vector3 vertex = verts[vertIdx].Vector3;
            Min = Vector3.Min(Min, vertex);
            Max = Vector3.Max(Max, vertex);
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
    
    public bool Overlap(Vector3 p)
    {
        for (int i = 0; i < 3; i++)
        {
            if (Min[i] > p[i] || Max[i] < p[i])
            {
                return false;
            }
        }

        return true;
    }

    public void Union(AABB other)
    {
        for (int i = 0; i < 3; i++)
        {
            if (other.Min[i] < Min[i])
            {
                Min[i] = other.Min[i];
            }

            if (other.Max[i] > Max[i])
            {
                Max[i] = other.Max[i];
            }
        }
    }

    public int MaximumExtent()
    {
        int idx = 0;
        for (int i = 1; i < 3; i++)
        {
            if (Max[i] - Min[i] > Max[idx] - Min[idx])
            {
                idx = i;
            }
        }

        return idx;
    }
}