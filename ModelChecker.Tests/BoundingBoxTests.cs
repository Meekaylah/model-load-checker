using ModelChecker.Core;

namespace ModelChecker.Tests;

public class BoundingBoxTests
{
    [Fact]
    public void FromPoints_FindsTheSmallestAndLargestCorner()
    {
        List<ThreeDPoint> points = new List<ThreeDPoint>
        {
            new ThreeDPoint(-1, 5, 2),
            new ThreeDPoint(3, -2, 0),
            new ThreeDPoint(0, 0, 10)
        };

        BoundingBox box = BoundingBox.FromPoints(points);

        Assert.Equal(new ThreeDPoint(-1, -2, 0), box.Min);
        Assert.Equal(new ThreeDPoint(3, 5, 10), box.Max);
        Assert.Equal(4, box.Width);
        Assert.Equal(7, box.Height);
        Assert.Equal(10, box.Depth);
    }

    [Fact]
    public void FromPoints_WithNoPoints_GivesAZeroSizedBox()
    {
        BoundingBox box = BoundingBox.FromPoints(new List<ThreeDPoint>());

        Assert.Equal(0, box.Width);
        Assert.Equal(0, box.Height);
        Assert.Equal(0, box.Depth);
    }

    [Fact]
    public void FromPoints_WithOnePoint_GivesAZeroSizedBoxAtThatPoint()
    {
        ThreeDPoint only = new ThreeDPoint(4, 5, 6);

        BoundingBox box = BoundingBox.FromPoints(new List<ThreeDPoint> { only });

        Assert.Equal(only, box.Min);
        Assert.Equal(only, box.Max);
    }
}
