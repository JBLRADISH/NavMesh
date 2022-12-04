public static class Util
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
}
