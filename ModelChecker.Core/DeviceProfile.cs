namespace ModelChecker.Core;

// Describes one kind of device: its name and how many triangles it can comfortably draw.
public record DeviceProfile(string Name, int TriangleBudget)
{
    // These budgets are rough assumptions, not measurements. Real limits depend on the
    // exact device, textures and the app showing the model. They live here, in one
    // place, so they are easy to find and change.
    public static readonly DeviceProfile Phone = new DeviceProfile("Phone", 100_000);
    public static readonly DeviceProfile Tablet = new DeviceProfile("Tablet", 300_000);
    public static readonly DeviceProfile Desktop = new DeviceProfile("Desktop", 1_000_000);

    // Every device we check, in the order the UI shows them.
    public static readonly IReadOnlyList<DeviceProfile> All = new List<DeviceProfile> { Phone, Tablet, Desktop };
}
