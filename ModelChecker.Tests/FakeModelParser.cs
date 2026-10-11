using ModelChecker.Core;

namespace ModelChecker.Tests;

// A pretend parser for tests. It ignores the file text and just returns whatever
// result the test gave it.
//
// Why: ModelChecker tests are about the verdict rules, not about reading .obj files.
// With a fake we can say "pretend the model has exactly 100,001 triangles" in one
// line, instead of writing a 100,001-triangle file.
public class FakeModelParser : IModelParser
{
    private readonly ParseResult _result;

    public FakeModelParser(ParseResult result)
    {
        _result = result;
    }

    // Lets a test check what text ModelChecker passed to the parser.
    public string? LastFileText { get; private set; }

    public ParseResult Parse(string fileText)
    {
        LastFileText = fileText;
        return _result;
    }

    // Shortcut for building a ParseResult when a test only cares about
    // the triangle count and the warnings.
    public static ParseResult ResultWith(int triangles, params string[] warnings)
    {
        ThreeDPoint origin = new ThreeDPoint(0, 0, 0);

        return new ParseResult(
            VertexCount: triangles * 3,
            TriangleCount: triangles,
            BoundingBox: new BoundingBox(origin, origin),
            Warnings: warnings.ToList());
    }
}
