using System.Collections.Generic;
using UnityEngine;

public static class PolyMeshFieldGUI
{
    public static void Draw(PolyMeshField polyMeshField, List<Color> drawColors)
    {
        int polyCount = 0;
        for (int ppi = 0; ppi < polyMeshField.Polys.Length; ppi += Global.MaxVertCountInPoly * 2)
        {
            for (int pvi = 0; pvi < Global.MaxVertCountInPoly; pvi++)
            {
                int pvc = polyMeshField.Polys[ppi + pvi];
                if (pvc == -1)
                {
                    break;
                }

                int pvn;
                if (pvi + 1 >= Global.MaxVertCountInPoly || polyMeshField.Polys[ppi + pvi + 1] == -1)
                {
                    pvn = polyMeshField.Polys[ppi];
                }
                else
                {
                    pvn = polyMeshField.Polys[ppi + pvi + 1];
                }

                Vector3 cur = GetPolyPoint(polyMeshField.Verts[pvc]);
                Vector3 next = GetPolyPoint(polyMeshField.Verts[pvn]);
                Gizmos.color = drawColors[polyMeshField.Regions[polyCount]];
                Gizmos.DrawSphere(cur, 0.1f);
                Gizmos.DrawLine(cur, next);
            }

            polyCount++;
        }
    }
    
    private static Vector3 GetPolyPoint(Vector4Int vert)
    {
        float x = Global.Bounds.Min.x + vert.x * Global.VoxelSize;
        float y = Global.Bounds.Min.y + vert.y * Global.VoxelSize;
        float z = Global.Bounds.Min.z + vert.z * Global.VoxelSize;
        return new Vector3(x, y, z);
    }
}