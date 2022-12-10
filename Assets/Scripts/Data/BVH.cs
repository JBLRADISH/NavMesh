public struct LinearBVHNode
{
    public AABB Bounds;
    //>=0代表多边形索引 <0代表右子树在数组里的偏移
    public int Idx;
}

public class BVH
{
    public LinearBVHNode[] Nodes;
}