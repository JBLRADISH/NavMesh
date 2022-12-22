using UnityEngine;

public class GameManager : MonoBehaviour
{
    private bool load = false;

    private void Start()
    {
        LoadData();
    }

    private void LoadData()
    {
        LoadTool.DeSerialization(ref RuntimeData.NavMeshData);
        load = true;
    }

    private void OnDrawGizmos()
    {
        if (load)
        {
            PolyMeshFieldGUI.Draw(ref RuntimeData.NavMeshData);
        }
    }
}