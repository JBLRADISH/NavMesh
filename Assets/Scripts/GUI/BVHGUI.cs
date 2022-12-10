using UnityEngine;

public static class BVHGUI
{
    public static void Draw(BVH bvh)
    {
        for (int i = 0; i < bvh.Nodes.Length; i++)
        {
            DrawAABB(bvh.Nodes[i].Bounds, Color.red);
        }
    }

    private static void DrawAABB(AABB aabb, Color color)
    {
        Vector3 min = GetPolyPoint(aabb.Min);
        Vector3 max = GetPolyPoint(aabb.Max);
        Vector3 center = (min + max) / 2;
        Vector3 size = max - min;
        Gizmos.color = color;
        Gizmos.DrawWireCube(center, size);
    }

    private static Vector3 GetPolyPoint(Vector3 vert)
    {
        float x = Global.Bounds.Min.x + vert.x * Global.VoxelSize;
        float y = Global.Bounds.Min.y + vert.y * Global.VoxelSize;
        float z = Global.Bounds.Min.z + vert.z * Global.VoxelSize;
        return new Vector3(x, y, z);
    }
}