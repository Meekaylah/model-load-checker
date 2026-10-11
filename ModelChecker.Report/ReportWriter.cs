using System.Globalization;
using System.Text;

namespace ModelChecker.Report;

// Turns a TestRun into the plain-text report.
// It only builds a string; saving it to a file is Program.cs's job. Keeping the two
// apart means this class has nothing to do with the disk and is easy to reason about.
public static class ReportWriter
{
    public static string Write(TestRun run)
    {
        // StringBuilder is the efficient way to build a long string piece by piece.
        StringBuilder report = new StringBuilder();

        report.AppendLine("Model Load Checker - Test Report");
        report.AppendLine("================================");
        report.AppendLine();

        foreach (TestResult test in run.Tests)
        {
            // PASS, FAIL and SKIP are padded to the same width so the names line up.
            string label = test.Outcome switch
            {
                TestOutcome.Passed => "PASS",
                TestOutcome.Failed => "FAIL",
                _ => "SKIP"
            };

            report.AppendLine($"{label}  {test.Name}");

            if (test.Outcome == TestOutcome.Failed)
            {
                WriteFailureMessage(report, test.FailureMessage);
            }
        }

        report.AppendLine();
        report.Append(WriteSummary(run));

        return report.ToString();
    }

    // The summary block on its own, so Program.cs can also print it to the console.
    public static string WriteSummary(TestRun run)
    {
        StringBuilder summary = new StringBuilder();

        summary.AppendLine("Summary");
        summary.AppendLine("-------");
        summary.AppendLine($"Passed:    {run.Passed}");
        summary.AppendLine($"Failed:    {run.Failed}");

        // Only mention skipped tests when there are some, to keep the usual report short.
        if (run.Skipped > 0)
        {
            summary.AppendLine($"Skipped:   {run.Skipped}");
        }

        summary.AppendLine($"Total:     {run.Total}");

        // InvariantCulture so the report always says "97.1%" and never "97,1%",
        // whichever computer it runs on.
        summary.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Pass rate: {run.PassRate:0.0}%"));
        summary.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Duration:  {run.Duration.TotalSeconds:0.00} s"));

        return summary.ToString();
    }

    // Failure messages often have several lines. Each one is indented under its test.
    private static void WriteFailureMessage(StringBuilder report, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            report.AppendLine("      (no failure message was recorded)");
            return;
        }

        foreach (string line in message.Split('\n'))
        {
            report.AppendLine($"      {line.TrimEnd()}");
        }
    }
}
