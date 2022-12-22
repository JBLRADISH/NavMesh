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
                Vector3 cur = GetContourPoint(verts[i]);
                Vector3 next = GetContourPoint(verts[Util.Neighbor(i, verts.Count, false)]);
                Gizmos.color = drawColors[contour.Region];
                Gizmos.DrawSphere(cur, 0.1f);
                Gizmos.DrawLine(cur, next);
            }
        }
    }

    private static Vector3 GetContourPoint(Vector4Int vert)
    {
        float x = BuilderData.Bounds.Min.x + vert.x * BuilderData.VoxelSize;
        float y = BuilderData.Bounds.Min.y + vert.y * BuilderData.VoxelSize;
        float z = BuilderData.Bounds.Min.z + vert.z * BuilderData.VoxelSize;
        return new Vector3(x, y, z);
    }
}