using System.Globalization;

namespace ModelChecker.Web;

// Small helpers that turn numbers into text for the screen.
// They live in one place so the page and the device cards format things the same way.
//
// InvariantCulture is used so everyone sees "100,000" and "1.5", and so the
// tests get the same text on every computer.
public static class Format
{
    // 156334 -> "156,334"
    public static string Number(int value)
    {
        return value.ToString("N0", CultureInfo.InvariantCulture);
    }

    // 50342 -> "49.2 KB", 22084413 -> "21.1 MB"
    public static string FileSize(long bytes)
    {
        const double Kilobyte = 1024;
        const double Megabyte = 1024 * 1024;

        if (bytes < Kilobyte)
        {
            return $"{bytes} bytes";
        }

        if (bytes < Megabyte)
        {
            return (bytes / Kilobyte).ToString("0.0", CultureInfo.InvariantCulture) + " KB";
        }

        return (bytes / Megabyte).ToString("0.0", CultureInfo.InvariantCulture) + " MB";
    }

    // 0.33541234 -> "0.335". Up to 3 decimal places, without trailing zeros.
    public static string Dimension(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
