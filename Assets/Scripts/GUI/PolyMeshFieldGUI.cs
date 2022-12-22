using System.Collections.Generic;
using UnityEngine;

public static class PolyMeshFieldGUI
{
    public static void Draw(PolyMeshField polyMeshField, List<Color> drawColors)
    {
        int polyCount = 0;
        for (int ppi = 0; ppi < polyMeshField.Polys.Length; ppi += BuilderData.MaxVertCountInPoly * 2)
        {
            for (int pvi = 0; pvi < BuilderData.MaxVertCountInPoly; pvi++)
            {
                int pvc = polyMeshField.Polys[ppi + pvi];
                if (pvc == -1)
                {
                    break;
                }

                int pvn;
                if (pvi + 1 >= BuilderData.MaxVertCountInPoly || polyMeshField.Polys[ppi + pvi + 1] == -1)
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
        float x = BuilderData.Bounds.Min.x + vert.x * BuilderData.VoxelSize;
        float y = BuilderData.Bounds.Min.y + vert.y * BuilderData.VoxelSize;
        float z = BuilderData.Bounds.Min.z + vert.z * BuilderData.VoxelSize;
        return new Vector3(x, y, z);
    }

    public static void Draw(ref NavMeshData navMeshData)
    {
        int polyCount = 0;
        for (int ppi = 0; ppi < navMeshData.Polys.Length; ppi += navMeshData.MaxVertCountInPoly * 2)
        {
            for (int pvi = 0; pvi < navMeshData.MaxVertCountInPoly; pvi++)
            {
                int pvc = navMeshData.Polys[ppi + pvi];
                if (pvc == -1)
                {
                    break;
                }

                int pvn;
                if (pvi + 1 >= navMeshData.MaxVertCountInPoly || navMeshData.Polys[ppi + pvi + 1] == -1)
                {
                    pvn = navMeshData.Polys[ppi];
                }
                else
                {
                    pvn = navMeshData.Polys[ppi + pvi + 1];
                }

                Vector3 cur = navMeshData.Verts[pvc];
                Vector3 next = navMeshData.Verts[pvn];
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(cur, 0.1f);
                Gizmos.DrawLine(cur, next);
            }

            polyCount++;
        }
    }
}