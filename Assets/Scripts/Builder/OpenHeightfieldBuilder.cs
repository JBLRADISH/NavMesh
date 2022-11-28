using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.AI;

public class OpenHeightfieldBuilder
{
    private int agentHeight;
    private int agentClimb;
    private int agentRadius;

    public OpenHeightfieldBuilder(int agentTypeID)
    {
        var settings = NavMesh.GetSettingsByID(agentTypeID);
        agentHeight = MathTool.CeilToInt(settings.agentHeight / settings.voxelSize);
        agentClimb = MathTool.FloorToInt(settings.agentClimb / settings.voxelSize);
        agentRadius = MathTool.CeilToInt(settings.agentRadius / settings.voxelSize);
    }

    public OpenHeightField Build(SolidHeightfield solidHeightfield)
    {
        OpenHeightField openHeightField = new OpenHeightField(solidHeightfield);

        foreach (var item in solidHeightfield.Spans)
        {
            openHeightField.AddSpan(item.Key, item.Value);
        }

        GenerateNeighborLinks(openHeightField);
        MarkBorderSpans(openHeightField);
        BlurDist(openHeightField);
        GenerateRegions(openHeightField);
        FilterRegions(openHeightField);

        UpdateGUI(openHeightField);

        return openHeightField;
    }

