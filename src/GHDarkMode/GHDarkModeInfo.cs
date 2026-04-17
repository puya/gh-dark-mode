/*
 * GH Dark Mode — Assembly info for Grasshopper 1.
 * Grasshopper discovers this via one class inheriting GH_AssemblyInfo (empty constructor required).
 */

using Grasshopper.Kernel;
using System.Drawing;

namespace GHDarkMode;

/// <summary>
/// Assembly metadata for the GH Dark Mode .gha. Implemented once per GHA; empty constructor required.
/// </summary>
public class GHDarkModeInfo : GH_AssemblyInfo
{
    public GHDarkModeInfo() { }

    public override string Name => "GH Dark Mode";
    // Keep in sync with packaging/manifest.yml (Yak) for each release.
    public override string Version => "1.0.2";
    public override string AuthorName => "GH Dark Mode";
    public override string AuthorContact => "";
    public override string Description =>
        "Dark mode for Grasshopper (Rhino 8, Mac and Windows). Great default dark theme; customize colors if you like.";
    public override Bitmap? Icon => CreateMoonIcon24();

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
