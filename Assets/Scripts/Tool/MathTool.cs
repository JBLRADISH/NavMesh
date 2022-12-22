using System.Collections.Generic;
using UnityEngine;

public static class MathTool
{
    public static double Epsilon = 1e-6;

    public static Vector3 Normal(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 p1p2 = p2 - p1;
        Vector3 p1p3 = p3 - p1;
        Vector3 n = Vector3.Cross(p1p2, p1p3).normalized;
        return n;
    }

    public static int CeilToInt(float f)
    {
        int floor = Mathf.FloorToInt(f);
        if (Approximately(floor, f))
        {
            return floor;
        }
        else
        {
            return Mathf.CeilToInt(f);
        }
    }

    public static int FloorToInt(float f)
    {
        int ceil = Mathf.CeilToInt(f);
        if (Approximately(ceil, f))
        {
            return ceil;
        }
        else
        {
            return Mathf.FloorToInt(f);
        }
    }

    public static bool Approximately(float a, float b)
    {
        if (a >= b - Epsilon && a <= b + Epsilon)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static float GetPointToSegmentDistSq(Vector2Int a, Vector2Int b, Vector2Int p)
    {
        Vector2Int ab = b - a;
        Vector2Int ap = p - a;

        float abLenSq = ab.sqrMagnitude;

        if (abLenSq == 0)
        {
            return ap.sqrMagnitude;
        }

        //先计算AP在AB上投影长度(点乘) |AP| * |AB| * cosθ = AP.AB 投影长度(|AP| * cosθ) = AP.AB / |AB|
        //t = 投影长度/|AB| = AP.AB / |AB| * |AB|
        float t = (ap.x * ab.x + ap.y * ab.y) / abLenSq;

        t = Mathf.Clamp01(t);

        float distx = a.x + t * ab.x - p.x;
        float distz = a.y + t * ab.y - p.y;

        return distx * distx + distz * distz;
    }

    //计算轮廓有向面积(顶点顺时针面积为负，逆时针面积为正)
    //有向面积 = 0.5 * ∑(x(i) * z(i + 1) - x(i + 1) * z(i)) 
    public static int GetContourSignedArea(List<Vector4Int> verts)
    {
        int area = 0;
        int count = verts.Count;
        for (int i = 0; i < count; i++)
        {
            Vector2Int c = verts[i];
            Vector2Int n = verts[Util.Neighbor(i, count, false)];
            area += c.x * n.y - n.x * c.y;
        }

        return (area + 1) / 2;
    }

    public static int Cross(Vector2Int a, Vector2Int b, Vector2Int c)
    {
        return (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
    }
    
    public static float Cross(ref Vector3 a, ref Vector3 b, ref Vector3 c)
    {
        return (b.x - a.x) * (c.z - a.z) - (c.x - a.x) * (b.z - a.z);
    }

    //判断C是否在AB右边
    //AB叉乘AC 如果AC在AB逆时针方向值为正 顺时针方向值为负
    public static bool Right(Vector2Int a, Vector2Int b, Vector2Int c)
    {
        return Cross(a, b, c) < 0;
    }
    
    public static bool Right(ref Vector3 a, ref Vector3 b, ref Vector3 c)
    {
        return Cross(ref a, ref b, ref c) < 0;
    }

    //判断C是否在AB右边或者是AB上
    public static bool RightOrOn(Vector2Int a, Vector2Int b, Vector2Int c)
    {
        return Cross(a, b, c) <= 0;
    }
    
    public static bool RightOrOn(ref Vector3 a, ref Vector3 b, ref Vector3 c)
    {
        return Cross(ref a, ref b, ref c) <= 0;
    }

    //ABC3点共线
    public static bool Collinear(Vector2Int a, Vector2Int b, Vector2Int c)
    {
        return Cross(a, b, c) == 0;
    }

    //C点是否在线段AB上
    public static bool Between(Vector2Int a, Vector2Int b, Vector2Int c)
    {
        if (!Collinear(a, b, c))
        {
            return false;
        }

        if (a.x != b.x)
        {
            return a.x <= c.x && c.x <= b.x || a.x >= c.x && c.x >= b.x;
        }
        else
        {
            return a.y <= c.y && c.y <= b.y || a.y >= c.y && c.y >= b.y;
        }
    }

    //判断AB与CD是否相交
    //CD某一点在AB上或者AB某一点在CD上则相交
    //CD在AB左右两侧并且AB在CD左右两侧则相交
    public static bool Intersect(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int d)
    {
        if (Between(a, b, c) || Between(a, b, d) || Between(c, d, a) || Between(c, d, b))
        {
            return true;
        }

        return Right(a, b, c) ^ Right(a, b, d) && Right(c, d, a) ^ Right(c, d, b);
    }

    //判断点P是否在ABC组成的圆锥里
    public static bool InCone(Vector2Int a, Vector2Int b, Vector2Int c, Vector2Int p)
    {
        //如果点A是凸顶点
        if (RightOrOn(b, a, c))
        {
            return Right(a, p, b) && Right(p, a, c);
        }

        //否则点A是凹顶点
        return !(RightOrOn(a, p, c) && RightOrOn(p, a, b));
    }
    
    private const int DEFLAG = 0x3FFFFFFF;

    public static bool InCone(int ai, int bi, int ci, int pi, List<Vector4Int> verts, List<int> indices)
    {
        ai = indices[ai] & DEFLAG;
        bi = indices[bi] & DEFLAG;
        ci = indices[ci] & DEFLAG;
        pi = indices[pi] & DEFLAG;
        Vector2Int a = verts[ai];
        Vector2Int b = verts[bi];
        Vector2Int c = verts[ci];
        Vector2Int p = verts[pi];
        
        //如果点A是凸顶点
        if (RightOrOn(b, a, c))
        {
            return Right(a, p, b) && Right(p, a, c);
        }

        //否则点A是凹顶点
        return !(RightOrOn(a, p, c) && RightOrOn(p, a, b));
    }

    public static bool IntersectCountour(Vector2Int a, Vector2Int b, int index, List<Vector4Int> verts)
    {
        int count = verts.Count;
        for (int i = 0; i < count; i++)
        {
            int next = Util.Neighbor(i, count, false);
            if (i == index || next == index)
            {
                continue;
            }

            Vector2Int c = verts[i];
            Vector2Int d = verts[next];

            if (Intersect(a, b, c, d))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IntersectCountour(int ai, int pi, List<Vector4Int> verts, List<int> indices)
    {
        ai = indices[ai] & DEFLAG;
        pi = indices[pi] & DEFLAG;
        Vector2Int a = verts[ai];
        Vector2Int p = verts[pi];
        int count = verts.Count;
        for (int i = 0; i < count; i++)
        {
            int next = Util.Neighbor(i, count, false);
            if (i == ai || i == pi || next == ai || next == pi)
            {
                continue;
            }

            Vector2Int c = verts[i];
            Vector2Int d = verts[next];

            if (Intersect(a, p, c, d))
            {
                return true;
            }
        }

        return false;
    }
    
    //射线法判断点是否在多边形内
    public static bool InPoly(ref Vector3 p, Vector3[] verts, int[] polys, int ppi, int mvc)
    {
        int cvi, pvi;
        int ppc = Util.GetPolyVertCount(ppi, polys, mvc);
        bool @in = false;
        for (cvi = 0, pvi = ppc - 1; cvi < ppc; pvi = cvi++)
        {
            Vector3 vc = verts[polys[ppi + cvi]];
            Vector3 vp = verts[polys[ppi + pvi]];
            if (vc.z > p.z != vp.z > p.z && p.x < (vp.x - vc.x) * (p.z - vc.z) / (vp.z - vc.z) + vc.x)
            {
                @in = !@in;
            }
        }

        return @in;
    }
}