using System.Collections.Generic;
using UnityEngine;

public static class PolyMeshFieldGUI
{
    public static void Draw(PolyMeshField polyMeshField, List<Color> drawColors)
    {
        int polyCount = 0;
        for (int ppi = 0; ppi < polyMeshField.Polys.Length; ppi += Config.MaxVertCountInPoly * 2)
        {
            for (int pvi = 0; pvi < Config.MaxVertCountInPoly; pvi++)
            {
                int pvc = polyMeshField.Polys[ppi + pvi];
                if (pvc == -1)
                {
                    break;
                }

                int pvn;
                if (pvi + 1 >= Config.MaxVertCountInPoly || polyMeshField.Polys[ppi + pvi + 1] == -1)
                {
                    pvn = polyMeshField.Polys[ppi];
                }
                else
                {
                    pvn = polyMeshField.Polys[ppi + pvi + 1];
                }

                Vector3 cur = GetPolyPoint(polyMeshField, polyMeshField.Verts[pvc]);
                Vector3 next = GetPolyPoint(polyMeshField, polyMeshField.Verts[pvn]);
                Gizmos.color = drawColors[polyMeshField.Regions[polyCount]];
                Gizmos.DrawSphere(cur, 0.1f);
                Gizmos.DrawLine(cur, next);
            }

            polyCount++;
        }
    }
    
    private static Vector3 GetPolyPoint(PolyMeshField polyMeshField, Vector4Int vert)
    {
        float x = polyMeshField.Bounds.Min.x + vert.x * polyMeshField.VoxelSize;
        float y = polyMeshField.Bounds.Min.y + vert.y * polyMeshField.VoxelSize;
        float z = polyMeshField.Bounds.Min.z + vert.z * polyMeshField.VoxelSize;
        return new Vector3(x, y, z);
    }
}