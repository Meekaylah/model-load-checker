using System.Globalization;
using ModelChecker.Core;

namespace ModelChecker.Tests;

// Tests for ObjParser. Each test writes a tiny .obj file as text right inside the
// test, so you can see the input and the expected output side by side.
//
// The """ ... """ blocks are "raw string literals": multi-line text exactly as typed.
// The first line of text inside is line 1 of the pretend file.
public class ObjParserTests
{
    // A fresh parser for every test. xUnit creates a new instance of this class
    // per test, so tests can't affect each other.
    private readonly ObjParser _parser = new ObjParser();

    // ---------- Normal shapes ----------

    [Fact]
    public void Triangle_HasThreeVerticesAndOneTriangle()
    {
        string obj = """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 3
            """;

        ParseResult result = _parser.Parse(obj);

        Assert.Equal(3, result.VertexCount);
        Assert.Equal(1, result.TriangleCount);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Quad_CountsAsTwoTriangles()
    {
        // A face with 4 corners is cut into 4 - 2 = 2 triangles.
        string obj = """
            v 0 0 0
            v 1 0 0
            v 1 1 0
            v 0 1 0
            f 1 2 3 4
            """;

        ParseResult result = _parser.Parse(obj);

        Assert.Equal(4, result.VertexCount);
        Assert.Equal(2, result.TriangleCount);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Cube_HasEightVerticesAndTwelveTriangles()
    {
        // 6 square sides, each worth 2 triangles = 12.
        string obj = """
            # A cube that is 2 wide, 3 high and 4 deep
            v 0 0 0
            v 2 0 0
            v 2 3 0
            v 0 3 0
            v 0 0 4
            v 2 0 4
            v 2 3 4
            v 0 3 4

            f 1 2 3 4
            f 5 6 7 8
            f 1 2 6 5
            f 2 3 7 6
            f 3 4 8 7
            f 4 1 5 8
            """;

        ParseResult result = _parser.Parse(obj);

        Assert.Equal(8, result.VertexCount);
        Assert.Equal(12, result.TriangleCount);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Cube_BoundingBoxMatchesItsSize()
    {
        // This cube doesn't start at 0,0,0 on purpose, to prove the box
        // is measured from the real smallest point and not from the origin.
        string obj = """
            v 1 1 1
            v 3 1 1
            v 3 4 1
            v 1 4 1
            v 1 1 5
            v 3 1 5
            v 3 4 5
            v 1 4 5
            f 1 2 3 4
            """;

        BoundingBox box = _parser.Parse(obj).BoundingBox;

        Assert.Equal(new ThreeDPoint(1, 1, 1), box.Min);
        Assert.Equal(new ThreeDPoint(3, 4, 5), box.Max);
        Assert.Equal(2, box.Width);
        Assert.Equal(3, box.Height);
        Assert.Equal(4, box.Depth);
    }

    // ---------- Face formats ----------

    // A Theory is one test run several times with different inputs.
    // Each InlineData line is one run.
    [Theory]
    [InlineData("f 1 2 3")]              // vertex only
    [InlineData("f 1/1 2/2 3/3")]        // vertex/texture
    [InlineData("f 1/1/1 2/2/2 3/3/3")]  // vertex/texture/normal
    [InlineData("f 1//1 2//2 3//3")]     // vertex//normal
    public void Face_AllFourCornerFormatsAreUnderstood(string faceLine)
    {
        string obj = "v 0 0 0\nv 1 0 0\nv 0 1 0\n" + faceLine;

        ParseResult result = _parser.Parse(obj);

        Assert.Equal(1, result.TriangleCount);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Face_NegativeNumbersCountBackFromTheLatestVertex()
    {
        // -1 is the most recent vertex, -2 the one before, and so on.
        string obj = """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f -3 -2 -1
            """;

        ParseResult result = _parser.Parse(obj);

        Assert.Equal(1, result.TriangleCount);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void OtherLineTypes_AreIgnoredWithoutWarnings()
    {
        // Real files are full of lines we don't need. They must not cause warnings,
        // otherwise every real model would be marked Broken.
        string obj = """
            # a comment
            mtllib chair.mtl
            o Chair
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vt 0.5 0.5
            vn 0 0 1
            usemtl wood
            s off
            f 1/1/1 2/1/1 3/1/1
            """;

        ParseResult result = _parser.Parse(obj);

        Assert.Equal(3, result.VertexCount);
        Assert.Equal(1, result.TriangleCount);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void WindowsLineEndings_AreHandled()
    {
        // Files saved on Windows end lines with \r\n instead of \n.
        string obj = "v 0 0 0\r\nv 1 0 0\r\nv 0 1 0\r\nf 1 2 3\r\n";

        ParseResult result = _parser.Parse(obj);

        Assert.Equal(3, result.VertexCount);
        Assert.Equal(1, result.TriangleCount);
        Assert.Empty(result.Warnings);
    }

    // ---------- Edge cases: bad input must warn, never crash ----------

    [Fact]
    public void EmptyFile_GivesZeroCountsAndNoWarnings()
    {
        ParseResult result = _parser.Parse("");

        Assert.Equal(0, result.VertexCount);
        Assert.Equal(0, result.TriangleCount);
        Assert.Empty(result.Warnings);

        // No vertices means a zero-sized box, not a crash or a null.
        Assert.Equal(0, result.BoundingBox.Width);
        Assert.Equal(0, result.BoundingBox.Height);
        Assert.Equal(0, result.BoundingBox.Depth);
    }

    [Fact]
    public void BadNumber_AddsWarningWithLineNumberAndSkipsTheVertex()
    {
        string obj = """
            v 0 0 0
            v 1 oops 0
            v 0 1 0
            """;

        ParseResult result = _parser.Parse(obj);

        // The bad vertex is left out, the two good ones are kept.
        Assert.Equal(2, result.VertexCount);

        // Assert.Single checks there is exactly one item and gives it back to us.
        string warning = Assert.Single(result.Warnings);
        Assert.StartsWith("Line 2:", warning);
    }

    [Fact]
    public void VertexWithTooFewNumbers_AddsWarning()
    {
        string obj = "v 1 2";

        ParseResult result = _parser.Parse(obj);

        Assert.Equal(0, result.VertexCount);
        string warning = Assert.Single(result.Warnings);
        Assert.StartsWith("Line 1:", warning);
    }

    [Fact]
    public void MissingVertex_AddsWarningAndDoesNotCountTheFace()
    {
        // The face asks for vertex 4 but the file only has 3.
        string obj = """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 4
            """;

        ParseResult result = _parser.Parse(obj);

        Assert.Equal(0, result.TriangleCount);
        string warning = Assert.Single(result.Warnings);
        Assert.StartsWith("Line 4:", warning);
        Assert.Contains("vertex 4", warning);
    }

    [Fact]
    public void FaceWithTwoCorners_AddsWarningAndDoesNotCountTheFace()
    {
        string obj = """
            v 0 0 0
            v 1 0 0
            f 1 2
            """;

        ParseResult result = _parser.Parse(obj);

        Assert.Equal(0, result.TriangleCount);
        string warning = Assert.Single(result.Warnings);
        Assert.StartsWith("Line 3:", warning);
    }

    [Fact]
    public void FaceCornerThatIsNotANumber_AddsWarning()
    {
        string obj = """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 x
            """;

        ParseResult result = _parser.Parse(obj);

        Assert.Equal(0, result.TriangleCount);
        string warning = Assert.Single(result.Warnings);
        Assert.StartsWith("Line 4:", warning);
    }

    [Fact]
    public void LineNumbers_CountBlankLinesAndComments()
    {
        // The warning must point at the line a text editor would show (line 5),
        // so blank lines and comments have to be counted too.
        string obj = """
            # comment

            v 0 0 0

            v bad 0 0
            """;

        ParseResult result = _parser.Parse(obj);

        string warning = Assert.Single(result.Warnings);
        Assert.StartsWith("Line 5:", warning);
    }

    [Fact]
    public void GoodFacesAreStillCounted_WhenAnotherFaceIsBroken()
    {
        // One bad line should not stop the parser reading the rest of the file.
        string obj = """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            f 1 2 99
            f 1 2 3
            """;

        ParseResult result = _parser.Parse(obj);

        Assert.Equal(1, result.TriangleCount);
        Assert.Single(result.Warnings);
    }

    // ---------- Culture ----------

    [Fact]
    public void DecimalPoints_AreReadCorrectlyOnAComputerSetToGerman()
    {
        // In Germany "1.5" is written "1,5". If the parser used the computer's own
        // settings it would misread every .obj file there. This test switches the
        // current thread to German and checks we still read 1.5 as one and a half.
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            ParseResult result = _parser.Parse("v 1.5 2.25 -0.5");

            Assert.Empty(result.Warnings);
            Assert.Equal(new ThreeDPoint(1.5, 2.25, -0.5), result.BoundingBox.Min);
        }
        finally
        {
            // "finally" always runs, even if an Assert fails, so we never leave
            // the German setting switched on for other tests.
            CultureInfo.CurrentCulture = original;
        }
    }
}
