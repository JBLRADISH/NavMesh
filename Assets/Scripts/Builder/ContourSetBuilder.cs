using System.Collections.Generic;
using UnityEngine;

public class ContourSetBuilder
{
    private static List<int> originVerts = new List<int>(256);
    private static List<int> simplifiedVerts = new List<int>(64);

    public ContourSet Build(OpenHeightField openHeightField)
    {
        ContourSet contourSet = new ContourSet(openHeightField);

        foreach (var item in openHeightField.Spans)
        {
            var span = item.Value;
            while (span != null)
            {
                span.Flag = 0;
                if (span.Region == 0)
                {
                    span = span.Next;
                    continue;
                }

                for (int dir = 0; dir < 4; dir++)
                {
                    int neighborRegion = 0;
                    var neighborSpan = span.Neighbors[dir];
                    if (neighborSpan != null)
                    {
                        neighborRegion = neighborSpan.Region;
                    }

                    if (span.Region != neighborRegion)
                    {
                        span.Flag |= 1 << dir;
                    }
                }

                //一个span的孤岛区域无法形成轮廓
                if (span.Flag == 0xf)
                {
                    span.Flag = 0;
                }

                span = span.Next;
            }
        }

        foreach (var item in openHeightField.Spans)
        {
            var span = item.Value;
            while (span != null)
            {
                if (span.Flag == 0)
                {
                    span = span.Next;
                    continue;
                }

                originVerts.Clear();
                simplifiedVerts.Clear();
                int startDir = 0;
                while ((span.Flag & (1 << startDir)) == 0)
                {
                    startDir++;
                }

                int widthIndex = item.Key / openHeightField.Depth;
                int depthIndex = item.Key % openHeightField.Depth;

                GenerateOriginContour(span, widthIndex, depthIndex, startDir, originVerts);

                GenerateSimplifiedContour(originVerts, simplifiedVerts);

                RemoveVerticalSegments(simplifiedVerts);

                if (simplifiedVerts.Count >= 12)
                {
                    contourSet.AddContour(new Contour(span.Region, originVerts, simplifiedVerts));
                }

                span = span.Next;
            }
        }

        return contourSet;
    }

    private void GenerateOriginContour(OpenHeightSpan startSpan, int startWidthIdx, int startDepthIdx, int startDir, List<int> originVerts)
    {
        OpenHeightSpan span = startSpan;
        int dir = startDir;
        int spanX = startWidthIdx;
        int spanZ = startDepthIdx;

        int iter = 0;
        while (++iter < 40000)
        {
            if ((span.Flag & (1 << dir)) != 0)
            {
                int px = spanX;
                int py = GetCornerHeight(span, dir);
                int pz = spanZ;
                switch (dir)
                {
                    case 0:
                        pz++;
                        break;
                    case 1:
                        px++;
                        pz++;
                        break;
                    case 2:
                        px++;
                        break;
                }

                int neighborRegion = 0;
                var neighborSpan = span.Neighbors[dir];
                if (neighborSpan != null)
                {
                    neighborRegion = neighborSpan.Region;
                }

                originVerts.Add(px);
                originVerts.Add(py);
                originVerts.Add(pz);
                originVerts.Add(neighborRegion);

                span.Flag &= ~(1 << dir);
                dir = (dir + 1) & 0x3;
            }
            else
            {
                span = span.Neighbors[dir];

                switch (dir)
                {
                    case 0:
                        spanX--;
                        break;
                    case 1:
                        spanZ++;
                        break;
                    case 2:
                        spanX++;
                        break;
                    case 3:
                        spanZ--;
                        break;
                }

                dir = (dir + 3) & 0x3;
            }

            if (span == startSpan && dir == startDir)
            {
                break;
            }
        }
    }

    private int GetCornerHeight(OpenHeightSpan span, int dir)
    {
        int maxFloor = span.Floor;

        OpenHeightSpan diagonalSpan = null;

        int dirOffset = (dir + 1) & 0x3;

        var neighborSpan = span.Neighbors[dir];
        if (neighborSpan != null)
        {
            maxFloor = Mathf.Max(maxFloor, neighborSpan.Floor);
            diagonalSpan = neighborSpan.Neighbors[dirOffset];
        }

        neighborSpan = span.Neighbors[dirOffset];
        if (neighborSpan != null)
        {
            maxFloor = Mathf.Max(maxFloor, neighborSpan.Floor);
            diagonalSpan ??= neighborSpan.Neighbors[dir];
        }

        if (diagonalSpan != null)
        {
            maxFloor = Mathf.Max(maxFloor, diagonalSpan.Floor);
        }

        return maxFloor;
    }

