using System.Collections.Generic;

public class Region
{
    public int ID;
    public int SpanCount;
    public HashSet<int> Overlaps = new HashSet<int>();
    public List<int> Neighbors = new List<int>();
    public bool Remapping;
    
    public Region(int id)
    {
        ID = id;
    }

    public void Reset(int id)
    {
        ID = id;
        SpanCount = 0;
        Overlaps.Clear();
        Neighbors.Clear();
    }
}