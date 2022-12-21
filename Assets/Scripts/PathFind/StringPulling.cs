using System.Collections.Generic;
using UnityEngine;

public static class StringPulling
{
	public static void FindPath(List<Vector3> path, ref NavMeshData navMeshData, ref Vector3 start, ref Vector3 end, List<int> polys)
	{
		if (polys.Count <= 1)
		{
			path.Add(start);
			path.Add(end);
			return;
		}

		Vector3 portalApex = start;
		path.Add(portalApex);
		Vector3 portalLeft = portalApex;
		Vector3 portalRight = portalApex;
		int apexIndex = 0;
		int leftIndex = 0;
		int rightIndex = 0;

		for (int i = 0; i < polys.Count; i++)
		{
			Vector3 left = Vector3.zero;
			Vector3 right = Vector3.zero;

			if (i + 1 < polys.Count)
			{
				GetNextPortal(ref navMeshData, polys, i, ref left, ref right);
			}
			else
			{
				left = end;
				right = end;
			}

			if (!MathTool.Right(ref portalApex, ref portalRight, ref right))
			{
				if (portalApex == portalRight || MathTool.Right(ref portalApex, ref portalLeft, ref right))
				{
					portalRight = right;
					rightIndex = i;
				}
				else
				{
					portalApex = portalLeft;
					apexIndex = leftIndex;
					path.Add(portalApex);

					portalLeft = portalApex;
					portalRight = portalApex;
					leftIndex = apexIndex;
					rightIndex = apexIndex;

					i = apexIndex;
					
					continue;
				}
			}

			if (MathTool.RightOrOn(ref portalApex, ref portalLeft, ref left))
			{
				if (portalApex == portalLeft || !MathTool.RightOrOn(ref portalApex, ref portalRight, ref left))
				{
					portalLeft = left;
					leftIndex = i;
				}
				else
				{
					portalApex = portalRight;
					apexIndex = rightIndex;
					path.Add(portalRight);

					portalLeft = portalApex;
					portalRight = portalApex;
					leftIndex = apexIndex;
					rightIndex = apexIndex;

					i = apexIndex;

					continue;
				}
			}
		}

		path.Add(end);
	}

	private static void GetNextPortal(ref NavMeshData navMeshData, List<int> polys, int index, ref Vector3 left, ref Vector3 right)
	{
		int np = polys[index + 1];
		int lpi = polys[index] * Global.MaxVertCountInPoly * 2;
		int lei = lpi + Global.MaxVertCountInPoly;
		int lvi;
		for (lvi = 0; lvi < Global.MaxVertCountInPoly; lvi++)
		{
			if (navMeshData.Polys[lei + lvi] == np)
			{
				break;
			}
		}

		if (lvi == Global.MaxVertCountInPoly)
		{
			return;
		}

		left = navMeshData.Verts[navMeshData.Polys[lpi + lvi]];
		right = navMeshData.Verts[navMeshData.Polys[lpi + Util.Neighbor(lvi, Util.GetPolyVertCount(lpi, navMeshData.Polys), false)]];
	}
}