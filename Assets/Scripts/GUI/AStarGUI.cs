using System.Collections.Generic;
using UnityEngine;

public class AStarGUI
{
    public static void Draw(ref NavMeshData navMeshData, List<int> polys)
    {
        Vector3 pre = Vector3.negativeInfinity;
        foreach (var poly in polys)
        {
            int ppi = poly * Global.MaxVertCountInPoly * 2;
            int ppc = Util.GetPolyVertCount(ppi, navMeshData.Polys);
            Vector3 cur = Vector3.zero;
            for (int pvi = 0; pvi < ppc; pvi++)
            {
                cur += navMeshData.Verts[navMeshData.Polys[ppi + pvi]];
            }

            cur /= ppc;
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(cur, 0.1f);
            if (pre != Vector3.negativeInfinity)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(pre, cur);
            }

            pre = cur;
        }
    }
}
