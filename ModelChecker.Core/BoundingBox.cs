namespace ModelChecker.Core;

// The smallest box that fits around the whole model.
// Min is the corner with the smallest x, y and z; Max is the opposite corner.
public record BoundingBox(ThreeDPoint Min, ThreeDPoint Max)
{
    public double Width => Max.X - Min.X;
    public double Height => Max.Y - Min.Y;
    public double Depth => Max.Z - Min.Z;

    // "static" means we call this on the type itself (BoundingBox.FromPoints(...)),
    // because we don't have a box yet - this is how we make one.
    public static BoundingBox FromPoints(IReadOnlyList<ThreeDPoint> points)
    {
        // A model with no vertices has no size. Returning a zero-sized box
        // (instead of null) means callers never have to do a null check.
        if (points.Count == 0)
        {
            ThreeDPoint origin = new ThreeDPoint(0, 0, 0);
            return new BoundingBox(origin, origin);
        }

        // Start with the first point as both the smallest and largest seen so far.
        // Starting from 0 would be wrong for a model that sits entirely away from the origin.
        double minX = points[0].X, minY = points[0].Y, minZ = points[0].Z;
        double maxX = minX, maxY = minY, maxZ = minZ;

        foreach (ThreeDPoint point in points)
        {
            minX = Math.Min(minX, point.X);
            minY = Math.Min(minY, point.Y);
            minZ = Math.Min(minZ, point.Z);

            maxX = Math.Max(maxX, point.X);
            maxY = Math.Max(maxY, point.Y);
            maxZ = Math.Max(maxZ, point.Z);
        }

        return new BoundingBox(new ThreeDPoint(minX, minY, minZ), new ThreeDPoint(maxX, maxY, maxZ));
    }
}
