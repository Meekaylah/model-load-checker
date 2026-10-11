namespace ModelChecker.Core;

// The answer for one device, e.g. (Phone, TooHeavy).
public record DeviceVerdict(DeviceProfile Device, Verdict Verdict);
