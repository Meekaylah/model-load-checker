using ModelChecker.Web;

namespace ModelChecker.Tests;

// Tests for the small text-formatting helpers the UI uses.
public class FormatTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(656, "656")]
    [InlineData(156_334, "156,334")]
    [InlineData(1_000_000, "1,000,000")]
    public void Number_AddsThousandsSeparators(int value, string expected)
    {
        Assert.Equal(expected, Format.Number(value));
    }

    [Theory]
    [InlineData(0, "0 bytes")]
    [InlineData(1023, "1023 bytes")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(50_342, "49.2 KB")]
    [InlineData(22_084_413, "21.1 MB")]
    [InlineData(50 * 1024 * 1024, "50.0 MB")]
    public void FileSize_PicksASensibleUnit(long bytes, string expected)
    {
        Assert.Equal(expected, Format.FileSize(bytes));
    }

    [Theory]
    [InlineData(2, "2")]
    [InlineData(0.5, "0.5")]
    [InlineData(0.33541234, "0.335")]
    public void Dimension_KeepsUpToThreeDecimals(double value, string expected)
    {
        Assert.Equal(expected, Format.Dimension(value));
    }
}
