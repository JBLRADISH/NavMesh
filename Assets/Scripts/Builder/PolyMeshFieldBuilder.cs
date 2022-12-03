using System;
using System.Collections.Generic;
using UnityEngine;

public class PolyMeshFieldBuilder
{
    public PolyMeshField Build(ContourSet contourSet)
    {
        PolyMeshField polyMeshField = new PolyMeshField(contourSet);

        int totalVertCount = 0;
        int totalPolyCount = 0;
        int maxVertCount = 0;
        int maxTriCount = 0;

        foreach (var contour in contourSet.Contours)
        {
            int vertCount = contour.SimplifiedVerts.Count;
            totalVertCount += vertCount;
            totalPolyCount += vertCount - 2;
            maxVertCount = Mathf.Max(maxVertCount, vertCount);
        }

        maxTriCount = Mathf.Max(maxVertCount - 2, maxTriCount);

        List<Vector4Int> globalVerts = new List<Vector4Int>(totalVertCount);
        Dictionary<Vector4Int, int> globalIndices = new Dictionary<Vector4Int, int>();
        List<int> globalPolys = new List<int>(totalPolyCount * Config.MaxVertCountInPoly);
        List<int> globalRegions = new List<int>(totalPolyCount);

        List<int> idxs = new List<int>(maxVertCount);
        List<int> tris = new List<int>(maxVertCount);
        int[] contour2GlobalIndices = new int[maxVertCount];
        int[] polys = new int[maxTriCount * Config.MaxVertCountInPoly];

        MergeInfo mergeInfo = new MergeInfo();
        int[] mergePoly = new int[Config.MaxVertCountInPoly];

        foreach (var contour in contourSet.Contours)
        {
            idxs.Clear();
            for (int i = 0; i < contour.SimplifiedVerts.Count; i++)
            {
                idxs.Add(i);
            }

            tris.Clear();

            int triCount = Triangulate(contour.SimplifiedVerts, idxs, tris);
            if (triCount <= 0)
            {
                continue;
            }

            for (int i = 0; i < contour.SimplifiedVerts.Count; i++)
            {
                var vert = contour.SimplifiedVerts[i];
                if (!globalIndices.TryGetValue(vert, out int index))
                {
                    index = globalVerts.Count;
                    globalIndices.Add(vert, index);
                    globalVerts.Add(vert);
                }

                contour2GlobalIndices[i] = index;
            }

            for (int i = 0; i < polys.Length; i++)
            {
                polys[i] = -1;
            }

            for (int i = 0; i < triCount; i++)
            {
                polys[i * Config.MaxVertCountInPoly] = contour2GlobalIndices[tris[i * 3]];
                polys[i * Config.MaxVertCountInPoly + 1] = contour2GlobalIndices[tris[i * 3 + 1]];
                polys[i * Config.MaxVertCountInPoly + 2] = contour2GlobalIndices[tris[i * 3 + 2]];
            }

            int polyCount = triCount;

            if (Config.MaxVertCountInPoly > 3)
            {
                while (true)
                {
                    int maxLenSq = -1;
                    int api = -1;
                    int avi = -1;
                    int bpi = -1;
                    int bvi = -1;

                    for (int i = 0; i < polyCount - 1; i++)
                    {
                        for (int j = i + 1; j < polyCount; j++)
                        {
                            GetPolyMergeInfo(i * Config.MaxVertCountInPoly, j * Config.MaxVertCountInPoly, polys, globalVerts, ref mergeInfo);
                            if (mergeInfo.lenSq > maxLenSq)
                            {
                                maxLenSq = mergeInfo.lenSq;
                                api = i * Config.MaxVertCountInPoly;
                                avi = mergeInfo.avi;
                                bpi = j * Config.MaxVertCountInPoly;
                                bvi = mergeInfo.bvi;
                            }
                        }
                    }

                    if (maxLenSq <= 0)
                    {
                        break;
                    }

                    for (int i = 0; i < mergePoly.Length; i++)
                    {
                        mergePoly[i] = -1;
                    }

                    int apc = GetPolyVertCount(api, polys);
                    int bpc = GetPolyVertCount(bpi, polys);
                    int pos = 0;

                    for (int i = 0; i < apc - 1; i++)
                    {
                        mergePoly[pos++] = polys[api + (avi + 1 + i) % apc];
                    }

                    for (int i = 0; i < bpc - 1; i++)
                    {
                        mergePoly[pos++] = polys[bpi + (bvi + 1 + i) % bpc];
                    }

                    Array.Copy(mergePoly, 0, polys, api, Config.MaxVertCountInPoly);
                    Array.Copy(polys, bpi + Config.MaxVertCountInPoly, polys, bpi, polys.Length - bpi - Config.MaxVertCountInPoly);
                    polyCount--;
                }
            }

            for (int i = 0; i < polyCount; i++)
            {
                for (int j = 0; j < Config.MaxVertCountInPoly; j++)
                {
                    globalPolys.Add(polys[i * Config.MaxVertCountInPoly + j]);
                }

                globalRegions.Add(contour.Region);
            }
        }

        polyMeshField.Verts = globalVerts.ToArray();

        int globalPolyCount = globalRegions.Count;
        polyMeshField.Polys = new int[globalPolys.Count * 2];
        for (int i = 0; i < globalPolyCount; i++)
        {
            int ppi = i * Config.MaxVertCountInPoly;
            for (int pvi = 0; pvi < Config.MaxVertCountInPoly; pvi++)
            {
                polyMeshField.Polys[ppi * 2 + pvi] = globalPolys[ppi + pvi];
                polyMeshField.Polys[ppi * 2 + Config.MaxVertCountInPoly + pvi] = -1;
            }
        }

        polyMeshField.Regions = globalRegions.ToArray();

        BuildAdjacencyData(polyMeshField);

        return polyMeshField;
    }

