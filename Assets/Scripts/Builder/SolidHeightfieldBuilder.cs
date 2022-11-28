using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SolidHeightfieldBuilder
{
    private NavMeshBuildSettings settings;
    private int agentHeight;
    private float inverseVoxelSize;
    private float minNormalY;

    public SolidHeightfieldBuilder(int agentTypeID)
    {
        settings = NavMesh.GetSettingsByID(agentTypeID);
        agentHeight = MathTool.CeilToInt(settings.agentHeight / settings.voxelSize);
        inverseVoxelSize = 1 / settings.voxelSize;
        minNormalY = Mathf.Cos(settings.agentSlope / 180 * Mathf.PI);
    }

    public SolidHeightfield Build()
    {
        SolidHeightfield solidHeightfield = new SolidHeightfield(settings.voxelSize);
        SolidHeightfieldHelper.Collect(out List<Vector3> vertices, out List<int> triangles);
        AABB bounds = new AABB(vertices);
        solidHeightfield.SetBounds(bounds);
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
    private static AABB tempAABB = new AABB();

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
        if (!solidHeightfield.Bounds.Overlap(tempAABB))
        {
            return;
        }

        int triWidthMin = MathTool.FloorToInt((tempAABB.Min.x - solidHeightfield.Bounds.Min.x) * inverseVoxelSize);
        int triDepthMin = MathTool.FloorToInt((tempAABB.Min.z - solidHeightfield.Bounds.Min.z) * inverseVoxelSize);
        int triWidthMax = MathTool.FloorToInt((tempAABB.Max.x - solidHeightfield.Bounds.Min.x) * inverseVoxelSize);
        int triDepthMax = MathTool.FloorToInt((tempAABB.Max.z - solidHeightfield.Bounds.Min.z) * inverseVoxelSize);

        triWidthMin = Mathf.Clamp(triWidthMin, 0, solidHeightfield.Width - 1);
        triDepthMin = Mathf.Clamp(triDepthMin, 0, solidHeightfield.Depth - 1);
        triWidthMax = Mathf.Clamp(triWidthMax, 0, solidHeightfield.Width - 1);
        triDepthMax = Mathf.Clamp(triDepthMax, 0, solidHeightfield.Depth - 1);

        for (int depthIndex = triDepthMin; depthIndex <= triDepthMax; ++depthIndex)
        {
            tempInVerticesList.Clear();
            tempInVerticesList.Add(p1);
            tempInVerticesList.Add(p2);
            tempInVerticesList.Add(p3);

            float z = solidHeightfield.Bounds.Min.z + depthIndex * solidHeightfield.VoxelSize;

            ClipTriangle(tempInVerticesList, tempInOutVerticesList, 2, 1, -z);
            if (tempInOutVerticesList.Count < 3)
            {
                continue;
            }

            ClipTriangle(tempInOutVerticesList, tempOutVerticesList, 2, -1, z + solidHeightfield.VoxelSize);
            if (tempOutVerticesList.Count < 3)
            {
                continue;
            }

            for (int widthIndex = triWidthMin; widthIndex <= triWidthMax; ++widthIndex)
            {
                float x = solidHeightfield.Bounds.Min.x + widthIndex * solidHeightfield.VoxelSize;

                ClipTriangle(tempOutVerticesList, tempInOutVerticesList, 0, 1, -x);
                if (tempInOutVerticesList.Count < 3)
                {
                    continue;
                }

                ClipTriangle(tempInOutVerticesList, tempInVerticesList, 0, -1, x + solidHeightfield.VoxelSize);
                if (tempInVerticesList.Count < 3)
                {
                    continue;
                }

                tempAABB.Refresh(tempInVerticesList);

                float heightMin = tempAABB.Min.y - solidHeightfield.Bounds.Min.y;
                float heightMax = tempAABB.Max.y - solidHeightfield.Bounds.Min.y;

                if (heightMax < 0.0f || heightMin > solidHeightfield.BoundsHeight)
                {
                    continue;
                }

                if (heightMin < 0.0f)
                {
                    heightMin = 0.0f;
                }

                if (heightMax > solidHeightfield.BoundsHeight)
                {
                    heightMax = solidHeightfield.BoundsHeight;
                }

                int heightIndexMin = Mathf.Max(MathTool.FloorToInt(heightMin * inverseVoxelSize), 0);
                int heightIndexMax = Mathf.Max(MathTool.CeilToInt(heightMax * inverseVoxelSize), 0);

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
                if (spanCeiling - spanFloor < agentHeight)
                {
                    span.Flag = 0;
                }

                span = span.Next;
            }
        }
    }
}