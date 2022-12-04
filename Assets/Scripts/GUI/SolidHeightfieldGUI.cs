using UnityEngine;

public static class SolidHeightfieldGUI
{
    public static void Draw(SolidHeightfield solidHeightfield)
    {
        Vector3 size = Vector3.one * Global.VoxelSize;

        foreach (var item in solidHeightfield.Spans)
        {
            var span = item.Value;
            while (span != null)
            {
                int widthIndex = item.Key / Global.Depth;
                int depthIndex = item.Key % Global.Depth;
                float x = Global.Bounds.Min.x + (widthIndex + 0.5f) * Global.VoxelSize;
                float y = Global.Bounds.Min.y + (span.HeightIndexMin + 0.5f) * Global.VoxelSize;
                float z = Global.Bounds.Min.z + (depthIndex + 0.5f) * Global.VoxelSize;
                Vector3 center = new Vector3(x, y, z);

                Gizmos.color = Color.red;
                for (int i = span.HeightIndexMin; i < span.HeightIndexMax; i++)
                {
                    Gizmos.DrawWireCube(center, size);
                    center.y += Global.VoxelSize;
                }

                if (span.Flag == 1)
                {
                    Gizmos.color = Color.green;
                }

                Gizmos.DrawWireCube(center, size);

                span = span.Next;
            }
        }
    }
}