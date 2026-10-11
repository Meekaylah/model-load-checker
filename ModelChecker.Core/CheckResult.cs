namespace ModelChecker.Core;

// Everything the UI needs after checking a file:
// what the parser found (Model) and the answer for each device (Devices).
public record CheckResult(ParseResult Model, IReadOnlyList<DeviceVerdict> Devices);
