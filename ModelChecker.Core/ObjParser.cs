using System.Globalization;

namespace ModelChecker.Core;

// Reads the text of a Wavefront .obj file and counts what is in it.
//
// An .obj file is plain text, one instruction per line. We only care about two kinds:
//   v 1.0 2.0 3.0      a vertex (a point in 3D space)
//   f 1 2 3            a face (a flat shape joining vertices 1, 2 and 3)
//
// The parser never throws on bad input. A broken line becomes a warning with its
// line number, and we carry on, so the user gets a full list of what is wrong.
public class ObjParser : IModelParser
{
    public ParseResult Parse(string fileText)
    {
        List<ThreeDPoint> points = new List<ThreeDPoint>();
        List<string> warnings = new List<string>();
        int triangleCounter = 0;

        // StringReader hands us one line at a time. We use it instead of Split('\n') because:
        //  - blank lines still count, so our line numbers match what a text editor shows
        //  - it understands both Mac/Linux (\n) and Windows (\r\n) line endings
        //  - it doesn't build one huge array for a 20 MB file
        using StringReader reader = new StringReader(fileText);

        int lineNumber = 0;
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            lineNumber++;

            // Split on spaces and tabs, dropping the empty bits left by double spaces.
            // "v  1.0 2.0 3.0 " becomes ["v", "1.0", "2.0", "3.0"].
            string[] parts = line.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

            // Blank line: nothing to do.
            if (parts.Length == 0)
            {
                continue;
            }

            // The first word says what kind of line this is.
            if (parts[0] == "v")
            {
                ThreeDPoint? point = ParseVertex(parts, lineNumber, warnings);

                if (point != null)
                {
                    points.Add(point);
                }
            }
            else if (parts[0] == "f")
            {
                triangleCounter += ParseFace(parts, lineNumber, points.Count, warnings);
            }

            // Anything else (comments "#", texture coordinates "vt", normals "vn",
            // materials "usemtl"...) is valid .obj but doesn't affect our counts, so we skip it.
        }

        return new ParseResult(points.Count, triangleCounter, BoundingBox.FromPoints(points), warnings);
    }

    private static readonly char[] Separators = { ' ', '\t' };

    // Turns ["v", "1.0", "2.0", "3.0"] into a point.
    // Returns null (and adds a warning) if the line can't be read.
    private static ThreeDPoint? ParseVertex(string[] parts, int lineNumber, List<string> warnings)
    {
        // parts[0] is "v", so we need at least 3 more for x, y and z.
        // (Some files add a 4th number, w. We ignore it.)
        if (parts.Length < 4)
        {
            warnings.Add($"Line {lineNumber}: a vertex needs 3 numbers (x y z) but has {parts.Length - 1}.");
            return null;
        }

        // TryParse returns false instead of throwing when the text isn't a number.
        //
        // InvariantCulture matters: .obj files always use a dot for decimals ("1.5").
        // Without it, a computer set to German or French would expect "1,5" and
        // read the file wrongly.
        bool xOk = double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double x);
        bool yOk = double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double y);
        bool zOk = double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double z);

        if (!xOk || !yOk || !zOk)
        {
            warnings.Add($"Line {lineNumber}: a vertex has a value that is not a number (\"{parts[1]} {parts[2]} {parts[3]}\").");
            return null;
        }

        return new ThreeDPoint(x, y, z);
    }

    // Checks one face line and returns how many triangles it adds.
    // Returns 0 (and adds one warning) if the face is broken.
    private static int ParseFace(string[] parts, int lineNumber, int verticesSoFar, List<string> warnings)
    {
        // parts[0] is "f"; every part after it is one corner of the face.
        int corners = parts.Length - 1;

        if (corners < 3)
        {
            warnings.Add($"Line {lineNumber}: a face has only {corners} corners when it needs at least 3.");
            return 0;
        }

        for (int i = 1; i <= corners; i++)
        {
            // A corner can be written four ways:
            //   7          just the vertex
            //   7/2        vertex / texture coordinate
            //   7/2/5      vertex / texture coordinate / normal
            //   7//5       vertex / (no texture) / normal
            // In all of them the vertex number is the bit before the first slash,
            // which is all we need.
            string vertexText = parts[i].Split('/')[0];

            if (!int.TryParse(vertexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int vertexIndex))
            {
                warnings.Add($"Line {lineNumber}: a face has a corner \"{parts[i]}\" that is not a vertex number.");
                return 0;
            }

            // .obj counts vertices from 1, not 0.
            // A negative number counts backwards from the latest vertex: -1 is the most recent one.
            int position = vertexIndex;

            if (vertexIndex < 0)
            {
                position = verticesSoFar + vertexIndex + 1;
            }

            if (position < 1 || position > verticesSoFar)
            {
                warnings.Add($"Line {lineNumber}: a face points at vertex {vertexIndex} when only {verticesSoFar} vertices have been read.");
                return 0;
            }
        }

        // A graphics card only draws triangles, so a face with more corners gets cut up.
        // A shape with n corners always splits into n - 2 triangles:
        // triangle (3) -> 1, quad (4) -> 2, pentagon (5) -> 3.
        return corners - 2;
    }
}
