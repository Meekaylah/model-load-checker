namespace ModelChecker.Report;

// How one test ended.
public enum TestOutcome
{
    Passed,
    Failed,
    Skipped
}

// One test from the results file.
// FailureMessage is only filled in for failed tests, so it is nullable (string?).
public record TestResult(string Name, TestOutcome Outcome, TimeSpan Duration, string? FailureMessage);
