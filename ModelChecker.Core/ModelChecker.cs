namespace ModelChecker.Core;

// Decides whether a model will load on each device.
//
// Naming note: this class has the same name as the first part of our namespace
// (ModelChecker.Core). Inside this project that is fine, but in the Tests and Web
// projects the word "ModelChecker" on its own means the namespace, so they have to
// write "Core.ModelChecker" to mean this class.
public class ModelChecker
{
    private readonly IModelParser _parser;

    // We ask for "something that can parse" (the interface) rather than creating an
    // ObjParser ourselves. This is dependency injection. It means:
    //  - tests can pass in a fake parser and check the verdict rules on their own
    //  - a new file format later only needs a new IModelParser, not changes here
    public ModelChecker(IModelParser parser)
    {
        _parser = parser;
    }

    // Parses the file text, then works out a verdict for every device.
    public CheckResult Check(string fileText)
    {
        ParseResult model = _parser.Parse(fileText);

        List<DeviceVerdict> devices = new List<DeviceVerdict>();

        foreach (DeviceProfile device in DeviceProfile.All)
        {
            devices.Add(new DeviceVerdict(device, GetVerdict(model, device)));
        }

        return new CheckResult(model, devices);
    }

    // The rules, in order of importance.
    public static Verdict GetVerdict(ParseResult model, DeviceProfile device)
    {
        // Broken comes first: if the file is damaged, the triangle count can't be trusted,
        // so it makes no sense to compare it with a budget.
        // A model with no triangles has nothing to draw, so we call that broken too.
        if (model.Warnings.Count > 0 || model.TriangleCount == 0)
        {
            return Verdict.Broken;
        }

        // Strictly greater than: a model with exactly the budget still fits.
        if (model.TriangleCount > device.TriangleBudget)
        {
            return Verdict.TooHeavy;
        }

        return Verdict.Ready;
    }
}
