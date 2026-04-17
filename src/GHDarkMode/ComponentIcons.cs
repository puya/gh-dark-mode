using System.Drawing;
using System.Reflection;

namespace GHDarkMode;

/// <summary>
/// Embedded 48×48 BMP icons shipped with the plugin (see repo <c>icons/</c>).
/// </summary>
internal static class ComponentIcons
{
    private const string MoonResource = "GHDarkMode.icon-moon-48.bmp";
    private const string MoonGearsResource = "GHDarkMode.icon-moon-gears-48.bmp";

    private static Bitmap? _moon48;
    private static Bitmap? _moonGears48;

    /// <summary>Moon only — shown when the main component last applied light / idle.</summary>
    public static Bitmap Moon48 => _moon48 ??= Load(MoonResource);

    /// <summary>Moon + gears — dark mode active on main component, or the override component.</summary>
    public static Bitmap MoonGears48 => _moonGears48 ??= Load(MoonGearsResource);

    private static Bitmap Load(string logicalName)
    {
        Assembly asm = typeof(ComponentIcons).Assembly;
        using Stream? stream = asm.GetManifestResourceStream(logicalName);
        if (stream is null)
        {
            string available = string.Join(", ", asm.GetManifestResourceNames());
            throw new InvalidOperationException($"Missing embedded icon '{logicalName}'. Known: {available}");
        }

        return new Bitmap(stream);
    }
}
