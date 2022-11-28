using UnityEngine;

public struct Vector4Int
{
    public int x;
    public int y;
    public int z;
    public int w;

    public Vector4Int(int x, int y, int z, int w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    public static implicit operator Vector2Int(Vector4Int v) => new Vector2Int(v.x, v.z);
}