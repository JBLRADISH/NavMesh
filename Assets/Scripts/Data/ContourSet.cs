using System.Collections.Generic;

public class ContourSet
{
    public float VoxelSize { get; private set; }
    public AABB Bounds { get; private set; }
    public List<Contour> Contours = new List<Contour>();
    
    public ContourSet(OpenHeightField openHeightField)
    {
        VoxelSize = openHeightField.VoxelSize;
        Bounds = openHeightField.Bounds;
    }

    public void AddContour(Contour contour)
    {
        Contours.Add(contour);
    }
}