    private const int FLAG = 0x40000000;
    private const int DEFLAG = 0x3FFFFFFF;

    private int Triangulate(List<Vector4Int> verts, List<int> idxs, List<int> tris)
    {
        for (int i = 0; i < idxs.Count; i++)
        {
            int bi = Neighbor(i, idxs.Count, true);
            int ci = Neighbor(i, idxs.Count, false);
            int pi = Neighbor(ci, idxs.Count, false);
            if (MathTool.InCone(i, bi, ci, pi, verts, idxs) && !MathTool.IntersectCountour(i, pi, verts, idxs))
            {
                idxs[ci] |= FLAG;
            }
        }

        while (idxs.Count > 3)
        {
            int i = -1;
            int next = -1;
            int ai = -1;
            int pi = -1;
            int minLenSq = -1;
            int minIndex = -1;

            for (i = 0; i < idxs.Count; i++)
            {
                next = Neighbor(i, idxs.Count, false);
                if ((idxs[next] & FLAG) == FLAG)
                {
                    ai = idxs[i] & DEFLAG;
                    pi = idxs[Neighbor(next, idxs.Count, false)] & DEFLAG;

                    int lenSq = ((Vector2Int) verts[pi] - verts[ai]).sqrMagnitude;

                    if (minLenSq < 0 || lenSq < minLenSq)
                    {
                        minLenSq = lenSq;
                        minIndex = i;
                    }
                }
            }

            if (minLenSq == -1)
            {
                return tris.Count / 3;
            }

            i = minIndex;
            next = Neighbor(i, idxs.Count, false);

            tris.Add(idxs[i] & DEFLAG);
            tris.Add(idxs[next] & DEFLAG);
            tris.Add(idxs[Neighbor(next, idxs.Count, false)] & DEFLAG);

            idxs.RemoveAt(next);

            if (next == 0 || next >= idxs.Count)
            {
                i = idxs.Count - 1;
                next = 0;
            }

            ai = Neighbor(i, idxs.Count, true);
            int bi = Neighbor(ai, idxs.Count, true);
            pi = Neighbor(next, idxs.Count, false);

            if (MathTool.InCone(ai, bi, i, next, verts, idxs) && !MathTool.IntersectCountour(ai, next, verts, idxs))
            {
                idxs[i] |= FLAG;
            }
            else
            {
                idxs[i] &= DEFLAG;
            }

            if (MathTool.InCone(i, ai, next, pi, verts, idxs) && !MathTool.IntersectCountour(i, pi, verts, idxs))
            {
                idxs[next] |= FLAG;
            }
            else
            {
                idxs[next] &= DEFLAG;
            }
        }

        tris.Add(idxs[0] & DEFLAG);
        tris.Add(idxs[1] & DEFLAG);
        tris.Add(idxs[2] & DEFLAG);

        return tris.Count / 3;
    }

    private int Neighbor(int i, int n, bool pre)
    {
        if (pre)
        {
            return i == 0 ? n - 1 : i - 1;
        }
        else
        {
            return i == n - 1 ? 0 : i + 1;
        }
    }

    private struct MergeInfo
    {
        public int avi;
        public int bvi;
        public int lenSq;
    }

