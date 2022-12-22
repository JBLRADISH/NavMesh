using System.Runtime.InteropServices;
using UnityEngine;

[StructLayout(LayoutKind.Sequential)]
public struct NavMeshData
{
    public Vector3 BoundsMin;
    public float InverseVoxelSize;
    public int MaxVertCountInPoly;
    public Vector3[] Verts;
    public int[] Polys;
    public BVH BVH;
}