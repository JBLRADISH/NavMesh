using System.Collections.Generic;
using UnityEngine;

public class SolidHeightfieldBuilder
{
    private float minNormalY;

    public SolidHeightfieldBuilder()
    {
        minNormalY = Mathf.Cos(BuilderData.AgentSlope / 180 * Mathf.PI);
    }

    public SolidHeightfield Build()
    {
        SolidHeightfield solidHeightfield = new SolidHeightfield();
        SolidHeightfieldHelper.Collect(out List<Vector3> vertices, out List<int> triangles);
        AABB bounds = new AABB(vertices);
        BuilderData.SetBounds(bounds);
        int[] triangleFlags = MarkInputMeshWalkableFlags(vertices, triangles);
        for (int i = 0; i < triangleFlags.Length; i++)
        {
            VoxelizeTriangle(i, vertices, triangles, triangleFlags[i], solidHeightfield);
        }

        MarkLowHeightSpans(solidHeightfield);

        return solidHeightfield;
    }

    private int[] MarkInputMeshWalkableFlags(List<Vector3> vertices, List<int> triangles)
    {
        int[] flags = new int[triangles.Count / 3];
        for (int i = 0; i < flags.Length; i++)
        {
            Vector3 p1 = vertices[triangles[i * 3]];
            Vector3 p2 = vertices[triangles[i * 3 + 1]];
            Vector3 p3 = vertices[triangles[i * 3 + 2]];
            Vector3 n = MathTool.Normal(p1, p2, p3);
            if (n.y > minNormalY)
            {
                flags[i] = 1;
            }
        }

        return flags;
    }

    private static List<Vector3> tempInVerticesList = new List<Vector3>();
    private static List<Vector3> tempInOutVerticesList = new List<Vector3>();
    private static List<Vector3> tempOutVerticesList = new List<Vector3>();
    private static AABB tempAABB = AABB.Default;

    private void VoxelizeTriangle(int index, List<Vector3> vertices, List<int> triangles, int flag, SolidHeightfield solidHeightfield)
    {
        Vector3 p1 = vertices[triangles[index * 3]];
        Vector3 p2 = vertices[triangles[index * 3 + 1]];
        Vector3 p3 = vertices[triangles[index * 3 + 2]];

        tempInVerticesList.Clear();
        tempInVerticesList.Add(p1);
        tempInVerticesList.Add(p2);
        tempInVerticesList.Add(p3);

        tempAABB.Refresh(tempInVerticesList);
        if (!BuilderData.Bounds.Overlap(tempAABB))
        {
            return;
        }

        int triWidthMin = MathTool.FloorToInt((tempAABB.Min.x - BuilderData.Bounds.Min.x) * BuilderData.InverseVoxelSize);
        int triDepthMin = MathTool.FloorToInt((tempAABB.Min.z - BuilderData.Bounds.Min.z) * BuilderData.InverseVoxelSize);
        int triWidthMax = MathTool.FloorToInt((tempAABB.Max.x - BuilderData.Bounds.Min.x) * BuilderData.InverseVoxelSize);
        int triDepthMax = MathTool.FloorToInt((tempAABB.Max.z - BuilderData.Bounds.Min.z) * BuilderData.InverseVoxelSize);

        triWidthMin = Mathf.Clamp(triWidthMin, 0, BuilderData.Width - 1);
        triDepthMin = Mathf.Clamp(triDepthMin, 0, BuilderData.Depth - 1);
        triWidthMax = Mathf.Clamp(triWidthMax, 0, BuilderData.Width - 1);
        triDepthMax = Mathf.Clamp(triDepthMax, 0, BuilderData.Depth - 1);

        for (int depthIndex = triDepthMin; depthIndex <= triDepthMax; ++depthIndex)
        {
            tempInVerticesList.Clear();
            tempInVerticesList.Add(p1);
            tempInVerticesList.Add(p2);
            tempInVerticesList.Add(p3);

            float z = BuilderData.Bounds.Min.z + depthIndex * BuilderData.VoxelSize;

            ClipTriangle(tempInVerticesList, tempInOutVerticesList, 2, 1, -z);
            if (tempInOutVerticesList.Count < 3)
            {
                continue;
            }

            ClipTriangle(tempInOutVerticesList, tempOutVerticesList, 2, -1, z + BuilderData.VoxelSize);
            if (tempOutVerticesList.Count < 3)
            {
                continue;
            }

            for (int widthIndex = triWidthMin; widthIndex <= triWidthMax; ++widthIndex)
            {
                float x = BuilderData.Bounds.Min.x + widthIndex * BuilderData.VoxelSize;

                ClipTriangle(tempOutVerticesList, tempInOutVerticesList, 0, 1, -x);
                if (tempInOutVerticesList.Count < 3)
                {
                    continue;
                }

                ClipTriangle(tempInOutVerticesList, tempInVerticesList, 0, -1, x + BuilderData.VoxelSize);
                if (tempInVerticesList.Count < 3)
                {
                    continue;
                }

                tempAABB.Refresh(tempInVerticesList);

                float heightMin = tempAABB.Min.y - BuilderData.Bounds.Min.y;
                float heightMax = tempAABB.Max.y - BuilderData.Bounds.Min.y;

                if (heightMax < 0.0f || heightMin > BuilderData.BoundsHeight)
                {
                    continue;
                }

                if (heightMin < 0.0f)
                {
                    heightMin = 0.0f;
                }

                if (heightMax > BuilderData.BoundsHeight)
                {
                    heightMax = BuilderData.BoundsHeight;
                }

                int heightIndexMin = Mathf.Max(MathTool.FloorToInt(heightMin * BuilderData.InverseVoxelSize), 0);
                int heightIndexMax = Mathf.Max(MathTool.CeilToInt(heightMax * BuilderData.InverseVoxelSize), 0);

                solidHeightfield.AddSpan(widthIndex, depthIndex, heightIndexMin, heightIndexMax, flag);
            }
        }
    }

    private void ClipTriangle(List<Vector3> inVertices, List<Vector3> outVertices, int axis, int scale, float offset)
    {
        outVertices.Clear();

        float[] dist = new float[inVertices.Count];
        for (int i = 0; i < inVertices.Count; i++)
        {
            dist[i] = (axis == 0 ? 1 : 0) * scale * inVertices[i].x + (axis == 2 ? 1 : 0) * scale * inVertices[i].z + offset;
        }

        for (int cur = 0, pre = dist.Length - 1; cur < dist.Length; pre = cur, ++cur)
        {
            bool ina = dist[pre] >= 0;
            bool inb = dist[cur] >= 0;
            if (ina != inb)
            {
                float t = dist[pre] / (dist[pre] - dist[cur]);
                outVertices.Add(Vector3.Lerp(inVertices[pre], inVertices[cur], t));
            }

            if (inb)
            {
                outVertices.Add(inVertices[cur]);
            }
        }
    }

    private void MarkLowHeightSpans(SolidHeightfield solidHeightfield)
    {
        foreach (var item in solidHeightfield.Spans)
        {
            var span = item.Value;
            while (span != null)
            {
                if (span.Flag == 0)
                {
                    span = span.Next;
                    continue;
                }

                int spanFloor = span.HeightIndexMax;
                int spanCeiling = span.Next?.HeightIndexMin ?? int.MaxValue;
                if (spanCeiling - spanFloor < BuilderData.AgentHeight)
                {
                    span.Flag = 0;
                }

                span = span.Next;
            }
        }
    }
}