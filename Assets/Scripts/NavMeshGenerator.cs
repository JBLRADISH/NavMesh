﻿using System.Collections.Generic;
using UnityEngine;

public class NavMeshGenerator : MonoBehaviour
{
    private SolidHeightfield solidHeightfield;
    private OpenHeightField openHeightfield;
    private ContourSet contourSet;
    private PolyMeshField polyMeshField;

    private NavMeshData navMeshData;

    private int drawIndex = 0;
    public bool simplified = true;
    public bool bvh = true;

    private void InitGlobal()
    {
        BuilderData.Init(0);
    }

    public void BuildSolidHeightfield()
    {
        InitGlobal();

        SolidHeightfieldBuilder builder = new SolidHeightfieldBuilder();
        solidHeightfield = builder.Build();
        drawIndex = 1;
    }

    public void BuildOpenHeightfield()
    {
        OpenHeightfieldBuilder builder = new OpenHeightfieldBuilder();
        openHeightfield = builder.Build(solidHeightfield);
        drawIndex = 2;
    }

    public void BuildContourSet()
    {
        ContourSetBuilder builder = new ContourSetBuilder();
        contourSet = builder.Build(openHeightfield);
        drawIndex = 3;
    }

    public void BuildPolyMeshField()
    {
        PolyMeshFieldBuilder builder = new PolyMeshFieldBuilder();
        polyMeshField = builder.Build(contourSet);
        drawIndex = 4;
    }

    public void BuildNavMeshData()
    {
        NavMeshDataBuilder builder = new NavMeshDataBuilder();
        navMeshData = builder.Build(polyMeshField);
        LoadTool.Serialization(ref navMeshData);
        drawIndex = 5;
    }

    private void OnDrawGizmos()
    {
        if (solidHeightfield != null && drawIndex == 1)
        {
            SolidHeightfieldGUI.Draw(solidHeightfield);
        }

        if (openHeightfield != null && drawIndex == 2)
        {
            OpenHeightfieldGUI.Draw(openHeightfield);
        }

        if (contourSet != null && drawIndex == 3)
        {
            ContourSetGUI.Draw(contourSet, openHeightfield.DrawColors, simplified);
        }

        if (polyMeshField != null && drawIndex >= 4)
        {
            PolyMeshFieldGUI.Draw(polyMeshField, openHeightfield.DrawColors);
        }

        if (bvh && drawIndex == 5)
        {
            BVHGUI.Draw(navMeshData.BVH);
        }
    }
}