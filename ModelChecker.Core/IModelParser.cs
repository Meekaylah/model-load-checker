namespace ModelChecker.Core;

public interface IModelParser
{
    ParseResult Parse(string fileText);
}