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
    
    public static float GetPointToSegmentDistSq(int px, int pz, int ax, int az, int bx, int bz)
    {
        float dABx = bx - ax;
        float dABz = bz - az;
        float dAPx = px - ax;
        float dAPz = pz - az;

        float seLenSq = dABx * dABx + dABz * dABz;

        if (seLenSq == 0)
        {
            return dAPx * dAPx + dAPz * dAPz;
        }

        //先计算SP在SE上投影长度(点乘) |SP| * |SE| * cosθ = SP.SE 投影长度(|SP| * cosθ) = SP.SE / |SE|
        //t = 投影长度/|SE| = SP.SE / |SE| * |SE|
        float t = (dAPx * dABx + dAPz * dABz) / seLenSq;

        t = Mathf.Clamp01(t);

        float distx = ax + t * dABx - px;
        float distz = az + t * dABz - pz;

        return distx * distx + distz * distz;
    }

    //计算轮廓有向面积(顶点顺时针面积为负，逆时针面积为正)
    //有向面积 = 0.5 * ∑(x(i) * z(i + 1) - x(i + 1) * z(i)) 
    public static int GetContourSignedArea(List<int> simplifiedVerts)
    {
        int area = 0;
        int count = simplifiedVerts.Count / 4;
        for (int i = 0; i < count; i++)
        {
            int cx = simplifiedVerts[i * 4];
            int cz = simplifiedVerts[i * 4 + 2];
            int nx = simplifiedVerts[(i + 1) % count * 4];
            int nz = simplifiedVerts[(i + 1) % count * 4 + 2];
            area += cx * nz - nx * cz;
        }

        return (area + 1) / 2;
    }
    
    //判断C是否在AB右边
    //AB叉乘AC 如果AC在AB逆时针方向值为正 顺时针方向值为负
    public static bool Right(int ax, int az, int bx, int bz, int cx, int cz)
    {
        return (bx - ax) * (cz - az) - (cx - ax) * (bz - az) < 0;
    }
    
    //判断C是否在AB右边或者是AB上
    public static bool RightOrOn(int ax, int az, int bx, int bz, int cx, int cz)
    {
        return (bx - ax) * (cz - az) - (cx - ax) * (bz - az) <= 0;
    }
}