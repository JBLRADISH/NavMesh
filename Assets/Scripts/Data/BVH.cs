using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct LinearBVHNode
{
    public AABB Bounds;
    //>=0代表多边形索引 <0代表右子树在数组里的偏移
    public int Idx;
}

public struct BVH
{
    public LinearBVHNode[] Nodes;

    public List<int> Traverse(ref Vector3 voxel)
    {
        List<int> polys = ObjectPool.Get<List<int>>();
        polys.Clear();
        int i = 0;
        while (i < Nodes.Length)
        {
            var cur = Nodes[i];
            bool overlap = cur.Bounds.Overlap(voxel);
            bool leaf = cur.Idx >= 0;
            if (leaf && overlap)
            {
                polys.Add(cur.Idx);
            }

            if (overlap || leaf)
            {
                i++;
            }
            else
            {
                i = -cur.Idx;
            }
        }

        return polys;
    }
}