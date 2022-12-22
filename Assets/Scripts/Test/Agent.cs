using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Agent : MonoBehaviour
{
    private enum Status
    {
        Idle,
        Move
    }

    public float Velocity = 3f;

    private List<int> polys;
    private List<Vector3> path;

    private int index;
    private Vector3 target;
    private Vector3 dir;
    private float stopDistSq;
    private Status status = Status.Idle;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                SetDestination(hit.point);
            }
        }
    }

    public void SetDestination(Vector3 target)
    {
        target.y = 0;
        Vector3 start = transform.position;
        start.y = 0;

        if (polys == null)
        {
            polys = ObjectPool.Get<List<int>>();
        }

        polys.Clear();
        var pathFindStatus = AStar.FindPath(polys, ref RuntimeData.NavMeshData, ref start, ref target);

        if (pathFindStatus == PathFindStatus.NOT_IN_POLY)
        {
            Debug.LogError("起点或终点不在navmesh内！！！");
            return;
        }

        if (path == null)
        {
            path = ObjectPool.Get<List<Vector3>>();
        }

        path.Clear();
        StringPulling.FindPath(path, ref RuntimeData.NavMeshData, ref start, ref target, polys);

        index = 1;
        UpdateCur();
    }

    private void FixedUpdate()
    {
        if (status == Status.Move)
        {
            transform.Translate(dir * Velocity * Time.fixedDeltaTime);
            Vector3 dist = target - transform.position;
            dist.y = 0;
            float distSq = dist.sqrMagnitude;
            if (distSq < stopDistSq)
            {
                index++;
                UpdateCur();
            }
        }
    }

    private void UpdateCur()
    {
        if (index >= path.Count)
        {
            status = Status.Idle;
        }
        else
        {
            target = path[index];
            dir = path[index] - path[index - 1];
            dir.y = 0;
            dir.Normalize();
            stopDistSq = Velocity * Time.fixedDeltaTime;
            stopDistSq *= stopDistSq;
            status = Status.Move;
        }
    }

    private void OnDestroy()
    {
        ObjectPool.Return(polys);
        ObjectPool.Return(path);
    }

    private void OnDrawGizmos()
    {
        if (status == Status.Move)
        {
            AStarGUI.Draw(ref RuntimeData.NavMeshData, polys);
            StringPullingGUI.Draw(path);
        }
    }
}