using System;
using UnityEngine;

public struct Vector4Int : IEquatable<Vector4Int>
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

    public bool Equals(Vector4Int other) => x == other.x && y == other.y && z == other.z;

    public override int GetHashCode()
    {
        int num1 = x;
        int hashCode = num1.GetHashCode();
        num1 = z;
        int num2 = num1.GetHashCode() << 2;
        return hashCode ^ num2;
    }
}