using UnityEngine;

public class NavMeshDataBuilder
{
    public NavMeshData Build(PolyMeshField polyMeshField)
    {
        NavMeshData navMeshData = new NavMeshData();
        
        navMeshData.BoundsMin = BuilderData.Bounds.Min;
        navMeshData.InverseVoxelSize = BuilderData.InverseVoxelSize;
        navMeshData.MaxVertCountInPoly = BuilderData.MaxVertCountInPoly;
        
        navMeshData.Verts = new Vector3[polyMeshField.Verts.Length];
        for (int i = 0; i < polyMeshField.Verts.Length; i++)
        {
            Vector4Int voxelVert = polyMeshField.Verts[i];
            Vector3 vert = new Vector3();
            vert.x = BuilderData.Bounds.Min.x + voxelVert.x * BuilderData.VoxelSize;
            vert.y = BuilderData.Bounds.Min.y + voxelVert.y * BuilderData.VoxelSize;
            vert.z = BuilderData.Bounds.Min.z + voxelVert.z * BuilderData.VoxelSize;
            navMeshData.Verts[i] = vert;
        }

        navMeshData.Polys = polyMeshField.Polys;

        BVH bvh = new BVH();
        LinearBVHNode[] items = new LinearBVHNode[polyMeshField.Regions.Length];
        for (int i = 0; i < items.Length; i++)
        {
            AABB aabb = new AABB(polyMeshField.Verts, polyMeshField.Polys, i * BuilderData.MaxVertCountInPoly * 2, BuilderData.MaxVertCountInPoly);
            items[i].Bounds = aabb;
            items[i].Idx = i;
        }

        bvh.Nodes = new LinearBVHNode[items.Length * 2 - 1];
        int offset = 0;
        BuildBVH(items, 0, items.Length, bvh.Nodes, ref offset);
        navMeshData.BVH = bvh;
        return navMeshData;
    }

    private void BuildBVH(LinearBVHNode[] items, int start, int end, LinearBVHNode[] nodes, ref int offset)
    {
        int cur = offset++;
        LinearBVHNode bvhNode = new LinearBVHNode();
        if (start == end - 1)
        {
            bvhNode = items[start];
        }
        else
        {
            AABB aabb = AABB.Default;
            for (int i = start; i < end; i++)
            {
                aabb.Union(items[i].Bounds);
            }

            bvhNode.Bounds = aabb;
            int dim = aabb.MaximumExtent();
            int split = (start + end) / 2;
            QuickSelect(items, split, start, end - 1, dim);
            BuildBVH(items, start, split, nodes, ref offset);
            BuildBVH(items, split, end, nodes, ref offset);
            bvhNode.Idx = -offset;
        }

        nodes[cur] = bvhNode;
    }

    private void QuickSelect(LinearBVHNode[] items, int k, int left, int right, int dim)
    {
        float pivot = Median3(items, left, right, dim);
        int i = left - 1;
        for (int j = left; j < right; ++j)
        {
            if (items[j].Bounds.Min[dim] <= pivot)
            {
                Swap(items, ++i, j);
            }
        }

        Swap(items, i + 1, right);

        if (k < i + 1)
        {
            QuickSelect(items, k, left, i, dim);
        }
        else if (k > i + 1)
        {
            QuickSelect(items, k, i + 2, right, dim);
        }
    }

    private void Swap(LinearBVHNode[] items, int i, int j)
    {
        var tmp = items[i];
        items[i] = items[j];
        items[j] = tmp;
    }

    private float Median3(LinearBVHNode[] items, int left, int right, int dim)
    {
        int mid = (left + right) / 2;
        if (items[left].Bounds.Min[dim] > items[mid].Bounds.Min[dim])
        {
            Swap(items, left, mid);
        }

        if (items[left].Bounds.Min[dim] > items[right].Bounds.Min[dim])
        {
            Swap(items, left, right);
        }

        if (items[mid].Bounds.Min[dim] > items[right].Bounds.Min[dim])
        {
            Swap(items, mid, right);
        }

        Swap(items, mid, right);

        return items[right].Bounds.Min[dim];
    }
}