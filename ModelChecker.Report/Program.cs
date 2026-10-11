using System.Xml;
using ModelChecker.Report;

// Turns a TRX test results file into a short, readable text report.
//
// Usage:  dotnet run --project ModelChecker.Report -- <input.trx> <output.txt>
//
// Exit codes (the number a program hands back so scripts and CI know how it went):
//   0 = every test passed
//   1 = at least one test failed
//   2 = the tool could not do its job (wrong arguments, file missing or unreadable)
//
// This file is kept thin on purpose: it only deals with arguments, files and exit
// codes. Reading the TRX is TrxReader's job and the wording is ReportWriter's job.

const int ExitAllPassed = 0;
const int ExitTestsFailed = 1;
const int ExitCouldNotRun = 2;

if (args.Length != 2)
{
    // Console.Error is the "something went wrong" output stream, kept separate from normal output.
    Console.Error.WriteLine("Usage: ModelChecker.Report <input.trx> <output.txt>");
    return ExitCouldNotRun;
}

string inputPath = args[0];
string outputPath = args[1];

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Could not find the test results file: {inputPath}");
    return ExitCouldNotRun;
}

TestRun run;

try
{
    run = TrxReader.Read(inputPath);
}
catch (Exception ex) when (ex is XmlException or IOException or UnauthorizedAccessException)
{
    // Only the errors we expect from a bad or locked file are caught here,
    // so we can show a clear message instead of a stack trace.
    Console.Error.WriteLine($"Could not read {inputPath}: {ex.Message}");
    return ExitCouldNotRun;
}

try
{
    File.WriteAllText(outputPath, ReportWriter.Write(run));
}
catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine($"Could not write {outputPath}: {ex.Message}");
    return ExitCouldNotRun;
}

// Print the summary too, so it shows up in the terminal and in the CI log.
Console.Write(ReportWriter.WriteSummary(run));
Console.WriteLine($"Report written to {outputPath}");

return run.Failed > 0 ? ExitTestsFailed : ExitAllPassed;
