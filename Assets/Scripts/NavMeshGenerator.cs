using System;
using UnityEngine;

public class NavMeshGenerator : MonoBehaviour
{
    private SolidHeightfield solidHeightfield;
    private OpenHeightField openHeightfield;
    private ContourSet contourSet;

    private int drawIndex = 0;
    public bool simplified = true;

    public void BuildSolidHeightfield()
    {
        SolidHeightfieldBuilder builder = new SolidHeightfieldBuilder(0);
        solidHeightfield = builder.Build();
        drawIndex = 1;
    }

    public void BuildOpenHeightfield()
    {
        OpenHeightfieldBuilder builder = new OpenHeightfieldBuilder(0);
        openHeightfield = builder.Build(solidHeightfield);
        drawIndex = 2;
    }

    public void BuildContourSet()
    {
        ContourSetBuilder builder = new ContourSetBuilder();
        contourSet = builder.Build(openHeightfield);
        drawIndex = 3;
    }

    private void OnDrawGizmos()
    {
        if (solidHeightfield != null && drawIndex == 1)
        {
            SolidHeightfieldGUI.Draw(solidHeightfield);
        }
        else if (openHeightfield != null && drawIndex == 2)
        {
            OpenHeightfieldGUI.Draw(openHeightfield);
        }
        else if (contourSet != null && drawIndex == 3)
        {
            ContourSetGUI.Draw(contourSet, openHeightfield.DrawColors, simplified);
        }
    }
}