    private void GenerateSimplifiedContour(List<int> originVerts, List<int> simplifiedVerts)
    {
        bool island = true;

        for (int i = 0; i < originVerts.Count; i += 4)
        {
            if (originVerts[i + 3] != 0)
            {
                island = false;
                break;
            }
        }

        if (island)
        {
            int ldx = originVerts[0];
            int ldy = originVerts[1];
            int ldz = originVerts[2];
            int ldi = 0;
            int rtx = originVerts[0];
            int rty = originVerts[1];
            int rtz = originVerts[2];
            int rti = 0;
            for (int i = 4; i < originVerts.Count; i += 4)
            {
                int x = originVerts[i];
                int y = originVerts[i + 1];
                int z = originVerts[i + 2];
                if (x < ldx || (x == ldx && z < ldz))
                {
                    ldx = x;
                    ldy = y;
                    ldz = z;
                    ldi = i / 4;
                }

                if (x > rtx || (x == rtx && z > rtz))
                {
                    rtx = x;
                    rty = y;
                    rtz = z;
                    rti = i / 4;
                }
            }

            simplifiedVerts.Add(ldx);
            simplifiedVerts.Add(ldy);
            simplifiedVerts.Add(ldz);
            simplifiedVerts.Add(ldi);

            simplifiedVerts.Add(rtx);
            simplifiedVerts.Add(rty);
            simplifiedVerts.Add(rtz);
            simplifiedVerts.Add(rti);
        }
        else
        {
            for (int i = 0; i < originVerts.Count; i += 4)
            {
                if (originVerts[i + 3] != originVerts[(i + 4) % originVerts.Count + 3])
                {
                    simplifiedVerts.Add(originVerts[i]);
                    simplifiedVerts.Add(originVerts[i + 1]);
                    simplifiedVerts.Add(originVerts[i + 2]);
                    simplifiedVerts.Add(i / 4);
                }
            }
        }

        Douglas_Peucker(originVerts, simplifiedVerts);
        SplitTooLongEdges(originVerts, simplifiedVerts);

        int originCount = originVerts.Count / 4;
        int simplifiedCount = simplifiedVerts.Count / 4;
        for (int i = 0; i < simplifiedCount; i++)
        {
            int originIndex = (simplifiedVerts[i * 4 + 3] + 1) % originCount;
            simplifiedVerts[i * 4 + 3] = originVerts[originIndex * 4 + 3];
        }
    }

    private void Douglas_Peucker(List<int> originVerts, List<int> simplifiedVerts)
    {
        int originCount = originVerts.Count / 4;
        int simplifiedCount = simplifiedVerts.Count / 4;
        int start = 0;

        while (start < simplifiedCount)
        {
            int end = (start + 1) % simplifiedCount;

            int sx = simplifiedVerts[start * 4];
            int sz = simplifiedVerts[start * 4 + 2];
            int si = simplifiedVerts[start * 4 + 3];

            int ex = simplifiedVerts[end * 4];
            int ez = simplifiedVerts[end * 4 + 2];
            int ei = simplifiedVerts[end * 4 + 3];

            int curIndex = (si + 1) % originCount;
            float maxDistSq = 0;

            int insertIndex = -1;

            if (originVerts[curIndex * 4 + 3] == 0)
            {
                while (curIndex != ei)
                {
                    float distSq = MathTool.GetPointToSegmentDistSq(originVerts[curIndex * 4], originVerts[curIndex * 4 + 2], sx, sz, ex, ez);
                    if (distSq > maxDistSq)
                    {
                        maxDistSq = distSq;
                        insertIndex = curIndex;
                    }

                    curIndex = (curIndex + 1) % originCount;
                }
            }

            if (insertIndex != -1 && maxDistSq > 1.3f * 1.3f)
            {
                simplifiedVerts.Insert((start + 1) * 4, originVerts[insertIndex * 4]);
                simplifiedVerts.Insert((start + 1) * 4 + 1, originVerts[insertIndex * 4 + 1]);
                simplifiedVerts.Insert((start + 1) * 4 + 2, originVerts[insertIndex * 4 + 2]);
                simplifiedVerts.Insert((start + 1) * 4 + 3, insertIndex);
                simplifiedCount = simplifiedVerts.Count / 4;
            }
            else
            {
                start++;
            }
        }
    }

