using System.IO;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

public static class LoadTool
{
    public static unsafe void Serialization(ref NavMeshData navMeshData)
    {
        int vertLen = navMeshData.Verts.Length;
        int vertSize = vertLen * sizeof(Vector3);
        int polyLen = navMeshData.Polys.Length;
        int polySize = polyLen * sizeof(int);
        int bvhLen = navMeshData.BVH.Nodes.Length;
        int bvhSize = bvhLen * sizeof(LinearBVHNode);

        //boundsMin + inverseVoxelSize + maxVertCountInPoly + vertLen + polyLen + bvhLen + vertSize + polySize + bvhSize
        int totalSize = sizeof(Vector3) + sizeof(float) + 4 * sizeof(int) + vertSize + polySize + bvhSize;
        byte[] buffer = new byte[totalSize];

        var bufferIntPtr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0);
        byte* bufferPtr = (byte*) bufferIntPtr;

        Vector3 boundsMin = navMeshData.BoundsMin;
        UnsafeUtility.MemCpy(bufferPtr, &boundsMin, sizeof(Vector3));
        bufferPtr += sizeof(Vector3);

        float inverseVoxelSize = navMeshData.InverseVoxelSize;
        UnsafeUtility.MemCpy(bufferPtr, &inverseVoxelSize, sizeof(float));
        bufferPtr += sizeof(float);

        int maxVertCountInPoly = navMeshData.MaxVertCountInPoly;
        UnsafeUtility.MemCpy(bufferPtr, &maxVertCountInPoly, sizeof(int));
        bufferPtr += sizeof(int);

        UnsafeUtility.MemCpy(bufferPtr, &vertLen, sizeof(int));
        bufferPtr += sizeof(int);
        var intPtr = Marshal.UnsafeAddrOfPinnedArrayElement(navMeshData.Verts, 0);
        byte* ptr = (byte*) intPtr;
        UnsafeUtility.MemCpy(bufferPtr, ptr, vertSize);
        bufferPtr += vertSize;

        UnsafeUtility.MemCpy(bufferPtr, &polyLen, sizeof(int));
        bufferPtr += sizeof(int);
        intPtr = Marshal.UnsafeAddrOfPinnedArrayElement(navMeshData.Polys, 0);
        ptr = (byte*) intPtr;
        UnsafeUtility.MemCpy(bufferPtr, ptr, polySize);
        bufferPtr += polySize;

        UnsafeUtility.MemCpy(bufferPtr, &bvhLen, sizeof(int));
        bufferPtr += sizeof(int);
        intPtr = Marshal.UnsafeAddrOfPinnedArrayElement(navMeshData.BVH.Nodes, 0);
        ptr = (byte*) intPtr;
        UnsafeUtility.MemCpy(bufferPtr, ptr, bvhSize);

        FileStream fs = new FileStream(Application.dataPath + "/Resources/Data/NavMeshData.bytes", FileMode.OpenOrCreate);
        fs.Write(buffer, 0, buffer.Length);
        fs.Close();

        AssetDatabase.Refresh();
    }

    public static unsafe void DeSerialization(ref NavMeshData navMeshData)
    {
        TextAsset asset = Resources.Load<TextAsset>("Data/NavMeshData");
        var assetIntPtr = Marshal.UnsafeAddrOfPinnedArrayElement(asset.bytes, 0);
        byte* assetPtr = (byte*) assetIntPtr;

        navMeshData.BoundsMin = *(Vector3*) assetPtr;
        assetPtr += sizeof(Vector3);

        navMeshData.InverseVoxelSize = *(float*) assetPtr;
        assetPtr += sizeof(float);

        navMeshData.MaxVertCountInPoly = *(int*) assetPtr;
        assetPtr += sizeof(int);

        int vertLen = *(int*) assetPtr;
        int vertSize = vertLen * sizeof(Vector3);
        assetPtr += sizeof(int);
        navMeshData.Verts = new Vector3[vertLen];
        var intPtr = Marshal.UnsafeAddrOfPinnedArrayElement(navMeshData.Verts, 0);
        byte* ptr = (byte*) intPtr;
        UnsafeUtility.MemCpy(ptr, assetPtr, vertSize);
        assetPtr += vertSize;

        int polyLen = *(int*) assetPtr;
        int polySize = polyLen * sizeof(int);
        assetPtr += sizeof(int);
        navMeshData.Polys = new int[polyLen];
        intPtr = Marshal.UnsafeAddrOfPinnedArrayElement(navMeshData.Polys, 0);
        ptr = (byte*) intPtr;
        UnsafeUtility.MemCpy(ptr, assetPtr, polySize);
        assetPtr += polySize;

        int bvhLen = *(int*) assetPtr;
        int bvhSize = bvhLen * sizeof(LinearBVHNode);
        assetPtr += sizeof(int);
        navMeshData.BVH.Nodes = new LinearBVHNode[bvhLen];
        intPtr = Marshal.UnsafeAddrOfPinnedArrayElement(navMeshData.BVH.Nodes, 0);
        ptr = (byte*) intPtr;
        UnsafeUtility.MemCpy(ptr, assetPtr, bvhSize);

        Resources.UnloadAsset(asset);
    }
}