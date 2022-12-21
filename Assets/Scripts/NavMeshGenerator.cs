using System.Collections.Generic;
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

    public Transform start;
    public Transform end;

    private List<int> polys = ObjectPool.Get<List<int>>();
    private List<Vector3> path = ObjectPool.Get<List<Vector3>>();

    private void InitGlobal()
    {
        Global.Init(0);
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
        drawIndex = 5;
    }

    public void AStarPathFind()
    {
        Vector3 sp = start.position;
        Vector3 ep = end.position;
        polys.Clear();
        AStar.FindPath(polys, ref navMeshData, ref sp, ref ep);
        drawIndex = 6;
    }
    
    public void StringPullingPathFind()
    {
        Vector3 sp = start.position;
        Vector3 ep = end.position;
        path.Clear();
        StringPulling.FindPath(path, ref navMeshData, ref sp, ref ep, polys);
        drawIndex = 7;
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

        if (polys.Count > 0 && drawIndex == 6)
        {
            AStarGUI.Draw(ref navMeshData, polys);
        }
        
        if (path.Count > 0 && drawIndex == 7)
        {
            StringPullingGUI.Draw(path);
        }
    }
}