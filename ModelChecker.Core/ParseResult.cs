namespace ModelChecker.Core;

public record ParseResult(int VertexCount, int TriangleCount, BoundingBox BoundingBox, List<string> Warnings);