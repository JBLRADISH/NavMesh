using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NavMeshGenerator))]
public class NavMeshGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        NavMeshGenerator generator = target as NavMeshGenerator;

        if (GUILayout.Button("1.Build SolidHeightfield"))
        {
            generator.BuildSolidHeightfield();
        }

        if (GUILayout.Button("2.Build OpenHeightfield"))
        {
            generator.BuildOpenHeightfield();
        }

        if (GUILayout.Button("3.Build ContourSet"))
        {
            generator.BuildContourSet();
        }

        generator.simplified = EditorGUILayout.Toggle("显示简易轮廓", generator.simplified);

        if (GUILayout.Button("4.Build PolyMeshField"))
        {
            generator.BuildPolyMeshField();
        }

        if (GUILayout.Button("5.Build NavMeshData"))
        {
            generator.BuildNavMeshData();
        }

        generator.bvh = EditorGUILayout.Toggle("显示BVH", generator.bvh);
    }
}