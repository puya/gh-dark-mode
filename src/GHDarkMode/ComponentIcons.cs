using System.Drawing;
using System.Reflection;

namespace GHDarkMode;

/// <summary>
/// Embedded 24×24 PNG icons with alpha (see repo <c>icons/</c>).
/// </summary>
internal static class ComponentIcons
{
    private const string MainResource = "GHDarkMode.icon-main.png";
    private const string GearResource = "GHDarkMode.icon-gear.png";

    private static Bitmap? _main;
    private static Bitmap? _gear;

    /// <summary>Main capsule — light / idle on the primary component.</summary>
    public static Bitmap Main => _main ??= Load(MainResource);

    /// <summary>Gear variant — dark state on the primary component and the override component.</summary>
    public static Bitmap Gear => _gear ??= Load(GearResource);

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
