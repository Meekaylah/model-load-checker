namespace ModelChecker.Core;

// The three possible answers to "will this model load on this device?".
// An enum is a fixed list of named choices, like a TypeScript union
// type: "Ready" | "TooHeavy" | "Broken".
public enum Verdict
{
    // The model is fine and fits inside the device's triangle budget.
    Ready,

    // The model is fine but has more triangles than the device can comfortably draw.
    TooHeavy,

    // The file has problems (or nothing to draw), so it won't load properly anywhere.
    Broken
}