    private void GetPolyMergeInfo(int api, int bpi, int[] polys, List<Vector4Int> verts, ref MergeInfo mergeInfo)
    {
        mergeInfo.avi = -1;
        mergeInfo.bvi = -1;
        mergeInfo.lenSq = -1;

        int apc = GetPolyVertCount(api, polys);
        int bpc = GetPolyVertCount(bpi, polys);

        if (apc + bpc - 2 > Config.MaxVertCountInPoly)
        {
            return;
        }

        for (int avi = 0; avi < apc; avi++)
        {
            int avc = polys[api + avi];
            int avn = polys[api + Neighbor(avi, apc, false)];

            for (int bvi = 0; bvi < bpc; bvi++)
            {
                int bvc = polys[bpi + bvi];
                int bvn = polys[bpi + Neighbor(bvi, bpc, false)];
                if (avc == bvn && avn == bvc)
                {
                    mergeInfo.avi = avi;
                    mergeInfo.bvi = bvi;
                    break;
                }
            }
        }

        if (mergeInfo.avi == -1)
        {
            return;
        }

        int sharePreIndex = polys[api + Neighbor(mergeInfo.avi, apc, true)];
        int shareIndex = polys[api + mergeInfo.avi];
        int shareNextIndex = polys[bpi + (mergeInfo.bvi + 2) % bpc];
        if (MathTool.RightOrOn(verts[sharePreIndex], verts[shareNextIndex], verts[shareIndex]))
        {
            return;
        }

        sharePreIndex = polys[bpi + Neighbor(mergeInfo.bvi, bpc, true)];
        shareIndex = polys[bpi + mergeInfo.bvi];
        shareNextIndex = polys[api + (mergeInfo.avi + 2) % apc];
        if (MathTool.RightOrOn(verts[sharePreIndex], verts[shareNextIndex], verts[shareIndex]))
        {
            return;
        }

        sharePreIndex = polys[api + mergeInfo.avi];
        shareIndex = polys[bpi + mergeInfo.bvi];

        mergeInfo.lenSq = ((Vector2Int) verts[shareIndex] - verts[sharePreIndex]).sqrMagnitude;
    }

    private int GetPolyVertCount(int ppi, int[] polys)
    {
        for (int i = 0; i < Config.MaxVertCountInPoly; i++)
        {
            if (polys[ppi + i] == -1)
            {
                return i;
            }
        }

        return Config.MaxVertCountInPoly;
    }

    private void BuildAdjacencyData(PolyMeshField polyMeshField)
    {
        int vertCount = polyMeshField.Verts.Length;
        int polyCount = polyMeshField.Regions.Length;
        int maxEdgeCount = polyCount * Config.MaxVertCountInPoly;

        int[] edges = new int[maxEdgeCount * 6];
        int edgeCount = 0;

        int[] startEdge = new int[vertCount];
        for (int i = 0; i < startEdge.Length; i++)
        {
            startEdge[i] = -1;
        }

        int[] nextEdge = new int[maxEdgeCount];

        for (int i = 0; i < polyCount; i++)
        {
            int ppi = i * Config.MaxVertCountInPoly * 2;
            for (int pvi = 0; pvi < Config.MaxVertCountInPoly; pvi++)
            {
                int pvc = polyMeshField.Polys[ppi + pvi];
                if (pvc == -1)
                {
                    break;
                }

                int pvn;
                if (pvi + 1 >= Config.MaxVertCountInPoly || polyMeshField.Polys[ppi + pvi + 1] == -1)
                {
                    pvn = polyMeshField.Polys[ppi];
                }
                else
                {
                    pvn = polyMeshField.Polys[ppi + pvi + 1];
                }

                if (pvc < pvn)
                {
                    edges[edgeCount * 6] = pvc;
                    edges[edgeCount * 6 + 1] = pvn;
                    edges[edgeCount * 6 + 2] = i;
                    edges[edgeCount * 6 + 3] = pvi;
                    edges[edgeCount * 6 + 4] = -1;
                    edges[edgeCount * 6 + 5] = -1;

                    nextEdge[edgeCount] = startEdge[pvc];
                    startEdge[pvc] = edgeCount;
                    edgeCount++;
                }
            }
        }

        for (int i = 0; i < polyCount; i++)
        {
            int ppi = i * Config.MaxVertCountInPoly * 2;
            for (int pvi = 0; pvi < Config.MaxVertCountInPoly; pvi++)
            {
                int pvc = polyMeshField.Polys[ppi + pvi];
                if (pvc == -1)
                {
                    break;
                }

                int pvn;
                if (pvi + 1 >= Config.MaxVertCountInPoly || polyMeshField.Polys[ppi + pvi + 1] == -1)
                {
                    pvn = polyMeshField.Polys[ppi];
                }
                else
                {
                    pvn = polyMeshField.Polys[ppi + pvi + 1];
                }

                if (pvc > pvn)
                {
                    for (int edgeIndex = startEdge[pvn]; edgeIndex != -1; edgeIndex = nextEdge[edgeIndex])
                    {
                        if (edges[edgeIndex * 6 + 1] == pvc)
                        {
                            edges[edgeIndex * 6 + 4] = i;
                            edges[edgeIndex * 6 + 5] = pvi;
                            break;
                        }
                    }
                }
            }
        }

        for (int i = 0; i < edgeCount * 6; i += 6)
        {
            if (edges[i + 4] != -1)
            {
                int api = edges[i + 2] * Config.MaxVertCountInPoly * 2;
                int avi = edges[i + 3];
                int bpi = edges[i + 4] * Config.MaxVertCountInPoly * 2;
                int bvi = edges[i + 5];
                polyMeshField.Polys[api + Config.MaxVertCountInPoly + avi] = edges[i + 4];
                polyMeshField.Polys[bpi + Config.MaxVertCountInPoly + bvi] = edges[i + 2];
            }
        }
    }
}