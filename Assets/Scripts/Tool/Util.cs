﻿public static class Util
{
    public static int Neighbor(int i, int n, bool pre)
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
    
    public static int GetPolyVertCount(int ppi, int[] polys, int mvc)
    {
        for (int pvi = 0; pvi < mvc; pvi++)
        {
            if (polys[ppi + pvi] == -1)
            {
                return pvi;
            }
        }

        return mvc;
    }
}