    private void SplitTooLongEdges(List<int> originVerts, List<int> simplifiedVerts)
    {
        int originCount = originVerts.Count / 4;
        int simplifiedCount = simplifiedVerts.Count / 4;
        int start = 0;

        while (start < simplifiedCount)
        {
            int end = (start + 1) % simplifiedCount;

            int sx = simplifiedVerts[start * 4];
            int sz = simplifiedVerts[start * 4 + 2];
            int si = simplifiedVerts[start * 4 + 3];

            int ex = simplifiedVerts[end * 4];
            int ez = simplifiedVerts[end * 4 + 2];
            int ei = simplifiedVerts[end * 4 + 3];

            int insertIndex = -1;

            int curIndex = (start + 1) % originCount;

            if (originVerts[curIndex * 4 + 3] == 0)
            {
                int dx = ex - sx;
                int dz = ez - sz;
                if (dx * dx + dz * dz > 12.0f * 12.0f)
                {
                    int indexDist = ei < si ? ei + (originCount - si) : ei - si;
                    insertIndex = (si + indexDist / 2) % originCount;
                }
            }

            if (insertIndex != -1)
            {
                simplifiedVerts.Insert((start + 1) * 4, originVerts[insertIndex * 4]);
                simplifiedVerts.Insert((start + 1) * 4 + 1, originVerts[insertIndex * 4 + 1]);
                simplifiedVerts.Insert((start + 1) * 4 + 2, originVerts[insertIndex * 4 + 2]);
                simplifiedVerts.Insert((start + 1) * 4 + 3, insertIndex);
                simplifiedCount = simplifiedVerts.Count / 4;
            }
            else
            {
                start++;
            }
        }
    }

    private void RemoveVerticalSegments(List<int> simplifiedVerts)
    {
        for (int i = 0; i < simplifiedVerts.Count;)
        {
            int next = (i + 4) % simplifiedVerts.Count;
            if (simplifiedVerts[i] == simplifiedVerts[next] && simplifiedVerts[i + 2] == simplifiedVerts[next + 2])
            {
                simplifiedVerts.Remove(i);
                simplifiedVerts.Remove(i);
                simplifiedVerts.Remove(i);
                simplifiedVerts.Remove(i);
            }
            else
            {
                i += 4;
            }
        }
    }

    private void MergeHoles(ContourSet contourSet)
    {
        List<Contour> holes = new List<Contour>();
        foreach (var contour in contourSet.Contours)
        {
            int area = MathTool.GetContourSignedArea(contour.SimplifiedVerts);
            //空洞顶点是逆时针顺序(有向面积>0)
            if (area > 0)
            {
                holes.Add(contour);
            }
        }

        contourSet.Contours.RemoveAll(x => holes.Contains(x));

        foreach (var contour in contourSet.Contours)
        {
            foreach (var hole in holes)
            {
                if (contour.Region == hole.Region)
                {
                    contour.Holes.Add(hole);
                    SetLeftDownIndex(hole);
                }
            }

            contour.Holes.Sort((x, y) =>
            {
                if (x.LDx == y.LDx)
                {
                    return x.LDz - y.LDz;
                }
                else
                {
                    return x.LDx - y.LDx;
                }
            });

            foreach (var hole in contour.Holes)
            {
                int cur = hole.LDi;
                for (int i = 0; i < hole.SimplifiedCount; i++)
                {

                }
            }
        }
    }

    private void SetLeftDownIndex(Contour hole)
    {
        int ldx = hole.SimplifiedVerts[0];
        int ldz = hole.SimplifiedVerts[2];
        int ldi = 0;
        for (int i = 1; i < hole.SimplifiedCount; i++)
        {
            int x = hole.SimplifiedVerts[i * 4];
            int z = hole.SimplifiedVerts[i * 4 + 2];
            if (x < ldx || x == ldx && z < ldz)
            {
                ldi = i;
            }
        }

        hole.LDi = ldi;
    }
}