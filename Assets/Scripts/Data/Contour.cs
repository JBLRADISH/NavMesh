using System.Collections.Generic;

public class Contour
{
    public int Region;
    //原始顶点不包含空洞的顶点
    public List<Vector4Int> OriginVerts;
    public List<Vector4Int> SimplifiedVerts;

    public Contour(int region, List<Vector4Int> originVerts, List<Vector4Int> simplifiedVerts)
    {
        Region = region;
        OriginVerts = new List<Vector4Int>(originVerts);
        SimplifiedVerts = new List<Vector4Int>(simplifiedVerts);
    }

    #region Hole
    
    public List<Contour> Holes = new List<Contour>();
    
    public int LDi;
    public int LDx => SimplifiedVerts[LDi].x;
    public int LDz => SimplifiedVerts[LDi].z;

    public class HoleToContour
    {
        public int ContourIndex;
        public int Dist;
    }

    public List<HoleToContour> HoleToContours = new List<HoleToContour>();

    public void ClearHoleData()
    {
        Holes.Clear();
        LDi = 0;
        HoleToContours.Clear();
    }

    #endregion
}