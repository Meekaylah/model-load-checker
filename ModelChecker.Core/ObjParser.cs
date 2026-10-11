using System.Globalization;

namespace ModelChecker.Core;

public class ObjParser : IModelParser
{
    public ParseResult Parse(string fileText)
    {
        List<ThreeDPoint> points = new List<ThreeDPoint>();

        string[] lines = fileText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        int triangleCounter = 0;
        List<string> warnings = new List<string>();

        foreach (string line in lines)
        {
            if (line.StartsWith("v "))
            {
                string[] splitLine = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                double x = double.Parse(splitLine[1], CultureInfo.InvariantCulture);
                double y = double.Parse(splitLine[2], CultureInfo.InvariantCulture);
                double z = double.Parse(splitLine[3], CultureInfo.InvariantCulture);

                points.Add(new ThreeDPoint(x, y, z));
            }
            else if (line.StartsWith("f "))
            {
                string[] splitLine = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                int corners = splitLine.Length - 1;
                if (corners < 3)
                {
                    warnings.Add($"A face has only {corners} corners when it needs at least 3");
                }
                else
                {
                    triangleCounter += corners - 2;

                    for (int i = 1; i <= corners; i++)
                    {
                        string[] chunkParts = splitLine[i].Split('/');

                        int vertexIndex = int.Parse(chunkParts[0], CultureInfo.InvariantCulture);

                        int position = vertexIndex;

                        if (vertexIndex < 0)
                        {
                            position = points.Count + vertexIndex + 1;
                        }

                        if (position < 1 || position > points.Count)
                        {
                            warnings.Add($"A face points at vertex {vertexIndex} when only {points.Count} vertices have been read.");
                        }

                    }
                }
            }
        }

        return new ParseResult(points.Count, triangleCounter, new BoundingBox(new ThreeDPoint(0,0,0), new ThreeDPoint(0,0,0)), warnings);
    }
}