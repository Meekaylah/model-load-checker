using ModelChecker.Core;

namespace ModelChecker.Tests;

// Tests for the verdict rules. They use FakeModelParser so no real .obj text is needed.
//
// Note "Core.ModelChecker": on its own, the word ModelChecker here would mean the
// namespace (ModelChecker.Tests starts with it), so we say which one we mean.
public class ModelCheckerTests
{
    // Small helper so each test reads as: given this parse result, check it.
    private static CheckResult CheckWith(ParseResult parseResult)
    {
        Core.ModelChecker checker = new Core.ModelChecker(new FakeModelParser(parseResult));
        return checker.Check("the fake parser ignores this text");
    }

    // Finds the verdict for one device in the results.
    private static Verdict VerdictFor(CheckResult result, DeviceProfile device)
    {
        return result.Devices.Single(d => d.Device == device).Verdict;
    }

    [Fact]
    public void Check_GivesOneVerdictPerDevice_InOrder()
    {
        CheckResult result = CheckWith(FakeModelParser.ResultWith(triangles: 10));

        Assert.Equal(3, result.Devices.Count);
        Assert.Equal(DeviceProfile.Phone, result.Devices[0].Device);
        Assert.Equal(DeviceProfile.Tablet, result.Devices[1].Device);
        Assert.Equal(DeviceProfile.Desktop, result.Devices[2].Device);
    }

    [Fact]
    public void Check_PassesTheFileTextToTheParser_AndReturnsItsResult()
    {
        ParseResult parseResult = FakeModelParser.ResultWith(triangles: 10);
        FakeModelParser fake = new FakeModelParser(parseResult);
        Core.ModelChecker checker = new Core.ModelChecker(fake);

        CheckResult result = checker.Check("v 0 0 0");

        Assert.Equal("v 0 0 0", fake.LastFileText);
        Assert.Same(parseResult, result.Model);
    }

    [Fact]
    public void SmallModel_IsReadyEverywhere()
    {
        CheckResult result = CheckWith(FakeModelParser.ResultWith(triangles: 656));

        Assert.All(result.Devices, d => Assert.Equal(Verdict.Ready, d.Verdict));
    }

    [Fact]
    public void MediumModel_IsTooHeavyForPhoneOnly()
    {
        // Over the phone's 100,000 but under the tablet's 300,000.
        CheckResult result = CheckWith(FakeModelParser.ResultWith(triangles: 156_334));

        Assert.Equal(Verdict.TooHeavy, VerdictFor(result, DeviceProfile.Phone));
        Assert.Equal(Verdict.Ready, VerdictFor(result, DeviceProfile.Tablet));
        Assert.Equal(Verdict.Ready, VerdictFor(result, DeviceProfile.Desktop));
    }

    [Fact]
    public void HugeModel_IsTooHeavyEverywhere()
    {
        CheckResult result = CheckWith(FakeModelParser.ResultWith(triangles: 2_000_000));

        Assert.All(result.Devices, d => Assert.Equal(Verdict.TooHeavy, d.Verdict));
    }

    [Fact]
    public void ModelWithWarnings_IsBrokenEverywhere()
    {
        CheckResult result = CheckWith(
            FakeModelParser.ResultWith(triangles: 10, "Line 4: a face points at vertex 9."));

        Assert.All(result.Devices, d => Assert.Equal(Verdict.Broken, d.Verdict));
    }

    [Fact]
    public void ModelWithNoTriangles_IsBrokenEverywhere()
    {
        // An empty file has no warnings, but there is nothing to draw.
        CheckResult result = CheckWith(FakeModelParser.ResultWith(triangles: 0));

        Assert.All(result.Devices, d => Assert.Equal(Verdict.Broken, d.Verdict));
    }

    [Fact]
    public void Broken_WinsOverTooHeavy()
    {
        // Too many triangles AND warnings: the file being damaged is the more
        // important thing to tell the user.
        CheckResult result = CheckWith(
            FakeModelParser.ResultWith(triangles: 5_000_000, "Line 1: something is wrong."));

        Assert.All(result.Devices, d => Assert.Equal(Verdict.Broken, d.Verdict));
    }

    // Boundary test: the values right around the phone budget of 100,000.
    // Off-by-one mistakes (> instead of >=) hide exactly here.
    [Theory]
    [InlineData(99_999, Verdict.Ready)]      // one under the budget
    [InlineData(100_000, Verdict.Ready)]     // exactly on the budget still fits
    [InlineData(100_001, Verdict.TooHeavy)]  // one over
    public void PhoneBudget_Boundary(int triangles, Verdict expected)
    {
        CheckResult result = CheckWith(FakeModelParser.ResultWith(triangles));

        Assert.Equal(expected, VerdictFor(result, DeviceProfile.Phone));
    }

    [Fact]
    public void DeviceBudgets_AreTheAgreedNumbers()
    {
        Assert.Equal(100_000, DeviceProfile.Phone.TriangleBudget);
        Assert.Equal(300_000, DeviceProfile.Tablet.TriangleBudget);
        Assert.Equal(1_000_000, DeviceProfile.Desktop.TriangleBudget);
    }
}
