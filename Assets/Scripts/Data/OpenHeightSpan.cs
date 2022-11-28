public class OpenHeightSpan
{
    public int Floor;
    public int Height;
    public int Ceiling => Floor + Height;
    public int DistToBorder = 0;
    public int Flag = 1;
    public int Region = 0;
    public int DistToRegion = 0;
    public OpenHeightSpan Next;
    public OpenHeightSpan[] Neighbors = new OpenHeightSpan[4];

    public OpenHeightSpan(int floor, int height)
    {
        Floor = floor;
        Height = height;
    }
}