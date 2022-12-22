using System.Collections.Generic;
using UnityEngine;

public static class StringPullingGUI
{
    public static void Draw(List<Vector3> path)
    {
        Vector3 pre = Vector3.negativeInfinity;
        foreach (var cur in path)
        {
            if (pre != Vector3.negativeInfinity)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(pre, cur);
            }

            pre = cur;
        }
    }
}