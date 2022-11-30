using System.Collections.Generic;
using UnityEngine;

public class ContourSetBuilder
{
    private static List<Vector4Int> originVerts = new List<Vector4Int>(64);
    private static List<Vector4Int> simplifiedVerts = new List<Vector4Int>(16);

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

                if (simplifiedVerts.Count >= 3)
                {
                    contourSet.AddContour(new Contour(span.Region, originVerts, simplifiedVerts));
                }

                span = span.Next;
            }
        }

        MergeHoles(contourSet);

        return contourSet;
    }

    private void GenerateOriginContour(OpenHeightSpan startSpan, int startWidthIdx, int startDepthIdx, int startDir, List<Vector4Int> originVerts)
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

                originVerts.Add(new Vector4Int(px, py, pz, neighborRegion));

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

    private void GenerateSimplifiedContour(List<Vector4Int> originVerts, List<Vector4Int> simplifiedVerts)
    {
        bool island = true;

        for (int i = 0; i < originVerts.Count; i++)
        {
            if (originVerts[i].w != 0)
            {
                island = false;
                break;
            }
        }

        if (island)
        {
            Vector4Int ld = originVerts[0];
            ld.w = 0;
            Vector4Int rt = originVerts[0];
            rt.w = 0;
            for (int i = 1; i < originVerts.Count; i++)
            {
                Vector4Int vert = originVerts[i];
                if (vert.x < ld.x || vert.x == ld.x && vert.z < ld.z)
                {
                    ld = vert;
                    ld.w = i;
                }

                if (vert.x > rt.x || vert.x == rt.x && vert.z > rt.z)
                {
                    rt = vert;
                    rt.w = i;
                }
            }

            simplifiedVerts.Add(ld);
            simplifiedVerts.Add(rt);
        }
        else
        {
            for (int i = 0; i < originVerts.Count; i++)
            {
                if (originVerts[i].w != originVerts[(i + 1) % originVerts.Count].w)
                {
                    Vector4Int vert = originVerts[i];
                    vert.w = i;
                    simplifiedVerts.Add(vert);
                }
            }
        }

        Douglas_Peucker(originVerts, simplifiedVerts);
        SplitTooLongEdges(originVerts, simplifiedVerts);

        int originCount = originVerts.Count;
        int simplifiedCount = simplifiedVerts.Count;
        for (int i = 0; i < simplifiedCount; i++)
        {
            int originIndex = (simplifiedVerts[i].w + 1) % originCount;
            Vector4Int vert = simplifiedVerts[i];
            vert.w = originVerts[originIndex].w;
            simplifiedVerts[i] = vert;
        }
    }

    private void Douglas_Peucker(List<Vector4Int> originVerts, List<Vector4Int> simplifiedVerts)
    {
        int originCount = originVerts.Count;
        int simplifiedCount = simplifiedVerts.Count;
        int start = 0;

        while (start < simplifiedCount)
        {
            int end = (start + 1) % simplifiedCount;

            Vector2Int s = simplifiedVerts[start];
            int si = simplifiedVerts[start].w;

            Vector2Int e = simplifiedVerts[end];
            int ei = simplifiedVerts[end].w;

            int curIndex = (si + 1) % originCount;
            float maxDistSq = 0;

            int insertIndex = -1;

            if (originVerts[curIndex].w == 0)
            {
                while (curIndex != ei)
                {
                    float distSq = MathTool.GetPointToSegmentDistSq(s, e, originVerts[curIndex]);
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
                Vector4Int vert = originVerts[insertIndex];
                vert.w = insertIndex;
                simplifiedVerts.Insert(start + 1, vert);
                simplifiedCount = simplifiedVerts.Count;
            }
            else
            {
                start++;
            }
        }
    }

    private void SplitTooLongEdges(List<Vector4Int> originVerts, List<Vector4Int> simplifiedVerts)
    {
        int originCount = originVerts.Count;
        int simplifiedCount = simplifiedVerts.Count;
        int start = 0;

        while (start < simplifiedCount)
        {
            int end = (start + 1) % simplifiedCount;

            Vector2Int s = simplifiedVerts[start];
            int si = simplifiedVerts[start].w;

            Vector2Int e = simplifiedVerts[end];
            int ei = simplifiedVerts[end].w;

            int insertIndex = -1;

            int curIndex = (start + 1) % originCount;

            if (originVerts[curIndex].w == 0)
            {
                int distSq = (e - s).sqrMagnitude;
                if (distSq > 12.0f * 12.0f)
                {
                    int indexDist = ei < si ? ei + (originCount - si) : ei - si;
                    insertIndex = (si + indexDist / 2) % originCount;
                }
            }

            if (insertIndex != -1)
            {
                Vector4Int vert = originVerts[insertIndex];
                vert.w = insertIndex;
                simplifiedVerts.Insert(start + 1, vert);
                simplifiedCount = simplifiedVerts.Count;
            }
            else
            {
                start++;
            }
        }
    }

    private void RemoveVerticalSegments(List<Vector4Int> simplifiedVerts)
    {
        for (int i = 0; i < simplifiedVerts.Count;)
        {
            int next = (i + 1) % simplifiedVerts.Count;
            if ((Vector2Int) simplifiedVerts[i] == (Vector2Int) simplifiedVerts[next])
            {
                simplifiedVerts.RemoveAt(i);
            }
            else
            {
                i++;
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

        foreach (var hole in holes)
        {
            foreach (var contour in contourSet.Contours)
            {
                if (hole.Region == contour.Region)
                {
                    contour.Holes.Add(hole);
                    SetLeftDownIndex(hole);
                    break;
                }
            }
        }

        foreach (var contour in contourSet.Contours)
        {
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

            for (int i = 0; i < contour.Holes.Count; i++)
            {
                var hole = contour.Holes[i];
                int holeIndex = hole.LDi;
                int contourIndex = -1;
                for (int iter = 0; iter < hole.SimplifiedVerts.Count; iter++)
                {
                    hole.HoleToContours.Clear();
                    Vector2Int p = hole.SimplifiedVerts[holeIndex];
                    for (int j = 0; j < contour.SimplifiedVerts.Count; j++)
                    {
                        int pre = j == 0 ? contour.SimplifiedVerts.Count - 1 : j - 1;
                        int next = j == contour.SimplifiedVerts.Count - 1 ? 0 : j + 1;
                        Vector2Int a = contour.SimplifiedVerts[j];
                        Vector2Int b = contour.SimplifiedVerts[pre];
                        Vector2Int c = contour.SimplifiedVerts[next];
                        if (MathTool.InCone(a, b, c, p))
                        {
                            hole.HoleToContours.Add(new Contour.HoleToContour {ContourIndex = j, Dist = (a - p).sqrMagnitude});
                        }
                    }

                    hole.HoleToContours.Sort((x, y) => x.Dist - y.Dist);

                    contourIndex = -1;

                    foreach (var holeToContour in hole.HoleToContours)
                    {
                        bool intersect = MathTool.IntersectCountour(p, contour.SimplifiedVerts[holeToContour.ContourIndex], holeToContour.ContourIndex, contour.SimplifiedVerts);
                        //轮廓已经与之前的空洞相连 只需检测剩下的空洞
                        for (int j = i; j < contour.Holes.Count && !intersect; j++)
                        {
                            intersect |= MathTool.IntersectCountour(p, contour.SimplifiedVerts[holeToContour.ContourIndex], holeIndex, contour.Holes[j].SimplifiedVerts);
                        }

                        if (!intersect)
                        {
                            contourIndex = holeToContour.ContourIndex;
                            break;
                        }
                    }

                    if (contourIndex != -1)
                    {
                        break;
                    }

                    holeIndex = (holeIndex + 1) % hole.SimplifiedVerts.Count;
                }

                if (contourIndex != -1)
                {
                    simplifiedVerts.Clear();
                    simplifiedVerts.AddRange(contour.SimplifiedVerts);
                    contour.SimplifiedVerts.Clear();
                    for (int j = 0; j <= simplifiedVerts.Count; j++)
                    {
                        contour.SimplifiedVerts.Add(simplifiedVerts[(j + contourIndex) % simplifiedVerts.Count]);
                    }

                    for (int j = 0; j <= hole.SimplifiedVerts.Count; j++)
                    {
                        contour.SimplifiedVerts.Add(hole.SimplifiedVerts[(j + holeIndex) % hole.SimplifiedVerts.Count]);
                    }
                }
            }

            contour.Holes.Clear();
        }
    }

    private void SetLeftDownIndex(Contour hole)
    {
        Vector4Int ld = hole.SimplifiedVerts[0];
        ld.w = 0;
        for (int i = 1; i < hole.SimplifiedVerts.Count; i++)
        {
            Vector4Int vert = hole.SimplifiedVerts[i];
            if (vert.x < ld.x || vert.x == ld.x && vert.z < ld.z)
            {
                ld = vert;
                ld.w = i;
            }
        }

        hole.LDi = ld.w;
    }
}