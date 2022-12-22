using UnityEngine.AI;

public static class BuilderData
{
    public static float VoxelSize { get; private set; }
    public static float InverseVoxelSize { get; private set; }
    public static float AgentSlope { get; private set; }
    public static int AgentHeight { get; private set; }
    public static int AgentClimb { get; private set; }
    public static int AgentRadius { get; private set; }
    public static int MinRegionVoxelCount { get; private set; } = 8;
    public static int MergeRegionVoxelCount { get; private set; } = 20;
    public static float EdgeMaxError { get; private set; } = 1.3f;
    public static int EdgeMaxLen { get; private set; } = 12;
    public static int MaxVertCountInPoly { get; private set; } = 6;

    public static void Init(int agentTypeID)
    {
        var settings = NavMesh.GetSettingsByID(agentTypeID);
        VoxelSize = settings.voxelSize;
        InverseVoxelSize = 1 / settings.voxelSize;
        AgentSlope = settings.agentSlope;
        AgentHeight = MathTool.CeilToInt(settings.agentHeight / settings.voxelSize);
        AgentClimb = MathTool.FloorToInt(settings.agentClimb / settings.voxelSize);
        AgentRadius = MathTool.CeilToInt(settings.agentRadius / settings.voxelSize);

        MinRegionVoxelCount *= MinRegionVoxelCount;
        MergeRegionVoxelCount *= MergeRegionVoxelCount;

        EdgeMaxLen = MathTool.FloorToInt(EdgeMaxLen * InverseVoxelSize);
    }

    public static AABB Bounds { get; private set; }
    public static int Width { get; private set; }
    public static int Depth { get; private set; }
    public static float BoundsHeight => Bounds.Height;

    public static void SetBounds(AABB bounds)
    {
        Bounds = bounds;
        CalcWidthDepth();
    }

    private static void CalcWidthDepth()
    {
        Width = MathTool.CeilToInt((Bounds.Max.x - Bounds.Min.x) / VoxelSize);
        Depth = MathTool.CeilToInt((Bounds.Max.z - Bounds.Min.z) / VoxelSize);
    }
}