    private void GenerateNeighborLinks(OpenHeightField openHeightField)
    {
        foreach (var item in openHeightField.Spans)
        {
            var span = item.Value;
            while (span != null)
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    var neighborSpan = openHeightField.GetNeighborSpan(item.Key, dir);
                    while (neighborSpan != null)
                    {
                        int maxFloor = Mathf.Max(span.Floor, neighborSpan.Floor);
                        int minCeiling = Mathf.Min(span.Ceiling, neighborSpan.Ceiling);
                        if (minCeiling - maxFloor >= agentHeight && Mathf.Abs(neighborSpan.Floor - span.Floor) <= agentClimb)
                        {
                            span.Neighbors[dir] = neighborSpan;
                            break;
                        }

                        neighborSpan = neighborSpan.Next;
                    }
                }

                span = span.Next;
            }
        }
    }

    private void MarkBorderSpans(OpenHeightField openHeightField)
    {
        foreach (var item in openHeightField.Spans)
        {
            var span = item.Value;
            while (span != null)
            {
                span.DistToBorder = int.MaxValue;
                for (int dir = 0; dir < 4; dir++)
                {
                    var neighborSpan = span.Neighbors[dir];
                    if (neighborSpan == null)
                    {
                        span.DistToBorder = 0;
                        break;
                    }
                }

                span = span.Next;
            }
        }

        for (int depthIndex = 0; depthIndex < openHeightField.Depth; depthIndex++)
        {
            for (int widthIndex = 0; widthIndex < openHeightField.Width; widthIndex++)
            {
                int index = openHeightField.GetIndex(widthIndex, depthIndex);
                var span = openHeightField.GetSpan(index);
                while (span != null)
                {
                    if (span.DistToBorder != 0)
                    {
                        var neighborSpan = span.Neighbors[0];
                        span.DistToBorder = Mathf.Min(span.DistToBorder, neighborSpan.DistToBorder + 2);

                        neighborSpan = neighborSpan.Neighbors[3];
                        if (neighborSpan != null)
                        {
                            span.DistToBorder = Mathf.Min(span.DistToBorder, neighborSpan.DistToBorder + 3);
                        }

                        neighborSpan = span.Neighbors[3];
                        span.DistToBorder = Mathf.Min(span.DistToBorder, neighborSpan.DistToBorder + 2);

                        neighborSpan = neighborSpan.Neighbors[2];
                        if (neighborSpan != null)
                        {
                            span.DistToBorder = Mathf.Min(span.DistToBorder, neighborSpan.DistToBorder + 3);
                        }
                    }

                    span = span.Next;
                }
            }
        }

        for (int depthIndex = openHeightField.Depth - 1; depthIndex >= 0; depthIndex--)
        {
            for (int widthIndex = openHeightField.Width - 1; widthIndex >= 0; widthIndex--)
            {
                int index = openHeightField.GetIndex(widthIndex, depthIndex);
                var span = openHeightField.GetSpan(index);
                while (span != null)
                {
                    if (span.DistToBorder != 0)
                    {
                        var neighborSpan = span.Neighbors[2];
                        span.DistToBorder = Mathf.Min(span.DistToBorder, neighborSpan.DistToBorder + 2);

                        neighborSpan = neighborSpan.Neighbors[1];
                        if (neighborSpan != null)
                        {
                            span.DistToBorder = Mathf.Min(span.DistToBorder, neighborSpan.DistToBorder + 3);
                        }

                        neighborSpan = span.Neighbors[1];
                        span.DistToBorder = Mathf.Min(span.DistToBorder, neighborSpan.DistToBorder + 2);

                        neighborSpan = neighborSpan.Neighbors[0];
                        if (neighborSpan != null)
                        {
                            span.DistToBorder = Mathf.Min(span.DistToBorder, neighborSpan.DistToBorder + 3);
                        }
                    }

                    if (span.DistToBorder < 2 * agentRadius)
                    {
                        span.Flag = 0;
                    }

                    span = span.Next;
                }
            }
        }
    }

    private void BlurDist(OpenHeightField openHeightField)
    {
        openHeightField.MaxDistToBorder = 0;
        foreach (var item in openHeightField.Spans)
        {
            var span = item.Value;
            while (span != null)
            {
                if (span.Flag == 1)
                {
                    var dist = span.DistToBorder;
                    for (int dir = 0; dir < 4; dir++)
                    {
                        var neighborSpan = span.Neighbors[dir];
                        dist += neighborSpan.DistToBorder;
                        neighborSpan = neighborSpan.Neighbors[(dir + 1) & 0x3];
                        dist += neighborSpan?.DistToBorder ?? span.DistToBorder;
                    }

                    //近似四舍五入
                    span.DistToBorder = (dist + 5) / 9;

                    if (span.DistToBorder > openHeightField.MaxDistToBorder)
                    {
                        openHeightField.MaxDistToBorder = span.DistToBorder;
                    }
                }

                span = span.Next;
            }
        }
    }

    private void GenerateRegions(OpenHeightField openHeightField)
    {
        int maxIter = 8;

        int dist = (openHeightField.MaxDistToBorder + 1) & ~1;

        List<OpenHeightSpan> floodedSpans = new List<OpenHeightSpan>(1024);

        Queue<OpenHeightSpan> pendingQueue = new Queue<OpenHeightSpan>(1024);

        int nextRegion = 1;

        while (dist > 0)
        {
            dist = Mathf.Max(dist - 2, 0);

            floodedSpans.Clear();
            foreach (var item in openHeightField.Spans)
            {
                var span = item.Value;
                while (span != null)
                {
                    if (span.Flag == 1 && span.Region == 0 && span.DistToBorder >= dist)
                    {
                        floodedSpans.Add(span);
                    }

                    span = span.Next;
                }
            }

            if (nextRegion > 1)
            {
                ExpandRegions(floodedSpans, dist > 0 ? maxIter : -1);
            }

            foreach (var span in floodedSpans)
            {
                if (span == null || span.Region != 0)
                {
                    continue;
                }

                if (FloodNewRegion(span, Mathf.Max(dist - 2, 0), nextRegion, pendingQueue))
                {
                    nextRegion++;
                }
            }
        }

        ExpandRegions(floodedSpans, maxIter * 8);

        openHeightField.RegionCount = nextRegion;
    }

    private void ExpandRegions(List<OpenHeightSpan> floodedSpans, int maxIter)
    {
        if (floodedSpans.Count == 0)
        {
            return;
        }

        int iter = 0;
        while (true)
        {
            int skip = 0;
            for (int i = 0; i < floodedSpans.Count; i++)
            {
                var span = floodedSpans[i];
                if (span == null || span.Region != 0)
                {
                    skip++;
                    continue;
                }

                int region = 0;
                int distToRegion = int.MaxValue;
                for (int dir = 0; dir < 4; dir++)
                {
                    OpenHeightSpan neighborSpan = span.Neighbors[dir];
                    if (neighborSpan == null)
                    {
                        continue;
                    }

                    if (neighborSpan.Region != 0)
                    {
                        if (neighborSpan.DistToRegion + 2 < distToRegion)
                        {
                            region = neighborSpan.Region;
                            distToRegion = neighborSpan.DistToRegion + 2;
                        }
                    }
                }

                if (region != 0)
                {
                    floodedSpans[i] = null;
                    span.Region = region;
                    span.DistToRegion = distToRegion;
                }
                else
                {
                    skip++;
                }
            }

            if (skip == floodedSpans.Count)
            {
                break;
            }

            if (maxIter != -1)
            {
                iter++;
                if (iter >= maxIter)
                {
                    break;
                }
            }
        }
    }

    private bool FloodNewRegion(OpenHeightSpan span, int dist, int region, Queue<OpenHeightSpan> pendingQueue)
    {
        pendingQueue.Clear();
        pendingQueue.Enqueue(span);
        span.Region = region;
        span.DistToRegion = 0;
        int regionSize = 0;
        while (pendingQueue.Count > 0)
        {
            span = pendingQueue.Dequeue();
            bool isOnRegionBorder = false;
            for (int dir = 0; dir < 4; dir++)
            {
                OpenHeightSpan neighborSpan = span.Neighbors[dir];
                if (neighborSpan == null)
                {
                    continue;
                }

                if (neighborSpan.Region != 0 && neighborSpan.Region != region)
                {
                    isOnRegionBorder = true;
                    break;
                }

                neighborSpan = neighborSpan.Neighbors[(dir + 1) & 0x3];
                if (neighborSpan != null && neighborSpan.Region != 0 && neighborSpan.Region != region)
                {
                    isOnRegionBorder = true;
                    break;
                }
            }

            if (isOnRegionBorder)
            {
                span.Region = 0;
                continue;
            }

            regionSize++;

            for (int dir = 0; dir < 4; dir++)
            {
                OpenHeightSpan neighborSpan = span.Neighbors[dir];
                if (neighborSpan != null && neighborSpan.Flag == 1 && neighborSpan.DistToBorder >= dist && neighborSpan.Region == 0)
                {
                    neighborSpan.Region = region;
                    neighborSpan.DistToRegion = 0;
                    pendingQueue.Enqueue(neighborSpan);
                }
            }
        }

        return regionSize > 0;
    }

    private void FilterRegions(OpenHeightField openHeightField)
    {
        if (openHeightField.RegionCount < 2)
        {
            return;
        }

        Region[] regions = RemoveSmallIslandRegions(openHeightField);
        MergeRegions(openHeightField, regions);
    }

    private Region[] RemoveSmallIslandRegions(OpenHeightField openHeightField)
    {
        Region[] regions = new Region[openHeightField.RegionCount];

        for (int i = 0; i < openHeightField.RegionCount; i++)
        {
            regions[i] = new Region(i);
        }

        foreach (var item in openHeightField.Spans)
        {
            var span = item.Value;
            while (span != null)
            {
                if (span.Region == 0)
                {
                    span = span.Next;
                    continue;
                }

                Region region = regions[span.Region];
                region.SpanCount++;

                var nextSpan = span.Next;
                while (nextSpan != null)
                {
                    if (nextSpan.Region == 0)
                    {
                        nextSpan = nextSpan.Next;
                        continue;
                    }

                    region.Overlaps.Add(nextSpan.Region);

                    nextSpan = nextSpan.Next;
                }

                if (region.Neighbors.Count > 0)
                {
                    span = span.Next;
                    continue;
                }

                int edgeDir = GetRegionEdgeDir(span);
                if (edgeDir != -1)
                {
                    FindRegionNeighbors(span, edgeDir, region.Neighbors);
                }

                span = span.Next;
            }
        }

        for (int regionId = 1; regionId < openHeightField.RegionCount; regionId++)
        {
            Region region = regions[regionId];

            if (region.Neighbors.Count == 1 && region.Neighbors[0] == 0)
            {
                if (region.SpanCount < 64)
                {
                    region.Reset(0);
                }
            }
        }

        return regions;
    }

    private int GetRegionEdgeDir(OpenHeightSpan span)
    {
        for (int dir = 0; dir < 4; dir++)
        {
            OpenHeightSpan neighborSpan = span.Neighbors[dir];
            if (neighborSpan == null || neighborSpan.Region != span.Region)
            {
                return dir;
            }
        }

        return -1;
    }

    private void FindRegionNeighbors(OpenHeightSpan startSpan, int startDir, List<int> neighbors)
    {
        var span = startSpan;
        int dir = startDir;
        int preEdgeRegion = 0;

        OpenHeightSpan neighborSpan = span.Neighbors[dir];
        if (neighborSpan != null)
        {
            preEdgeRegion = neighborSpan.Region;
        }

        neighbors.Add(preEdgeRegion);

        int iter = 0;
        while (++iter < 40000)
        {
            neighborSpan = span.Neighbors[dir];
            int curEdgeRegion = 0;
            if (neighborSpan == null || neighborSpan.Region != span.Region)
            {
                if (neighborSpan != null)
                {
                    curEdgeRegion = neighborSpan.Region;
                }

                if (curEdgeRegion != preEdgeRegion)
                {
                    neighbors.Add(curEdgeRegion);
                    preEdgeRegion = curEdgeRegion;
                }

                dir = (dir + 1) & 0x3;
            }
            else
            {
                span = neighborSpan;
                dir = (dir + 3) & 0x3;
            }

            if (startSpan == span && startDir == dir)
            {
                break;
            }
        }

        if (neighbors.Count > 1 && neighbors[0] == neighbors[neighbors.Count - 1])
        {
            neighbors.RemoveAt(neighbors.Count - 1);
        }
    }

    private void MergeRegions(OpenHeightField openHeightField, Region[] regions)
    {
        int mergeCount;
        do
        {
            mergeCount = 0;
            foreach (var region in regions)
            {
                if (region.ID == 0)
                {
                    continue;
                }

                if (region.SpanCount > 400)
                {
                    continue;
                }

                Region target = null;
                int min = int.MaxValue;
                foreach (var neighbor in region.Neighbors)
                {
                    if (neighbor == 0)
                    {
                        continue;
                    }

                    Region neighborRegion = regions[neighbor];
                    if (neighborRegion.SpanCount < min && CanMerge(region, neighborRegion))
                    {
                        target = neighborRegion;
                        min = neighborRegion.SpanCount;
                    }
                }

                if (target != null && MergeRegions(target, region))
                {
                    var old = region.ID;
                    region.Reset(target.ID);
                    foreach (Region r in regions)
                    {
                        if (r.ID == 0)
                        {
                            continue;
                        }

                        if (r.ID == old)
                        {
                            r.ID = target.ID;
                        }
                        else
                        {
                            ReplaceRegions(r, old, target.ID);
                        }
                    }

                    mergeCount++;
                }
            }
        } while (mergeCount > 0);

        foreach (Region region in regions)
        {
            if (region.ID > 0)
            {
                region.Remapping = true;
            }
        }

        int curRegion = 0;
        foreach (Region region in regions)
        {
            if (!region.Remapping)
            {
                continue;
            }

            curRegion++;
            int old = region.ID;
            foreach (Region r in regions)
            {
                if (r.ID == old)
                {
                    r.ID = curRegion;
                    r.Remapping = false;
                }
            }
        }

        openHeightField.RegionCount = curRegion + 1;

        foreach (var item in openHeightField.Spans)
        {
            var span = item.Value;
            while (span != null)
            {
                if (span.Region == 0)
                {
                    span = span.Next;
                    continue;
                }

                span.Region = regions[span.Region].ID;

                span = span.Next;
            }
        }
    }

    private bool CanMerge(Region region, Region neighborRegion)
    {
        int connectionCount = 0;
        foreach (var neighbor in region.Neighbors)
        {
            if (neighbor == neighborRegion.ID)
            {
                connectionCount++;
            }
        }

        if (connectionCount != 1)
        {
            return false;
        }

        if (region.Overlaps.Contains(neighborRegion.ID))
        {
            return false;
        }

        if (neighborRegion.Overlaps.Contains(region.ID))
        {
            return false;
        }

        return true;
    }

    private bool MergeRegions(Region target, Region candidate)
    {
        int neighborIndexOnTarget = target.Neighbors.IndexOf(candidate.ID);
        if (neighborIndexOnTarget == -1)
        {
            return false;
        }

        int neighborIndexOnCandidate = candidate.Neighbors.IndexOf(target.ID);
        if (neighborIndexOnCandidate == -1)
        {
            return false;
        }

        List<int> targetNeighbors = new List<int>(target.Neighbors);
        target.Neighbors.Clear();

        for (int i = 0; i < targetNeighbors.Count - 1; i++)
        {
            target.Neighbors.Add(targetNeighbors[(neighborIndexOnTarget + 1 + i) % targetNeighbors.Count]);
        }

        for (int i = 0; i < candidate.Neighbors.Count - 1; i++)
        {
            target.Neighbors.Add(candidate.Neighbors[(neighborIndexOnCandidate + 1 + i) % candidate.Neighbors.Count]);
        }

        RemoveAdjacentDuplicateNeighbors(target);

        foreach (var overlap in candidate.Overlaps)
        {
            target.Overlaps.Add(overlap);
        }

        target.SpanCount += candidate.SpanCount;

        return true;
    }

    private void RemoveAdjacentDuplicateNeighbors(Region region)
    {
        int cur = 0;
        while (cur < region.Neighbors.Count && region.Neighbors.Count > 1)
        {
            int next = cur + 1;
            if (next >= region.Neighbors.Count)
            {
                next = 0;
            }

            if (region.Neighbors[cur] == region.Neighbors[next])
            {
                region.Neighbors.RemoveAt(next);
            }
            else
            {
                cur++;
            }
        }
    }

    private void ReplaceRegions(Region region, int old, int @new)
    {
        bool change = false;
        for (int i = 0; i < region.Neighbors.Count; i++)
        {
            if (region.Neighbors[i] == old)
            {
                region.Neighbors[i] = @new;
                change = true;
            }
        }

        if (region.Overlaps.Contains(old))
        {
            region.Overlaps.Remove(old);
            region.Overlaps.Add(@new);
        }

        if (change)
        {
            RemoveAdjacentDuplicateNeighbors(region);
        }
    }

    private void UpdateGUI(OpenHeightField openHeightField)
    {
#if UNITY_EDITOR
        openHeightField.UpdateDrawColors();
#endif
    }
}