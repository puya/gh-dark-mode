/*
 * GH Dark Mode — Toggle Grasshopper GUI between Dark Mode and Light Mode.
 * Tab: Params → Util.
 * Inputs:
 *   - M (bool): true = Dark, false = Light.
 *   - R (bool): true = reset to factory GUI (delete grasshopper_gui.xml; restart Grasshopper).
 * Output:
 *   - Out (text): status message.
 * Uses GH_Skin API and grasshopper_gui.xml; settings persist across sessions.
 */

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.GUI.Canvas;

namespace GHDarkMode;

/// <summary>
/// Toggles Grasshopper GUI between Dark Mode and Light Mode via GH_Skin and SaveSkin.
/// </summary>
public class GHDarkModeComponent : GH_Component
{
    /// <summary>
    /// Baseline skin backup file name.
    /// Stored in the Grasshopper settings folder alongside grasshopper_gui.xml.
    /// </summary>
    private const string BaselineSkinFileName = "ghdarkmode_baseline_gui.xml";

    public GHDarkModeComponent()
        : base(
            name: "GH Dark Mode",
            nickname: "DarkMode",
            description: "Toggle Grasshopper GUI between Dark Mode and Light Mode. M = true: Dark; M = false: Light. Route Out to Panel for status.",
            category: "Params",
            subCategory: "Util")
    {
    }

    public override Guid ComponentGuid => new Guid("B1C2D3E4-F5A6-4780-BCDE-F12345678901");

    protected override Bitmap Icon => IconFactory.Moon24;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddBooleanParameter("M", "Mode", "True = Dark Mode, False = Light Mode. Use a Button or toggle to apply.", GH_ParamAccess.item, false);
        pManager.AddBooleanParameter("R", "Reset", "True = reset Grasshopper GUI to factory defaults (deletes grasshopper_gui.xml). Restart Grasshopper after running.", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Out", "Out", "Status message (route to Panel to see full text).", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        bool run = false;
        if (!da.GetData(0, ref run))
            return;

        bool reset = false;
        // Second input is optional in older definitions; ignore if not present.
        if (Params.Input.Count > 1)
            da.GetData(1, ref reset);

        if (reset)
        {
            try
            {
                string info = ResetOrRestoreBaseline();
                Message = "Reset";
                da.SetData(0, $"{info} Restart Grasshopper to ensure all UI elements refresh.");
            }
            catch (Exception ex)
            {
                string errReset = $"Reset failed: {ex.Message}";
                Message = "Error";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, errReset);
                da.SetData(0, errReset);
            }

            return;
        }

