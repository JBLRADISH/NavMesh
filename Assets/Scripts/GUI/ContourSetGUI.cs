using System.Collections.Generic;
using UnityEngine;

public static class ContourSetGUI
{
    public static void Draw(ContourSet contourSet, List<Color> drawColors, bool simplified)
    {
        foreach (var contour in contourSet.Contours)
        {
            var verts = simplified ? contour.SimplifiedVerts : contour.OriginVerts;
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 cur = GetContourPoint(contourSet, verts[i]);
                Vector3 next = GetContourPoint(contourSet, verts[(i + 1) % verts.Count]);
                Gizmos.color = drawColors[contour.Region];
                Gizmos.DrawSphere(cur, 0.1f);
                Gizmos.DrawLine(cur, next);
            }
        }
    }

    private static Vector3 GetContourPoint(ContourSet contourSet, Vector4Int vert)
    {
        float x = contourSet.Bounds.Min.x + vert.x * contourSet.VoxelSize;
        float y = contourSet.Bounds.Min.y + vert.y * contourSet.VoxelSize;
        float z = contourSet.Bounds.Min.z + vert.z * contourSet.VoxelSize;
        return new Vector3(x, y, z);
    }
}