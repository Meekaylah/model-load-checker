using System.Net;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using ModelChecker.Core;
using ModelChecker.Web.Pages;

namespace ModelChecker.Tests;

// bUnit tests for the whole Home page: the headline, the error state and the results.
//
// The page gets its services from dependency injection, so each test registers
// the ones it wants - a real or fake parser, and an HttpClient that never
// touches the real network.
public class HomePageTests : BunitContext
{
    private const string TriangleObj = "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 3\n";

    public HomePageTests()
    {
        // InputFile talks to JavaScript in a real browser. There is no browser here,
        // so "Loose" tells bUnit to quietly accept those calls.
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // Registers the same services Program.cs does, but with test doubles where we choose.
    private IRenderedComponent<Home> RenderHome(IModelParser parser, StubHttpHandler? http = null)
    {
        http ??= new StubHttpHandler(HttpStatusCode.OK, TriangleObj);

        Services.AddSingleton(parser);
        Services.AddSingleton<Core.ModelChecker>();
        Services.AddSingleton(new HttpClient(http) { BaseAddress = new Uri("http://localhost/") });

        return Render<Home>();
    }

    // Picks a file in the page's file input, as if the user had chosen it.
    private static void Upload(IRenderedComponent<Home> page, string fileName, string content)
    {
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(content, fileName));
    }

    [Fact]
    public void ShowsTheHeadline_AndNothingElseAtFirst()
    {
        IRenderedComponent<Home> page = RenderHome(new ObjParser());

        Assert.Equal("Will your 3D model load on a phone?", page.Find("h1").TextContent);

        // No results and no error until the user does something.
        Assert.Empty(page.FindAll(".device-card"));
        Assert.Empty(page.FindAll("[role=alert]"));
    }

    [Fact]
    public void ShowsASampleButtonForEachSample()
    {
        IRenderedComponent<Home> page = RenderHome(new ObjParser());

        Assert.Equal(4, page.FindAll(".picker__buttons button").Count);
    }

    // ---------- Error state ----------

    [Fact]
    public void UploadingTheWrongFileType_ShowsAFriendlyError()
    {
        IRenderedComponent<Home> page = RenderHome(new ObjParser());

        Upload(page, "holiday.png", "not a model");

        // The page works asynchronously, so we wait until the assertion becomes true
        // (it fails the test if that doesn't happen within a second or so).
        page.WaitForAssertion(() =>
        {
            string error = page.Find("[role=alert]").TextContent;
            Assert.Contains("holiday.png", error);
            Assert.Contains("not a .obj file", error);
        });

        // An error means no results are shown.
        Assert.Empty(page.FindAll(".device-card"));
    }

    [Fact]
    public void SampleThatFailsToLoad_ShowsAFriendlyError()
    {
        // The pretend server answers "404 Not Found".
        StubHttpHandler notFound = new StubHttpHandler(HttpStatusCode.NotFound);
        IRenderedComponent<Home> page = RenderHome(new ObjParser(), notFound);

        page.Find(".picker__buttons button").Click();

        page.WaitForAssertion(() =>
            Assert.Contains("could not be loaded", page.Find("[role=alert]").TextContent));

        Assert.Empty(page.FindAll(".device-card"));

        // The buttons must be usable again so the user can retry.
        Assert.False(page.Find(".picker__buttons button").HasAttribute("disabled"));
    }

    [Fact]
    public void AGoodCheckAfterAnError_ClearsTheError()
    {
        IRenderedComponent<Home> page = RenderHome(new ObjParser());

        Upload(page, "holiday.png", "not a model");
        page.WaitForAssertion(() => Assert.Single(page.FindAll("[role=alert]")));

        Upload(page, "triangle.obj", TriangleObj);
        page.WaitForAssertion(() => Assert.Equal(3, page.FindAll(".device-card").Count));

        Assert.Empty(page.FindAll("[role=alert]"));
    }

    // ---------- Results ----------

    [Fact]
    public void ClickingASample_LoadsItAndShowsACardPerDevice()
    {
        StubHttpHandler http = new StubHttpHandler(HttpStatusCode.OK, TriangleObj);
        IRenderedComponent<Home> page = RenderHome(new ObjParser(), http);

        page.Find(".picker__buttons button").Click();

        page.WaitForAssertion(() => Assert.Equal(3, page.FindAll(".device-card").Count));

        // The first sample button is the light chair, fetched from wwwroot/samples.
        Assert.Equal("/samples/light-chair.obj", http.LastRequestPath);

        // One triangle is well inside every budget, so all three cards are green.
        Assert.Equal(3, page.FindAll(".device-card--ready").Count);
        Assert.Equal("light-chair.obj", page.Find(".results h2").TextContent);
    }

    [Fact]
    public void UploadingAHeavyModel_ShowsOrangeForPhoneAndGreenForTheRest()
    {
        // The fake parser lets us pretend the file has 156,334 triangles
        // without writing a file that big.
        FakeModelParser parser = new FakeModelParser(FakeModelParser.ResultWith(triangles: 156_334));
        IRenderedComponent<Home> page = RenderHome(parser);

        Upload(page, "big.obj", "the fake parser ignores this text");

        page.WaitForAssertion(() => Assert.Equal(3, page.FindAll(".device-card").Count));

        Assert.Single(page.FindAll(".device-card--too-heavy"));
        Assert.Equal(2, page.FindAll(".device-card--ready").Count);
        Assert.Contains("156,334", page.Find(".stats").TextContent);
    }

    [Fact]
    public void UploadingABrokenModel_ShowsRedCardsAndTheWarnings()
    {
        // Real parser, real broken file: the face points at vertex 9, which doesn't exist.
        IRenderedComponent<Home> page = RenderHome(new ObjParser());

        Upload(page, "broken.obj", "v 0 0 0\nv 1 0 0\nv 0 1 0\nf 1 2 9\n");

        page.WaitForAssertion(() => Assert.Equal(3, page.FindAll(".device-card--broken").Count));

        string warning = Assert.Single(page.FindAll(".warnings li")).TextContent;
        Assert.StartsWith("Line 4:", warning);
    }

    [Fact]
    public void LongWarningLists_AreCutToTwentyWithACountOfTheRest()
    {
        string[] warnings = Enumerable.Range(1, 25).Select(n => $"Line {n}: problem.").ToArray();
        FakeModelParser parser = new FakeModelParser(FakeModelParser.ResultWith(10, warnings));
        IRenderedComponent<Home> page = RenderHome(parser);

        Upload(page, "messy.obj", "ignored");

        page.WaitForAssertion(() => Assert.Equal(20, page.FindAll(".warnings li").Count));
        Assert.Contains("and 5 more", page.Find(".warnings__more").TextContent);
    }

    [Fact]
    public void UploadingAnEmptyModel_ExplainsThereIsNothingToDraw()
    {
        IRenderedComponent<Home> page = RenderHome(new ObjParser());

        Upload(page, "empty.obj", "");

        page.WaitForAssertion(() => Assert.Equal(3, page.FindAll(".device-card--broken").Count));
        Assert.Contains("No triangles were found", page.Find(".results").TextContent);
    }
}
