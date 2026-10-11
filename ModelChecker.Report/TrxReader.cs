using System.Globalization;
using System.Xml.Linq;

namespace ModelChecker.Report;

// Reads a TRX file. TRX is the results file "dotnet test" writes when you ask for
// the trx logger. It is XML and looks roughly like this:
//
//   <TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
//     <Times start="..." finish="..." />
//     <Results>
//       <UnitTestResult testName="..." outcome="Passed" duration="00:00:00.0156370" />
//       <UnitTestResult testName="..." outcome="Failed" duration="...">
//         <Output><ErrorInfo><Message>what went wrong</Message></ErrorInfo></Output>
//       </UnitTestResult>
//     </Results>
//   </TestRun>
public static class TrxReader
{
    // Every element in a TRX file belongs to this XML namespace. When we look for
    // an element we must include it (Trx + "Results"), otherwise nothing is found.
    private static readonly XNamespace Trx = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

    public static TestRun Read(string path)
    {
        XDocument document = XDocument.Load(path);

        List<TestResult> tests = new List<TestResult>();

        // Descendants finds every <UnitTestResult> anywhere in the file.
        foreach (XElement element in document.Descendants(Trx + "UnitTestResult"))
        {
            tests.Add(ReadTest(element));
        }

        // Sort by name so the report is in the same order every time.
        // (The file lists tests in whatever order they happened to finish.)
        tests.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        return new TestRun(tests, ReadRunDuration(document, tests));
    }

    private static TestResult ReadTest(XElement element)
    {
        // "?." and "??" mean: if the attribute is missing, use the fallback
        // instead of crashing.
        string name = element.Attribute("testName")?.Value ?? "(unnamed test)";
        string outcomeText = element.Attribute("outcome")?.Value ?? "";

        // Anything that is not clearly passed or skipped is treated as a failure
        // (for example "Error" or "Timeout"). It is safer for a report to
        // over-report problems than to hide them.
        TestOutcome outcome = outcomeText switch
        {
            "Passed" => TestOutcome.Passed,
            "NotExecuted" => TestOutcome.Skipped,
            _ => TestOutcome.Failed
        };

        TimeSpan.TryParse(element.Attribute("duration")?.Value, CultureInfo.InvariantCulture, out TimeSpan duration);

        string? message = null;

        if (outcome == TestOutcome.Failed)
        {
            message = element
                .Element(Trx + "Output")?
                .Element(Trx + "ErrorInfo")?
                .Element(Trx + "Message")?
                .Value.Trim();
        }

        return new TestResult(name, outcome, duration, message);
    }

    // How long the whole run took, from the <Times start finish> element.
    // If that is missing we fall back to adding up the individual tests.
    private static TimeSpan ReadRunDuration(XDocument document, List<TestResult> tests)
    {
        XElement? times = document.Descendants(Trx + "Times").FirstOrDefault();

        bool hasStart = DateTimeOffset.TryParse(
            times?.Attribute("start")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset start);
        bool hasFinish = DateTimeOffset.TryParse(
            times?.Attribute("finish")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset finish);

        if (hasStart && hasFinish && finish >= start)
        {
            return finish - start;
        }

        TimeSpan total = TimeSpan.Zero;

        foreach (TestResult test in tests)
        {
            total += test.Duration;
        }

        return total;
    }
}
