using System.Collections.Generic;
using UnityEngine;

public static class AStar
{
	private class Node
	{
		public int id;
		public int flag;
		public int parent = -1;
		public float cost;
		public float total = -1.0f;
		public Vector3 pos;

		public void Reset()
		{
			id = 0;
			flag = 0;
			parent = -1;
			cost = 0;
			total = -1.0f;
			pos = Vector3.zero;
		}
	}

	private const int NODE_OPEN = 1;
	private const int NODE_CLOSE = 2;

	private class NodeComparer : IComparer<Node>
	{
		public int Compare(Node x, Node y)
		{
			if (x.total < y.total)
			{
				return -1;
			}
			else if (x.total > y.total)
			{
				return 1;
			}
			else
			{
				return 0;
			}
		}
	}

	private static PriorityQueue<Node> openList = new PriorityQueue<Node>(2048, new NodeComparer());
	private static Dictionary<int, Node> nodeCache = new Dictionary<int, Node>();

	public static PathFindStatus FindPath(List<int> path, ref NavMeshData navMeshData, ref Vector3 start, ref Vector3 end)
	{
		int sp = GetPoly(ref navMeshData, ref start);
		int ep = GetPoly(ref navMeshData, ref end);
		if (sp == -1 || ep == -1)
		{
			return PathFindStatus.NOT_IN_POLY;
		}

		if (sp == ep)
		{
			path.Add(sp);
			return PathFindStatus.SUCCESS;
		}

		openList.Clear();
		nodeCache.Clear();

		Node sn = ObjectPool.Get<Node>();
		sn.Reset();
		sn.id = sp;
		sn.flag = NODE_OPEN;
		sn.total = Vector3.Distance(start, end);
		sn.pos = start;
		openList.Insert(sn);
		nodeCache.Add(sn.id, sn);

		bool find = false;

		while (openList.Count > 0)
		{
			var cn = openList.DeleteMin();
			cn.flag &= ~NODE_OPEN;
			cn.flag |= NODE_CLOSE;

			if (cn.id == ep)
			{
				find = true;
				break;
			}

			int ppi = cn.id * navMeshData.MaxVertCountInPoly * 2;
			int pei = ppi + navMeshData.MaxVertCountInPoly;
			int ppc = Util.GetPolyVertCount(ppi, navMeshData.Polys, navMeshData.MaxVertCountInPoly);

			for (int pvi = 0; pvi < navMeshData.MaxVertCountInPoly; pvi++)
			{
				int np = navMeshData.Polys[pei + pvi];
				if (np == -1 || np == cn.parent)
				{
					continue;
				}

				if (!nodeCache.TryGetValue(np, out var nn))
				{
					nn = ObjectPool.Get<Node>();
					nn.Reset();
					nn.id = np;
					nodeCache.Add(np, nn);
				}

				if (nn.flag == 0)
				{
					var sv1 = navMeshData.Verts[navMeshData.Polys[ppi + pvi]];
					var sv2 = navMeshData.Verts[navMeshData.Polys[ppi + Util.Neighbor(pvi, ppc, false)]];
					nn.pos = (sv1 + sv2) * 0.5f;
				}

				float cost = 0;
				float heuristic = 0;

				if (nn.id == ep)
				{
					float curCost = Vector3.Distance(cn.pos, nn.pos);
					float endCost = Vector3.Distance(nn.pos, end);
					cost = cn.cost + curCost + endCost;
					heuristic = 0;
				}
				else
				{
					float curCost = Vector3.Distance(cn.pos, nn.pos);
					cost = cn.cost + curCost;
					heuristic = Vector3.Distance(nn.pos, end);
				}

				float total = cost + heuristic;

				if ((nn.flag & (NODE_OPEN | NODE_CLOSE)) != 0 && total >= nn.total)
				{
					continue;
				}

				nn.parent = cn.id;
				nn.flag &= ~NODE_CLOSE;
				nn.cost = cost;
				nn.total = total;

				if ((nn.flag & NODE_OPEN) != 0)
				{
					openList.Update(nn);
				}
				else
				{
					nn.flag |= NODE_OPEN;
					openList.Insert(nn);
				}
			}
		}

		if (find)
		{
			int poly = ep;
			while (nodeCache.TryGetValue(poly, out var node))
			{
				path.Add(poly);
				poly = node.parent;
			}

			for (int i = 0; i < path.Count / 2; i++)
			{
				int tmp = path[i];
				path[i] = path[path.Count - 1 - i];
				path[path.Count - 1 - i] = tmp;
			}
		}

		openList.Clear();
		foreach (var node in nodeCache.Values)
		{
			ObjectPool.Return(node);
		}

		nodeCache.Clear();

		return PathFindStatus.SUCCESS;
	}

	private static int GetPoly(ref NavMeshData navMeshData, ref Vector3 p)
	{
		Vector3 voxel = (p - navMeshData.BoundsMin) * navMeshData.InverseVoxelSize;
		for (int i = 0; i < 3; i++)
		{
			voxel[i] = MathTool.FloorToInt(voxel[i]);
		}

		int res = -1;
		List<int> polys = navMeshData.BVH.Traverse(ref voxel);
		foreach (var poly in polys)
		{
			int ppi = poly * navMeshData.MaxVertCountInPoly * 2;
			bool inPoly = MathTool.InPoly(ref p, navMeshData.Verts, navMeshData.Polys, ppi, navMeshData.MaxVertCountInPoly);
			if (inPoly)
			{
				res = poly;
				break;
			}
		}

		ObjectPool.Return(polys);
		return res;
	}
}