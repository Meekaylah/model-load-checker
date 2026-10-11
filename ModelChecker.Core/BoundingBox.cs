namespace ModelChecker.Core;

public record BoundingBox(ThreeDPoint Min, ThreeDPoint Max)
{
    public double Width => Max.X - Min.X;
    public double Height => Max.Y - Min.Y;
    public double Depth => Max.Z - Min.Z;
}