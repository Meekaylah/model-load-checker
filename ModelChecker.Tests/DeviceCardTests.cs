using Bunit;
using ModelChecker.Core;
using ModelChecker.Web.Components;

namespace ModelChecker.Tests;

// bUnit tests for the DeviceCard component. bUnit renders a Blazor component in
// memory (no browser) and lets us look at the HTML it produced - much like
// React Testing Library.
//
// Inheriting from BunitContext gives each test the Render method.
public class DeviceCardTests : BunitContext
{
    // Renders a phone card with the given verdict and triangle count.
    private IRenderedComponent<DeviceCard> RenderPhoneCard(Verdict verdict, int triangles)
    {
        DeviceVerdict result = new DeviceVerdict(DeviceProfile.Phone, verdict);

        // This is the test version of <DeviceCard Result="result" TriangleCount="triangles" />
        return Render<DeviceCard>(parameters => parameters
            .Add(card => card.Result, result)
            .Add(card => card.TriangleCount, triangles));
    }

    // The colour comes from a CSS class, so checking the class checks the colour.
    [Theory]
    [InlineData(Verdict.Ready, "device-card--ready", "Ready")]
    [InlineData(Verdict.TooHeavy, "device-card--too-heavy", "Too heavy")]
    [InlineData(Verdict.Broken, "device-card--broken", "Broken")]
    public void ShowsTheRightColourClassAndLabel(Verdict verdict, string expectedClass, string expectedLabel)
    {
        IRenderedComponent<DeviceCard> card = RenderPhoneCard(verdict, triangles: 500);

        // Find uses a CSS selector, like document.querySelector.
        Assert.Contains(expectedClass, card.Find("article").ClassList);
        Assert.Equal(expectedLabel, card.Find(".device-card__verdict").TextContent);
    }

    [Fact]
    public void ShowsTheDeviceNameAndBudget()
    {
        IRenderedComponent<DeviceCard> card = RenderPhoneCard(Verdict.Ready, triangles: 656);

        Assert.Equal("Phone", card.Find(".device-card__name").TextContent);
        Assert.Contains("Budget: 100,000 triangles", card.Find(".device-card__budget").TextContent);
    }

    [Fact]
    public void Ready_SaysHowMuchOfTheBudgetIsUsed()
    {
        IRenderedComponent<DeviceCard> card = RenderPhoneCard(Verdict.Ready, triangles: 656);

        Assert.Equal("Uses 656 of 100,000 triangles.", card.Find(".device-card__detail").TextContent);
    }

    [Fact]
    public void TooHeavy_SaysHowFarOverTheBudgetItIs()
    {
        // 156,334 - 100,000 = 56,334 over.
        IRenderedComponent<DeviceCard> card = RenderPhoneCard(Verdict.TooHeavy, triangles: 156_334);

        Assert.Equal("56,334 triangles over the budget.", card.Find(".device-card__detail").TextContent);
    }

    [Fact]
    public void Bar_ShowsThePercentageUsed_AndStopsAtOneHundred()
    {
        IRenderedComponent<DeviceCard> half = RenderPhoneCard(Verdict.Ready, triangles: 50_000);
        Assert.Contains("width: 50%", half.Find(".device-card__bar-fill").GetAttribute("style"));

        IRenderedComponent<DeviceCard> over = RenderPhoneCard(Verdict.TooHeavy, triangles: 250_000);
        Assert.Contains("width: 100%", over.Find(".device-card__bar-fill").GetAttribute("style"));
    }

    [Fact]
    public void Broken_HasNoUsageBar()
    {
        // A triangle count from a damaged file can't be trusted, so no bar is drawn.
        IRenderedComponent<DeviceCard> card = RenderPhoneCard(Verdict.Broken, triangles: 500);

        Assert.Empty(card.FindAll(".device-card__bar"));
    }
}
