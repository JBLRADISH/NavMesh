using System.Collections.Generic;

public class SolidHeightfield
{
    public Dictionary<int, HeightSpan> Spans = new Dictionary<int, HeightSpan>();

    public bool AddSpan(int widthIndex, int depthIndex, int heightIndexMin, int heightIndexMax, int flag)
    {
        if (widthIndex < 0 || widthIndex >= BuilderData.Width || depthIndex < 0 || depthIndex >= BuilderData.Depth)
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
        if (widthIndex < 0 || depthIndex < 0 || widthIndex >= BuilderData.Width || depthIndex >= BuilderData.Depth)
        {
            return -1;
        }

        return widthIndex * BuilderData.Depth + depthIndex;
    }
}