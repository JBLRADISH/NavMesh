﻿using UnityEngine;

public static class OpenHeightfieldGUI
{
    public static void Draw(OpenHeightField openHeightfield)
    {
        Vector3 size = Vector3.one * Global.VoxelSize;

        foreach (var item in openHeightfield.Spans)
        {
            var span = item.Value;
            while (span != null)
            {
                if (span.Region == 0)
                {
                    span = span.Next;
                    continue;
                }

                int widthIndex = item.Key / Global.Depth;
                int depthIndex = item.Key % Global.Depth;
                float x = Global.Bounds.Min.x + (widthIndex + 0.5f) * Global.VoxelSize;
                float y = Global.Bounds.Min.y + (span.Floor + 0.5f) * Global.VoxelSize;
                float z = Global.Bounds.Min.z + (depthIndex + 0.5f) * Global.VoxelSize;
                Vector3 center = new Vector3(x, y, z);

                Gizmos.color = openHeightfield.DrawColors[span.Region];
                Gizmos.DrawWireCube(center, size);

                span = span.Next;
            }
        }
    }
}