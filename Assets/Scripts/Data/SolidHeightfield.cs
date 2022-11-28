using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolidHeightfield
{
    public float VoxelSize { get; private set; }
    public AABB Bounds { get; private set; }
    public int Width { get; private set; }
    public int Depth { get; private set; }
    public float BoundsHeight => Bounds.Height;

    public Dictionary<int, HeightSpan> Spans = new Dictionary<int, HeightSpan>();

    public SolidHeightfield(float voxelSize)
    {
        this.VoxelSize = voxelSize;
    }

    public void SetBounds(AABB bounds)
    {
        Bounds = bounds;
        CalcWidthDepth();
    }

    private void CalcWidthDepth()
    {
        Width = MathTool.CeilToInt((Bounds.Max.x - Bounds.Min.x) / VoxelSize);
        Depth = MathTool.CeilToInt((Bounds.Max.z - Bounds.Min.z) / VoxelSize);
    }

    public bool AddSpan(int widthIndex, int depthIndex, int heightIndexMin, int heightIndexMax, int flag)
    {
        if (widthIndex < 0 || widthIndex >= Width || depthIndex < 0 || depthIndex >= Depth)
        {
            return false;
        }

        if (heightIndexMin < 0 || heightIndexMax < 0 || heightIndexMin > heightIndexMax)
        {
            return false;
        }

        int index = GetIndex(widthIndex, depthIndex);

        if (!Spans.TryGetValue(index, out HeightSpan curSpan))
        {
            Spans.Add(index, new HeightSpan(heightIndexMin, heightIndexMax, flag));
            return true;
        }

        HeightSpan preSpan = null;
        while (curSpan != null)
        {
            if (curSpan.HeightIndexMin > heightIndexMax)
            {
                HeightSpan newSpan = new HeightSpan(heightIndexMin, heightIndexMax, flag);
                newSpan.Next = curSpan;
                if (preSpan == null)
                {
                    Spans[index] = newSpan;
                }
                else
                {
                    preSpan.Next = newSpan;
                }

                return true;
            }
            else if (curSpan.HeightIndexMax < heightIndexMin)
            {
                if (curSpan.Next == null)
                {
                    curSpan.Next = new HeightSpan(heightIndexMin, heightIndexMax, flag);
                    return true;
                }

                preSpan = curSpan;
                curSpan = curSpan.Next;
            }
            else
            {
                if (heightIndexMin < curSpan.HeightIndexMin)
                {
                    curSpan.HeightIndexMin = heightIndexMin;
                }

                if (heightIndexMax == curSpan.HeightIndexMax)
                {
                    curSpan.Flag |= flag;
                    return true;
                }

                if (curSpan.HeightIndexMax > heightIndexMax)
                {
                    return true;
                }

                HeightSpan nextSpan = curSpan.Next;
                while (true)
                {
                    if (nextSpan == null || nextSpan.HeightIndexMin > heightIndexMax)
                    {
                        curSpan.HeightIndexMax = heightIndexMax;
                        curSpan.Flag = flag;
                        if (nextSpan == null)
                        {
                            curSpan.Next = null;
                        }
                        else
                        {
                            curSpan.Next = nextSpan;
                        }

                        return true;
                    }

                    if (heightIndexMax <= nextSpan.HeightIndexMax)
                    {
                        curSpan.HeightIndexMax = nextSpan.HeightIndexMax;
                        curSpan.Next = nextSpan.Next;
                        curSpan.Flag = nextSpan.Flag;
                        if (heightIndexMax == curSpan.HeightIndexMax)
                        {
                            curSpan.Flag |= flag;
                            return true;
                        }

                        return true;
                    }

                    nextSpan = nextSpan.Next;
                }
            }
        }

        return false;
    }

    private int GetIndex(int widthIndex, int depthIndex)
    {
        if (widthIndex < 0 || depthIndex < 0 || widthIndex >= Width || depthIndex >= Depth)
        {
            return -1;
        }

        return widthIndex * Depth + depthIndex;
    }
}