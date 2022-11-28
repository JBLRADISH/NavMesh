using System.Collections.Generic;
using UnityEngine;

public static class ContourSetGUI
{
    public static void Draw(ContourSet contourSet, List<Color> drawColors, bool simplified)
    {
        foreach (var contour in contourSet.Contours)
        {
            int count = simplified ? contour.SimplifiedCount : contour.OriginCount;
            var verts = simplified ? contour.SimplifiedVerts : contour.OriginVerts;
            for (int i = 0; i < count; i++)
            {
                Vector3 cur = GetContourPoint(contourSet, verts, i);
                Vector3 next = GetContourPoint(contourSet, verts, (i + 1) % count);
                Gizmos.color = drawColors[contour.Region];
                Gizmos.DrawSphere(cur, 0.1f);
                Gizmos.DrawLine(cur, next);
            }
        }
    }

    private static Vector3 GetContourPoint(ContourSet contourSet, List<int> verts, int index)
    {
        float x = contourSet.Bounds.Min.x + verts[index * 4] * contourSet.VoxelSize;
        float y = contourSet.Bounds.Min.y + verts[index * 4 + 1] * contourSet.VoxelSize;
        float z = contourSet.Bounds.Min.z + verts[index * 4 + 2] * contourSet.VoxelSize;
        return new Vector3(x, y, z);
    }
}