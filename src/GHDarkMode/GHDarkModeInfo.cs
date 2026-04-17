/*
 * GH Dark Mode — Assembly info for Grasshopper 1.
 * Grasshopper discovers this via one class inheriting GH_AssemblyInfo (empty constructor required).
 */

using Grasshopper.Kernel;
using System.Drawing;
using System.Reflection;

namespace GHDarkMode;

/// <summary>
/// Assembly metadata for the GH Dark Mode .gha. Implemented once per GHA; empty constructor required.
/// Release version is defined once in repo root <c>Directory.Build.props</c> (&lt;Version&gt;); this property
/// reflects the built assembly's informational version so Yak, Grasshopper, and scripts stay aligned.
/// </summary>
public class GHDarkModeInfo : GH_AssemblyInfo
{
    public GHDarkModeInfo() { }

    public override string Name => "GH Dark Mode";
    public override string Version => PluginVersionFromAssembly(typeof(GHDarkModeInfo).Assembly);
    public override string AuthorName => "Puya Khalili";
    public override string AuthorContact => "https://github.com/puya";
    public override string Description =>
        "Dark mode for Grasshopper (Rhino 8, Mac and Windows). Great default dark theme; customize colors if you like.";
    public override Bitmap? Icon => CreateMoonIcon24();

    /// <summary>
    /// Matches the SDK informational assembly version from MSBuild; strips optional +metadata suffix.
    /// </summary>
    private static string PluginVersionFromAssembly(Assembly asm)
    {
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrEmpty(info))
        {
            var plus = info.IndexOf('+', StringComparison.Ordinal);
            return plus >= 0 ? info[..plus] : info;
        }

        return asm.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    private static Bitmap CreateMoonIcon24()
    {
        // Keep this simple: re-use the same icon idea as the component icon.
        // Grasshopper displays this in various UI places (e.g. plugin info).
        const int size = 24;
        Bitmap bmp = new(size, size);
        using Graphics g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        Rectangle outer = new(3, 3, 18, 18);
        using System.Drawing.Drawing2D.GraphicsPath path = new();
        path.AddEllipse(outer);

        Rectangle cut = new(8, 2, 18, 18);
        using System.Drawing.Drawing2D.GraphicsPath cutPath = new();
        cutPath.AddEllipse(cut);

        using Region r = new(path);
        r.Exclude(cutPath);

        using SolidBrush brush = new(Color.FromArgb(255, 240, 240, 240));
        g.FillRegion(brush, r);

        using Pen pen = new(Color.FromArgb(120, 0, 0, 0), 1);
        g.DrawPath(pen, path);

        return bmp;
    }
}