        try
        {
            // Ensure we have a baseline skin snapshot before we modify anything.
            // This lets us restore the user's current/factory setup later (light = revert).
            EnsureBaselineSkinSnapshotExists();

            if (run)
            {
                ApplyDarkThemeBasedOnBaseline();
                Message = "Dark";
                da.SetData(0, "Dark Mode applied. Restart Grasshopper or reopen the definition to see changes.");
            }
            else
            {
                RestoreBaselineSkinOrFallbackLight();
                Message = "Light";
                da.SetData(0, "Reverted to baseline (pre-dark) skin. Restart Grasshopper or reopen the definition to see changes.");
            }

            GH_Skin.SaveSkin();
        }
        catch (Exception ex)
        {
            string err = $"Failed: {ex.Message}";
            Message = "Error";
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, err);
            da.SetData(0, err);
        }
    }

    /// <summary>
    /// Ensure a baseline skin snapshot exists.
    /// Baseline is "whatever the user had when this component first ran" (could be factory defaults or a custom theme).
    /// </summary>
    private static void EnsureBaselineSkinSnapshotExists()
    {
        string settingsFolder = GetSettingsFolderOrThrow();
        string baselinePath = Path.Combine(settingsFolder, BaselineSkinFileName);
        if (File.Exists(baselinePath))
            return;

        string guiPath = Path.Combine(settingsFolder, "grasshopper_gui.xml");

        // Make sure GH_Skin has a current state; if grasshopper_gui.xml is missing,
        // SaveSkin will create it from built-in defaults.
        GH_Skin.LoadSkin();
        if (!File.Exists(guiPath))
            GH_Skin.SaveSkin();

        if (!File.Exists(guiPath))
            throw new IOException($"Expected '{guiPath}' to exist after LoadSkin/SaveSkin, but it does not.");

        File.Copy(guiPath, baselinePath);
    }

    /// <summary>
    /// Reset behavior:
    /// - If a baseline snapshot exists, restore it (preferred).
    /// - Otherwise reset to factory defaults by deleting grasshopper_gui.xml.
    /// </summary>
    private static string ResetOrRestoreBaseline()
    {
        string settingsFolder = GetSettingsFolderOrThrow();
        string guiPath = Path.Combine(settingsFolder, "grasshopper_gui.xml");
        string baselinePath = Path.Combine(settingsFolder, BaselineSkinFileName);

        if (File.Exists(baselinePath))
        {
            File.Copy(baselinePath, guiPath, overwrite: true);
            GH_Skin.LoadSkin();
            GH_Skin.SaveSkin();
            return $"Restored baseline skin from '{baselinePath}'.";
        }

        bool existed = File.Exists(guiPath);
        if (existed)
            File.Delete(guiPath);

        GH_Skin.LoadSkin();
        GH_Skin.SaveSkin();

        if (existed)
            return $"Factory reset: deleted and recreated grasshopper_gui.xml at '{guiPath}'.";

        return $"Factory reset: grasshopper_gui.xml did not exist; created fresh defaults at '{guiPath}'.";
    }

    private static string GetSettingsFolderOrThrow()
    {
        string settingsFolder = Folders.SettingsFolder;
        if (string.IsNullOrWhiteSpace(settingsFolder))
            throw new InvalidOperationException("Grasshopper settings folder path is empty.");
        return settingsFolder;
    }

    /// <summary>
    /// Apply dark theme (VS/Adobe-style), but keep certain non-color values from the baseline.
    /// Currently: grid spacing + canvas shadow sizing + monochrome flag.
    /// </summary>
    private static void ApplyDarkThemeBasedOnBaseline()
    {
        BaselineCanvasSettings baseline = ReadBaselineCanvasSettingsOrFallback();

        // Keep the canvas/background close to GH's dark vibe, but bump contrast slightly for better component readability.
        Color darkBg = Color.FromArgb(255, 45, 45, 48);      // #2D2D30
        Color darkBg2 = Color.FromArgb(255, 42, 42, 46);     // slightly lighter than before for a touch more separation
        // Keep gridlines subtle. Baseline grid color uses alpha; preserve that alpha by using a low-A grid tone.
        Color darkGrid = Color.FromArgb(30, 255, 255, 255);
        Color darkEdge = Color.FromArgb(255, 62, 62, 66);
        Color lightWire = Color.FromArgb(255, 200, 200, 205);
        Color lightText = Color.FromArgb(255, 235, 235, 235);
        Color dimText = Color.FromArgb(255, 175, 175, 175);
        Color accent = Color.FromArgb(255, 55, 155, 255);    // brighter, slightly more saturated blue
        Color errorBg = Color.FromArgb(255, 105, 50, 50);
        Color warnBg = Color.FromArgb(255, 110, 90, 45);

        // Canvas
        GH_Skin.canvas_back = darkBg;
        GH_Skin.canvas_edge = darkEdge;
        GH_Skin.canvas_grid = darkGrid;
        GH_Skin.canvas_grid_col = baseline.GridColumnWidth;
        GH_Skin.canvas_grid_row = baseline.GridRowHeight;

        // Important: canvas_monochromatic can suppress grid/document styling on some versions.
        // Preserve the baseline flag and only adjust the actual colors.
        GH_Skin.canvas_mono = baseline.IsMonochrome;
        GH_Skin.canvas_mono_color = darkBg;

        // Preserve baseline shadow sizing so document edges/shading behave the same.
        GH_Skin.canvas_shade = Color.FromArgb(80, 0, 0, 0);
        GH_Skin.canvas_shade_size = baseline.ShadowSize;

        // Wires
        GH_Skin.wire_default = lightWire;
        GH_Skin.wire_empty = dimText;
        GH_Skin.wire_selected_a = accent;
        GH_Skin.wire_selected_b = Color.FromArgb(255, 230, 230, 235);

        // Panel / group
        GH_Skin.panel_back = darkBg2;
        GH_Skin.group_back = darkBg2;

        // Palettes: GH_PaletteStyle(Fill, Edge, Text)
        Color selFill = Color.FromArgb(255, 70, 70, 78);
        GH_Skin.palette_normal_standard = new GH_PaletteStyle(darkBg2, darkEdge, lightText);
        GH_Skin.palette_normal_selected = new GH_PaletteStyle(selFill, darkEdge, lightText);

        // Give colored palettes a bit more saturation/contrast so they read better on a dark canvas.
        GH_Skin.palette_black_standard = new GH_PaletteStyle(Color.FromArgb(255, 52, 52, 56), darkEdge, lightText);
        GH_Skin.palette_black_selected = new GH_PaletteStyle(Color.FromArgb(255, 78, 78, 84), darkEdge, lightText);

        GH_Skin.palette_blue_standard = new GH_PaletteStyle(Color.FromArgb(255, 55, 75, 120), darkEdge, lightText);
        GH_Skin.palette_blue_selected = new GH_PaletteStyle(Color.FromArgb(255, 70, 95, 150), darkEdge, lightText);

        GH_Skin.palette_brown_standard = new GH_PaletteStyle(Color.FromArgb(255, 105, 80, 55), darkEdge, lightText);
        GH_Skin.palette_brown_selected = new GH_PaletteStyle(Color.FromArgb(255, 130, 100, 70), darkEdge, lightText);
        GH_Skin.palette_error_standard = new GH_PaletteStyle(errorBg, darkEdge, lightText);
        GH_Skin.palette_error_selected = new GH_PaletteStyle(Color.FromArgb(255, 135, 60, 60), darkEdge, lightText);

        GH_Skin.palette_grey_standard = new GH_PaletteStyle(Color.FromArgb(255, 62, 62, 68), darkEdge, lightText);
        GH_Skin.palette_grey_selected = new GH_PaletteStyle(Color.FromArgb(255, 86, 86, 94), darkEdge, lightText);

        GH_Skin.palette_hidden_standard = new GH_PaletteStyle(Color.FromArgb(255, 52, 52, 56), darkEdge, dimText);
        GH_Skin.palette_hidden_selected = new GH_PaletteStyle(Color.FromArgb(255, 72, 72, 78), darkEdge, lightText);

        GH_Skin.palette_locked_standard = new GH_PaletteStyle(Color.FromArgb(255, 56, 56, 60), darkEdge, dimText);
        GH_Skin.palette_locked_selected = new GH_PaletteStyle(Color.FromArgb(255, 76, 76, 82), darkEdge, lightText);

        GH_Skin.palette_pink_standard = new GH_PaletteStyle(Color.FromArgb(255, 120, 70, 105), darkEdge, lightText);
        GH_Skin.palette_pink_selected = new GH_PaletteStyle(Color.FromArgb(255, 150, 85, 125), darkEdge, lightText);

        GH_Skin.palette_trans_standard = new GH_PaletteStyle(darkBg2, darkEdge, lightText);
        GH_Skin.palette_trans_selected = new GH_PaletteStyle(selFill, darkEdge, lightText);
        GH_Skin.palette_warning_standard = new GH_PaletteStyle(warnBg, darkEdge, lightText);
        GH_Skin.palette_warning_selected = new GH_PaletteStyle(Color.FromArgb(255, 140, 110, 55), darkEdge, lightText);

        GH_Skin.palette_white_standard = new GH_PaletteStyle(Color.FromArgb(255, 70, 70, 78), darkEdge, Color.FromArgb(255, 245, 245, 245));
        GH_Skin.palette_white_selected = new GH_PaletteStyle(Color.FromArgb(255, 95, 95, 105), darkEdge, Color.FromArgb(255, 250, 250, 250));

        // ZUI
        GH_Skin.zui_edge = darkEdge;
        GH_Skin.zui_edge_highlight = lightWire;
        GH_Skin.zui_fill = darkBg2;
        GH_Skin.zui_fill_highlight = Color.FromArgb(255, 55, 55, 58);
    }

    /// <summary>
    /// Restore baseline skin snapshot if present; otherwise fall back to the legacy hardcoded light theme.
    /// </summary>
    private static void RestoreBaselineSkinOrFallbackLight()
    {
        string settingsFolder = GetSettingsFolderOrThrow();
        string baselinePath = Path.Combine(settingsFolder, BaselineSkinFileName);
        string guiPath = Path.Combine(settingsFolder, "grasshopper_gui.xml");

        if (File.Exists(baselinePath))
        {
            File.Copy(baselinePath, guiPath, overwrite: true);
            GH_Skin.LoadSkin();
            return;
        }

        ApplyLegacyHardcodedLightTheme();
    }

    /// <summary>
    /// Legacy light theme (Grasshopper-style defaults).
    /// Kept as a fallback for when no baseline snapshot exists.
    /// </summary>
    private static void ApplyLegacyHardcodedLightTheme()
    {
        // Default canvas: RGB 212, 208, 199 (typical grasshopper_gui.xml)
        Color lightBg = Color.FromArgb(255, 212, 208, 199);
        Color lightGrid = Color.FromArgb(255, 190, 186, 178);
        Color darkEdge = Color.FromArgb(255, 140, 136, 128);
        Color darkWire = Color.FromArgb(255, 80, 80, 80);
        Color compBg = Color.FromArgb(255, 250, 250, 248);
        Color compSel = Color.FromArgb(255, 230, 230, 225);

        // Canvas
        GH_Skin.canvas_back = lightBg;
        GH_Skin.canvas_edge = darkEdge;
        GH_Skin.canvas_grid = lightGrid;
        GH_Skin.canvas_grid_col = 50;
        GH_Skin.canvas_grid_row = 50;
        GH_Skin.canvas_mono = false;
        GH_Skin.canvas_mono_color = lightBg;
        GH_Skin.canvas_shade = Color.FromArgb(255, 180, 176, 168);
        GH_Skin.canvas_shade_size = 4;

        // Wires
        GH_Skin.wire_default = darkWire;
        GH_Skin.wire_empty = Color.FromArgb(255, 200, 196, 188);
        GH_Skin.wire_selected_a = Color.FromArgb(255, 0, 122, 204);
        GH_Skin.wire_selected_b = darkWire;

        // Panel / group
        GH_Skin.panel_back = compBg;
        GH_Skin.group_back = Color.FromArgb(255, 240, 238, 235);

        // Palettes: GH_PaletteStyle(Fill, Edge, Text)
        Color darkText = Color.FromArgb(255, 50, 50, 50);
        GH_Skin.palette_normal_standard = new GH_PaletteStyle(compBg, darkEdge, darkText);
        GH_Skin.palette_normal_selected = new GH_PaletteStyle(compSel, darkEdge, darkText);
        GH_Skin.palette_black_standard = new GH_PaletteStyle(Color.FromArgb(255, 50, 50, 50), darkEdge, Color.White);
        GH_Skin.palette_black_selected = new GH_PaletteStyle(Color.FromArgb(255, 70, 70, 70), darkEdge, Color.White);
        GH_Skin.palette_blue_standard = new GH_PaletteStyle(Color.FromArgb(255, 200, 220, 255), darkEdge, darkText);
        GH_Skin.palette_blue_selected = new GH_PaletteStyle(Color.FromArgb(255, 180, 205, 255), darkEdge, darkText);
        GH_Skin.palette_brown_standard = new GH_PaletteStyle(Color.FromArgb(255, 220, 210, 195), darkEdge, darkText);
        GH_Skin.palette_brown_selected = new GH_PaletteStyle(Color.FromArgb(255, 205, 192, 175), darkEdge, darkText);
        GH_Skin.palette_error_standard = new GH_PaletteStyle(Color.FromArgb(255, 255, 220, 220), darkEdge, Color.FromArgb(255, 120, 40, 40));
        GH_Skin.palette_error_selected = new GH_PaletteStyle(Color.FromArgb(255, 255, 200, 200), darkEdge, Color.FromArgb(255, 140, 50, 50));
        GH_Skin.palette_grey_standard = new GH_PaletteStyle(Color.FromArgb(255, 220, 218, 214), darkEdge, darkText);
        GH_Skin.palette_grey_selected = new GH_PaletteStyle(Color.FromArgb(255, 205, 202, 196), darkEdge, darkText);
        GH_Skin.palette_hidden_standard = new GH_PaletteStyle(Color.FromArgb(255, 235, 233, 228), darkEdge, darkText);
        GH_Skin.palette_hidden_selected = new GH_PaletteStyle(Color.FromArgb(255, 220, 217, 212), darkEdge, darkText);
        GH_Skin.palette_locked_standard = new GH_PaletteStyle(Color.FromArgb(255, 230, 228, 224), darkEdge, darkText);
        GH_Skin.palette_locked_selected = new GH_PaletteStyle(Color.FromArgb(255, 215, 212, 207), darkEdge, darkText);
        GH_Skin.palette_pink_standard = new GH_PaletteStyle(Color.FromArgb(255, 255, 225, 235), darkEdge, darkText);
        GH_Skin.palette_pink_selected = new GH_PaletteStyle(Color.FromArgb(255, 255, 210, 225), darkEdge, darkText);
        GH_Skin.palette_trans_standard = new GH_PaletteStyle(Color.FromArgb(255, 252, 252, 250), darkEdge, darkText);
        GH_Skin.palette_trans_selected = new GH_PaletteStyle(Color.FromArgb(255, 240, 238, 235), darkEdge, darkText);
        GH_Skin.palette_warning_standard = new GH_PaletteStyle(Color.FromArgb(255, 255, 245, 220), darkEdge, Color.FromArgb(255, 120, 90, 40));
        GH_Skin.palette_warning_selected = new GH_PaletteStyle(Color.FromArgb(255, 255, 235, 200), darkEdge, Color.FromArgb(255, 130, 100, 50));
        GH_Skin.palette_white_standard = new GH_PaletteStyle(Color.White, darkEdge, darkText);
        GH_Skin.palette_white_selected = new GH_PaletteStyle(Color.FromArgb(255, 245, 244, 242), darkEdge, darkText);

        // ZUI
        GH_Skin.zui_edge = darkEdge;
        GH_Skin.zui_edge_highlight = Color.FromArgb(255, 0, 122, 204);
        GH_Skin.zui_fill = compBg;
        GH_Skin.zui_fill_highlight = compSel;
    }

    private static (int gridCol, int gridRow) ReadBaselineGridSpacingOrFallback()
    {
        BaselineCanvasSettings s = ReadBaselineCanvasSettingsOrFallback();
        return (s.GridColumnWidth, s.GridRowHeight);
    }

    private readonly record struct BaselineCanvasSettings(
        int GridColumnWidth,
        int GridRowHeight,
        bool IsMonochrome,
        int ShadowSize);

    private static BaselineCanvasSettings ReadBaselineCanvasSettingsOrFallback()
    {
        // Defaults match Grasshopper-ish typical values, but we prefer reading them from baseline XML.
        BaselineCanvasSettings fallback = new(
            GridColumnWidth: 150,
            GridRowHeight: 50,
            IsMonochrome: false,
            ShadowSize: 30);

        try
        {
            string settingsFolder = GetSettingsFolderOrThrow();
            string baselinePath = Path.Combine(settingsFolder, BaselineSkinFileName);
            if (!File.Exists(baselinePath))
                return fallback;

            XDocument doc = XDocument.Load(baselinePath);

            int gridCol = TryReadItemInt(doc, "canvas_columnwidth") ?? fallback.GridColumnWidth;
            int gridRow = TryReadItemInt(doc, "canvas_rowheight") ?? fallback.GridRowHeight;
            bool mono = TryReadItemBool(doc, "canvas_monochromatic") ?? fallback.IsMonochrome;
            int shadowSize = TryReadItemInt(doc, "canvas_shadowsize") ?? fallback.ShadowSize;

            return new BaselineCanvasSettings(
                GridColumnWidth: gridCol,
                GridRowHeight: gridRow,
                IsMonochrome: mono,
                ShadowSize: shadowSize);
        }
        catch
        {
            // Never block theme switching on a parse issue.
            return fallback;
        }
    }

    private static XElement? FindItemByName(XDocument doc, string itemName)
    {
        // grasshopper_gui.xml is an archive-like XML that stores key/value pairs under <item name="...">.
        return doc
            .Descendants("item")
            .FirstOrDefault(x => string.Equals((string?)x.Attribute("name"), itemName, StringComparison.OrdinalIgnoreCase));
    }

    private static int? TryReadItemInt(XDocument doc, string itemName)
    {
        XElement? item = FindItemByName(doc, itemName);
        if (item is null)
            return null;

        if (int.TryParse(item.Value?.Trim(), out int value))
            return value;

        return null;
    }

    private static bool? TryReadItemBool(XDocument doc, string itemName)
    {
        XElement? item = FindItemByName(doc, itemName);
        if (item is null)
            return null;

        if (bool.TryParse(item.Value?.Trim(), out bool value))
            return value;

        return null;
    }

    private static class IconFactory
    {
        // Grasshopper expects a small (typically 24×24) bitmap for component icons.
        // We generate it programmatically to avoid external assets and to keep the repo lightweight.
        public static readonly Bitmap Moon24 = CreateMoonIcon24();

        private static Bitmap CreateMoonIcon24()
        {
            const int size = 24;
            Bitmap bmp = new(size, size);

            using Graphics g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            // Lucide-style crescent: draw a filled circle, then subtract a slightly offset circle.
            Rectangle outer = new(3, 3, 18, 18);
            using GraphicsPath path = new();
            path.AddEllipse(outer);

            // "Cut out" circle
            Rectangle cut = new(8, 2, 18, 18);
            using GraphicsPath cutPath = new();
            cutPath.AddEllipse(cut);

            using Region r = new(path);
            r.Exclude(cutPath);

            using SolidBrush brush = new(Color.FromArgb(255, 240, 240, 240));
            g.FillRegion(brush, r);

            // Subtle outline to read well on both light and dark component backgrounds.
            using Pen pen = new(Color.FromArgb(120, 0, 0, 0), 1);
            g.DrawPath(pen, path);

            return bmp;
        }
    }
}
