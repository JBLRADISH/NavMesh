using System.Collections.Generic;

public class Contour
{
    public int Region;
    public List<int> OriginVerts;
    public List<int> SimplifiedVerts;
    public List<Contour> Holes = new List<Contour>();

    public int OriginCount => OriginVerts.Count / 4;
    public int SimplifiedCount => SimplifiedVerts.Count / 4;

    public Contour(int region, List<int> originVerts, List<int> simplifiedVerts)
    {
        Region = region;
        OriginVerts = new List<int>(originVerts);
        SimplifiedVerts = new List<int>(simplifiedVerts);
    }
    
    //Hole
    public int LDi;
    public int LDx => SimplifiedVerts[LDi];
    public int LDz => SimplifiedVerts[LDi + 2];
}