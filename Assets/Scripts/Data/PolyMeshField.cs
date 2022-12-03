using System.Collections.Generic;
using UnityEngine;

public class PolyMeshField
{
    public float VoxelSize { get; private set; }
    public AABB Bounds { get; private set; }
    
    public Vector4Int[] Verts = null;
    public int[] Polys;
    public int[] Regions;
    
    public PolyMeshField(ContourSet contourSet)
    {
        VoxelSize = contourSet.VoxelSize;
        Bounds = contourSet.Bounds;
    }
}