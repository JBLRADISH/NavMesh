using System.Collections.Generic;
using UnityEngine;

public class OpenHeightField
{
    public int SpanCount { get; private set; }
    public int MaxDistToBorder;
    public int RegionCount;

    public Dictionary<int, OpenHeightSpan> Spans = new Dictionary<int, OpenHeightSpan>();

    public void AddSpan(int index, HeightSpan span)
    {
        OpenHeightSpan baseSpan = null;
        OpenHeightSpan preSpan = null;
        while (span != null)
        {
            if (span.Flag == 0)
            {
                span = span.Next;
                continue;
            }

            int floor = span.HeightIndexMax;
            int ceiling = span.Next?.HeightIndexMin ?? int.MaxValue;
            var newSpan = new OpenHeightSpan(floor, ceiling - floor);

            if (baseSpan == null)
            {
                baseSpan = newSpan;
            }
            else
            {
                preSpan.Next = newSpan;
            }

            preSpan = newSpan;

            SpanCount++;

            span = span.Next;
        }

        if (baseSpan != null)
        {
            Spans[index] = baseSpan;
        }
    }

    private int GetNeighborIndex(int index, int dir)
    {
        int widthIndex = index / BuilderData.Depth;
        int depthIndex = index % BuilderData.Depth;
        switch (dir)
        {
            case 0:
                widthIndex--;
                break;
            case 1:
                depthIndex++;
                break;
            case 2:
                widthIndex++;
                break;
            case 3:
                depthIndex--;
                break;
        }

        return GetIndex(widthIndex, depthIndex);
    }

    public OpenHeightSpan GetNeighborSpan(int index, int dir)
    {
        int neighborIndex = GetNeighborIndex(index, dir);
        return GetSpan(neighborIndex);
    }

    public OpenHeightSpan GetSpan(int index)
    {
        Spans.TryGetValue(index, out OpenHeightSpan span);
        return span;
    }

    public int GetIndex(int widthIndex, int depthIndex)
    {
        if (widthIndex < 0 || depthIndex < 0 || widthIndex >= BuilderData.Width || depthIndex >= BuilderData.Depth)
        {
            return -1;
        }

        return widthIndex * BuilderData.Depth + depthIndex;
    }

#if UNITY_EDITOR

    public List<Color> DrawColors = new List<Color>();

    public void UpdateDrawColors()
    {
        DrawColors.Clear();
        for (int i = 0; i < RegionCount; i++)
        {
            DrawColors.Add(new Color(Random.value, Random.value, Random.value));
        }
    }

#endif
}