public class HeightSpan
{
    public int HeightIndexMin;
    public int HeightIndexMax;
    public int Flag;
    public HeightSpan Next;

    public HeightSpan(int heightIndexMin, int heightIndexMax, int flag)
    {
        HeightIndexMin = heightIndexMin;
        HeightIndexMax = heightIndexMax;
        Flag = flag;
    }
}