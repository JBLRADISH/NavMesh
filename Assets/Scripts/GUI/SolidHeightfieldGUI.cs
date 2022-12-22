using UnityEngine;

public static class SolidHeightfieldGUI
{
    public static void Draw(SolidHeightfield solidHeightfield)
    {
        Vector3 size = Vector3.one * BuilderData.VoxelSize;

        foreach (var item in solidHeightfield.Spans)
        {
            var span = item.Value;
            while (span != null)
            {
                int widthIndex = item.Key / BuilderData.Depth;
                int depthIndex = item.Key % BuilderData.Depth;
                float x = BuilderData.Bounds.Min.x + (widthIndex + 0.5f) * BuilderData.VoxelSize;
                float y = BuilderData.Bounds.Min.y + (span.HeightIndexMin + 0.5f) * BuilderData.VoxelSize;
                float z = BuilderData.Bounds.Min.z + (depthIndex + 0.5f) * BuilderData.VoxelSize;
                Vector3 center = new Vector3(x, y, z);

                Gizmos.color = Color.red;
                for (int i = span.HeightIndexMin; i < span.HeightIndexMax; i++)
                {
                    Gizmos.DrawWireCube(center, size);
                    center.y += BuilderData.VoxelSize;
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