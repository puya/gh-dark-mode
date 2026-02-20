/*
 * GH Dark Mode — Assembly info for Grasshopper 1.
 * Grasshopper discovers this via one class inheriting GH_AssemblyInfo (empty constructor required).
 */

using Grasshopper.Kernel;

namespace GHDarkMode;

/// <summary>
/// Assembly metadata for the GH Dark Mode .gha. Implemented once per GHA; empty constructor required.
/// </summary>
public class GHDarkModeInfo : GH_AssemblyInfo
{
    public GHDarkModeInfo() { }

    public override string Name => "GH Dark Mode";
    public override string Version => "1.0.0";
    public override string AuthorName => "GH Dark Mode (Mac)";
    public override string AuthorContact => "";
    public override string Description => "Toggle Grasshopper GUI between Dark Mode and Light Mode.";
    public override System.Drawing.Bitmap? Icon => null;
}
