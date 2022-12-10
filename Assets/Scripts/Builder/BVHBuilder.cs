public class BVHBuilder
{
    public BVH Build(PolyMeshField polyMeshField)
    {
        BVH bvh = new BVH();
        AABB[] aabbs = new AABB[polyMeshField.Regions.Length];
        for (int i = 0; i < aabbs.Length; i++)
        {
            AABB aabb = new AABB(polyMeshField.Verts, polyMeshField.Polys, i * Global.MaxVertCountInPoly * 2, Global.MaxVertCountInPoly);
            aabbs[i] = aabb;
        }

        bvh.Nodes = new LinearBVHNode[aabbs.Length * 2 - 1];
        int offset = 0;
        BuildBVH(aabbs, 0, aabbs.Length, bvh.Nodes, ref offset);
        return bvh;
    }

    private void BuildBVH(AABB[] aabbs, int start, int end, LinearBVHNode[] nodes, ref int offset)
    {
        int cur = offset++;
        LinearBVHNode bvhNode = new LinearBVHNode();
        if (start == end - 1)
        {
            bvhNode.Bounds = aabbs[start];
            bvhNode.Idx = start;
        }
        else
        {
            AABB aabb = AABB.Default;
            for (int i = start; i < end; i++)
            {
                aabb.Union(aabbs[i]);
            }

            bvhNode.Bounds = aabb;
            int dim = aabb.MaximumExtent();
            int split = (start + end) / 2;
            QuickSelect(aabbs, split, start, end - 1, dim);
            BuildBVH(aabbs, start, split, nodes, ref offset);
            bvhNode.Idx = -offset;
            BuildBVH(aabbs, split, end, nodes, ref offset);
        }
        nodes[cur] = bvhNode;
    }

    private void QuickSelect(AABB[] aabbs, int k, int left, int right, int dim)
    {
        float pivot = Median3(aabbs, left, right, dim);
        int i = left - 1;
        for (int j = left; j < right; ++j)
        {
            if (aabbs[j].Min[dim] <= pivot)
            {
                Swap(aabbs, ++i, j);
            }
        }

        Swap(aabbs, i + 1, right);

        if (k < i + 1)
        {
            QuickSelect(aabbs, k, left, i, dim);
        }
        else if (k > i + 1)
        {
            QuickSelect(aabbs, k, i + 2, right, dim);
        }
    }

    private void Swap(AABB[] aabbs, int i, int j)
    {
        AABB tmp = aabbs[i];
        aabbs[i] = aabbs[j];
        aabbs[j] = tmp;
    }

    private float Median3(AABB[] aabbs, int left, int right, int dim)
    {
        int mid = (left + right) / 2;
        if (aabbs[left].Min[dim] > aabbs[mid].Min[dim])
        {
            Swap(aabbs, left, mid);
        }

        if (aabbs[left].Min[dim] > aabbs[right].Min[dim])
        {
            Swap(aabbs, left, right);
        }

        if (aabbs[mid].Min[dim] > aabbs[right].Min[dim])
        {
            Swap(aabbs, mid, right);
        }

        Swap(aabbs, mid, right);

        return aabbs[right].Min[dim];
    }
}