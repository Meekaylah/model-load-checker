namespace ModelChecker.Report;

// A whole test run: every test plus how long the run took.
// The counts are worked out from the list, so they can never disagree with it.
public record TestRun(IReadOnlyList<TestResult> Tests, TimeSpan Duration)
{
    public int Total => Tests.Count;
    public int Passed => Tests.Count(t => t.Outcome == TestOutcome.Passed);
    public int Failed => Tests.Count(t => t.Outcome == TestOutcome.Failed);
    public int Skipped => Tests.Count(t => t.Outcome == TestOutcome.Skipped);

    // Percentage of tests that passed, e.g. 97.1.
    // The check for zero avoids dividing by zero when a run has no tests.
    public double PassRate => Total == 0 ? 0 : Passed * 100.0 / Total;
}
