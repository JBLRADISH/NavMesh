using System.Collections.Generic;

public class ContourSet
{
    public List<Contour> Contours = new List<Contour>();

    public void AddContour(Contour contour)
    {
        Contours.Add(contour);
    }
}