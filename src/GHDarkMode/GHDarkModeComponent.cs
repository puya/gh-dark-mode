/*
 * GH Dark Mode — Toggle Grasshopper GUI between Dark Mode and Light Mode.
 * Tab: Params → Util.
 * Inputs:
 *   - M (bool): true = Dark, false = Light.
 *   - OVR (text list): optional per-key XML colors from GH Dark Mode Override.
 *   - R (bool): true = reset to factory GUI (restore baseline or delete grasshopper_gui.xml).
 *   - I (bool): optional invert-all-colors debug on grasshopper_gui.xml.
 * Output:
 *   - Out (text): status, patch counts, merged override listing after dark apply.
 * Uses GH_Skin API and grasshopper_gui.xml; settings persist across sessions.
 */

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
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

    /// <summary>
    /// Drives which 48×48 icon is shown: plain moon (light / idle) vs moon+gears (last solve applied dark).
    /// </summary>
    private bool _lastModeWasDark;

    public GHDarkModeComponent()
        : base(
            name: "GH Dark Mode",
            nickname: "DarkMode",
            description:
                "Dark mode for Grasshopper: M true = dark theme, M false = restore your saved light look. Optional color overrides via OVR. Route Out to a Panel for status.",
            category: "Params",
            subCategory: "Util")
    {
    }

    public override Guid ComponentGuid => new Guid("B1C2D3E4-F5A6-4780-BCDE-F12345678901");

    protected override Bitmap Icon => _lastModeWasDark ? ComponentIcons.Gear : ComponentIcons.Main;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        // Grasshopper: first argument = full name, second = nickname (canvas abbreviation).
        pManager.AddBooleanParameter("Mode", "M", "True = Dark Mode, False = Light Mode. Use a Button or toggle to apply.", GH_ParamAccess.item, false);
        pManager.AddTextParameter("Overrides", "OVR", "Optional: list of override tokens from GH Dark Mode Override (same-key tokens replace built-in dark defaults).", GH_ParamAccess.list);
        pManager.AddBooleanParameter("Reset", "R", "True = reset Grasshopper GUI to factory defaults (deletes grasshopper_gui.xml). Restart Grasshopper after running.", GH_ParamAccess.item, false);
        pManager.AddBooleanParameter("Invert", "I", "Debug action: invert all colors in grasshopper_gui.xml (including background).", GH_ParamAccess.item, false);
        // Debug color transform was removed from the UI for now; re-add a BooleanParameter here to restore it.

        pManager[1].Optional = true;
        pManager[3].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Out", "Out", "Status and, after dark apply, the full merged override list (route to Panel).", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        bool run = false;
        if (!da.GetData(0, ref run))
            return;

        bool reset = false;
        da.GetData(2, ref reset);

        bool invert = false;
        da.GetData(3, ref invert);

        if (reset)
        {
            _lastModeWasDark = false;
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

            // Debug transforms have priority over mode toggle.
            if (invert)
            {
                _lastModeWasDark = false;
                int changed = TransformAllXmlColorsInvert();
                Message = "Invert";
                da.SetData(0, $"Invert applied to {changed} XML color entries. Restart Grasshopper or reopen the definition to see changes.");
                return;
            }

            List<XmlColorOverride> modularOverrides = ReadModularOverrides(da);

            if (run)
            {
                ApplyDarkThemeBasedOnBaseline();
                Message = "Dark";
                GH_Skin.SaveSkin();

                List<XmlColorOverride> merged = MergeBuiltinAndModular(BuiltInDarkModeXmlColorOverrides, modularOverrides);
                merged.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                (int applied, List<string> missing) = ApplyXmlColorOverrides(merged);
                _lastModeWasDark = true;
                da.SetData(0, BuildDarkApplyMessage("Dark Mode applied.", applied, merged, missing));
            }
            else
            {
                RestoreBaselineSkinOrFallbackLight();
                Message = "Light";
                _lastModeWasDark = false;
                da.SetData(0, BuildLightRevertMessage());
                GH_Skin.SaveSkin();
            }
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
    /// Apply dark theme (VS/Adobe-style), but keep certain non-color values from the baseline
    /// (grid spacing, shadow sizing, monochrome flag). Fine-grained XML color keys are enforced after
    /// <see cref="GH_Skin.SaveSkin"/> via <see cref="BuiltInDarkModeXmlColorOverrides"/> plus optional modular tokens.
    /// </summary>
    private static void ApplyDarkThemeBasedOnBaseline()
    {
        BaselineCanvasSettings baseline = ReadBaselineCanvasSettingsOrFallback();

        // Keep the canvas/background close to GH's dark vibe, but bump contrast slightly for better component readability.
        Color darkBg = Color.FromArgb(255, 38, 38, 38);
        Color darkBg2 = Color.FromArgb(255, 42, 42, 46);     // slightly lighter than before for a touch more separation
        // Keep gridlines subtle. Baseline grid color uses alpha; preserve that alpha by using a low-A grid tone.
        Color darkGrid = Color.FromArgb(30, 255, 255, 255);
        // Lighter outline so component borders separate better on dark canvas.
        Color darkEdge = Color.FromArgb(255, 224, 224, 224);
        Color lightWire = Color.FromArgb(255, 145, 145, 145);
        Color lightText = Color.FromArgb(255, 235, 235, 235);
        Color dimText = Color.FromArgb(255, 240, 240, 240);
        Color selectedInGreen = Color.FromArgb(255, 130, 215, 50);   // baseline before XML patch; wire keys patched after SaveSkin
        Color selectedOutPurple = Color.FromArgb(255, 170, 120, 235);
        Color errorBg = Color.FromArgb(255, 105, 50, 50);
        Color warnBg = Color.FromArgb(255, 199, 86, 0);

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
        // Requested selected-wire behavior: green entering selected component, purple leaving it.
        GH_Skin.wire_selected_a = selectedInGreen;
        GH_Skin.wire_selected_b = selectedOutPurple;

        // Panel / group
        // Preserve the baseline panel background so classic yellow panels remain unchanged.
        GH_Skin.panel_back = baseline.PanelBackColor;
        GH_Skin.group_back = darkBg2;

        // Palettes: GH_PaletteStyle(Fill, Edge, Text)
        // Selected palette here is a baseline; XML patches (e.g. normal.sel.fill) refine after SaveSkin.
        Color selEdgeGreen = Color.FromArgb(255, 40, 95, 40);
        Color selFillGreen = Color.FromArgb(255, 108, 168, 19);
        Color selTextGreen = Color.FromArgb(255, 15, 35, 10);

        Color normalFill = darkBg2;
        GH_Skin.palette_normal_standard = new GH_PaletteStyle(normalFill, darkEdge, lightText);
        GH_Skin.palette_normal_selected = new GH_PaletteStyle(selFillGreen, selEdgeGreen, selTextGreen);

        // Give colored palettes a bit more saturation/contrast so they read better on a dark canvas.
        GH_Skin.palette_black_standard = new GH_PaletteStyle(Color.FromArgb(255, 60, 60, 66), darkEdge, lightText);
        GH_Skin.palette_black_selected = new GH_PaletteStyle(selFillGreen, selEdgeGreen, selTextGreen);

        GH_Skin.palette_blue_standard = new GH_PaletteStyle(Color.FromArgb(255, 75, 110, 180), darkEdge, Color.FromArgb(255, 245, 248, 255));
        GH_Skin.palette_blue_selected = new GH_PaletteStyle(selFillGreen, selEdgeGreen, selTextGreen);

        GH_Skin.palette_brown_standard = new GH_PaletteStyle(Color.FromArgb(255, 145, 98, 62), darkEdge, Color.FromArgb(255, 255, 245, 230));
        GH_Skin.palette_brown_selected = new GH_PaletteStyle(selFillGreen, selEdgeGreen, selTextGreen);
        GH_Skin.palette_error_standard = new GH_PaletteStyle(errorBg, darkEdge, lightText);
        GH_Skin.palette_error_selected = new GH_PaletteStyle(selFillGreen, selEdgeGreen, selTextGreen);

        GH_Skin.palette_grey_standard = new GH_PaletteStyle(Color.FromArgb(255, 74, 74, 82), darkEdge, lightText);
        GH_Skin.palette_grey_selected = new GH_PaletteStyle(selFillGreen, selEdgeGreen, selTextGreen);

        GH_Skin.palette_hidden_standard = new GH_PaletteStyle(Color.FromArgb(255, 64, 64, 72), darkEdge, dimText);
        GH_Skin.palette_hidden_selected = new GH_PaletteStyle(selFillGreen, selEdgeGreen, selTextGreen);

        GH_Skin.palette_locked_standard = new GH_PaletteStyle(Color.FromArgb(255, 68, 68, 74), darkEdge, dimText);
        GH_Skin.palette_locked_selected = new GH_PaletteStyle(selFillGreen, selEdgeGreen, selTextGreen);

        GH_Skin.palette_pink_standard = new GH_PaletteStyle(Color.FromArgb(255, 188, 82, 150), darkEdge, Color.FromArgb(255, 255, 240, 252));
        GH_Skin.palette_pink_selected = new GH_PaletteStyle(selFillGreen, selEdgeGreen, selTextGreen);

        GH_Skin.palette_trans_standard = new GH_PaletteStyle(darkBg2, darkEdge, lightText);
        GH_Skin.palette_trans_selected = new GH_PaletteStyle(selFillGreen, selEdgeGreen, selTextGreen);
        GH_Skin.palette_warning_standard = new GH_PaletteStyle(warnBg, darkEdge, lightText);
        GH_Skin.palette_warning_selected = new GH_PaletteStyle(selFillGreen, selEdgeGreen, selTextGreen);

        GH_Skin.palette_white_standard = new GH_PaletteStyle(Color.FromArgb(255, 70, 70, 78), darkEdge, Color.FromArgb(255, 245, 245, 245));
        GH_Skin.palette_white_selected = new GH_PaletteStyle(selFillGreen, selEdgeGreen, selTextGreen);

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
        int ShadowSize,
        Color PanelBackColor);

    private readonly record struct XmlColorOverride(string Name, Color Value);

    /// <summary>
    /// Default dark-mode XML color keys applied after every dark apply. Modular <c>OVR</c> tokens with the same
    /// resolved key name replace these values.
    /// </summary>
    private static readonly XmlColorOverride[] BuiltInDarkModeXmlColorOverrides =
    {
        new("normal.std.fill", Color.FromArgb(255, 87, 87, 87)),
        new("hidden.sel.fill", Color.FromArgb(255, 113, 7, 184)),
        new("normal.sel.fill", Color.FromArgb(255, 118, 45, 161)),
        new("hidden.sel.text", Color.FromArgb(255, 255, 255, 255)),
        new("normal.sel.edge", Color.FromArgb(255, 255, 255, 255)),
        new("hidden.sel.edge", Color.FromArgb(255, 178, 178, 178)),
        new("normal.std.edge", Color.FromArgb(255, 214, 214, 214)),
        new("hidden.std.edge", Color.FromArgb(255, 168, 168, 168)),
        new("wire_selected_a", Color.FromArgb(255, 118, 45, 161)),
        new("warning.sel.fill", Color.FromArgb(255, 118, 45, 161)),
        new("wire_default", Color.FromArgb(255, 87, 87, 87)),
    };

    private static List<XmlColorOverride> MergeBuiltinAndModular(
        IReadOnlyList<XmlColorOverride> builtin,
        List<XmlColorOverride> modular)
    {
        // Case-insensitive keys; modular entries win on duplicates.
        Dictionary<string, XmlColorOverride> map = new(StringComparer.OrdinalIgnoreCase);
        foreach (XmlColorOverride o in builtin)
        {
            string key = ResolveOverrideName(o.Name);
            map[key] = new XmlColorOverride(key, o.Value);
        }

        foreach (XmlColorOverride o in modular)
        {
            string key = ResolveOverrideName(o.Name);
            map[key] = new XmlColorOverride(key, o.Value);
        }

        return map.Values.ToList();
    }

    private List<XmlColorOverride> ReadModularOverrides(IGH_DataAccess da)
    {
        const int modularOverridesIndex = 1;
        List<string> tokens = new();
        List<XmlColorOverride> parsed = new();

        if (Params.Input.Count <= modularOverridesIndex)
            return parsed;

        if (!da.GetDataList(modularOverridesIndex, tokens))
            return parsed;

        foreach (string token in tokens.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            if (TryParseOverrideToken(token, out XmlColorOverride ov))
                parsed.Add(ov);
            else
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Invalid override token skipped: '{token}'.");
        }

        return parsed;
    }

    private static BaselineCanvasSettings ReadBaselineCanvasSettingsOrFallback()
    {
        // Defaults match Grasshopper-ish typical values, but we prefer reading them from baseline XML.
        BaselineCanvasSettings fallback = new(
            GridColumnWidth: 150,
            GridRowHeight: 50,
            IsMonochrome: false,
            ShadowSize: 30,
            PanelBackColor: Color.FromArgb(255, 255, 250, 90));

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
            Color panelBack = TryReadItemColor(doc, "panel_backcolor") ?? fallback.PanelBackColor;

            return new BaselineCanvasSettings(
                GridColumnWidth: gridCol,
                GridRowHeight: gridRow,
                IsMonochrome: mono,
                ShadowSize: shadowSize,
                PanelBackColor: panelBack);
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

    private static Color? TryReadItemColor(XDocument doc, string itemName)
    {
        XElement? item = FindItemByName(doc, itemName);
        if (item is null)
            return null;

        XElement? argb = item.Element("ARGB");
        if (argb is null)
            return null;

        string[] parts = (argb.Value ?? string.Empty).Split(';');
        if (parts.Length != 4)
            return null;

        if (!byte.TryParse(parts[0], out byte a))
            return null;
        if (!byte.TryParse(parts[1], out byte r))
            return null;
        if (!byte.TryParse(parts[2], out byte g))
            return null;
        if (!byte.TryParse(parts[3], out byte b))
            return null;

        return Color.FromArgb(a, r, g, b);
    }

    private static string BuildLightRevertMessage()
    {
        return "Reverted to baseline (pre-dark) skin. Restart Grasshopper or reopen the definition to see changes.";
    }

    private static string BuildDarkApplyMessage(
        string prefix,
        int patchedCount,
        IReadOnlyList<XmlColorOverride> mergeOrder,
        IReadOnlyList<string> missingKeys)
    {
        StringBuilder sb = new();
        sb.Append(prefix);
        sb.Append(" Restart Grasshopper or reopen the definition to see changes.");
        sb.AppendLine();
        sb.Append($"XML color keys written: {patchedCount}");
        if (missingKeys.Count > 0)
        {
            sb.Append(" — not found in XML (skipped): ");
            sb.Append(string.Join(", ", missingKeys));
        }

        sb.AppendLine();
        sb.AppendLine("Overrides (built-in + OVR, resolved key → ARGB):");
        foreach (XmlColorOverride o in mergeOrder)
        {
            Color c = o.Value;
            sb.Append("  • ");
            sb.Append(o.Name);
            sb.Append(" → A=");
            sb.Append(c.A);
            sb.Append(" R=");
            sb.Append(c.R);
            sb.Append(" G=");
            sb.Append(c.G);
            sb.Append(" B=");
            sb.Append(c.B);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static bool TryParseOverrideToken(string token, out XmlColorOverride ov)
    {
        // Token format from GH Dark Mode Override component:
        // <name>|<a>|<r>|<g>|<b>
        ov = default;
        string[] parts = token.Split('|');
        if (parts.Length != 5)
            return false;

        string name = parts[0].Trim();
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (!byte.TryParse(parts[1], out byte a))
            return false;
        if (!byte.TryParse(parts[2], out byte r))
            return false;
        if (!byte.TryParse(parts[3], out byte g))
            return false;
        if (!byte.TryParse(parts[4], out byte b))
            return false;

        ov = new XmlColorOverride(name, Color.FromArgb(a, r, g, b));
        return true;
    }

    private static int TransformAllXmlColorsInvert()
    {
        string settingsFolder = GetSettingsFolderOrThrow();
        string guiPath = Path.Combine(settingsFolder, "grasshopper_gui.xml");
        if (!File.Exists(guiPath))
            throw new FileNotFoundException($"Could not find '{guiPath}'");

        XDocument doc = XDocument.Load(guiPath);
        XElement[] items = doc.Descendants("item").ToArray();
        int changed = 0;

        foreach (XElement item in items)
        {
            XElement? argb = item.Element("ARGB");
            if (argb is null)
                continue;

            if (!TryParseArgb(argb.Value, out Color original))
                continue;

            Color updated = Color.FromArgb(original.A, 255 - original.R, 255 - original.G, 255 - original.B);
            argb.Value = $"{updated.A};{updated.R};{updated.G};{updated.B}";
            changed++;
        }

        doc.Save(guiPath);
        GH_Skin.LoadSkin();
        GH_Skin.SaveSkin();
        return changed;
    }

    private static (int Patched, List<string> MissingKeys) ApplyXmlColorOverrides(List<XmlColorOverride> overrides)
    {
        List<string> missing = new();
        if (overrides.Count == 0)
            return (0, missing);

        string settingsFolder = GetSettingsFolderOrThrow();
        string guiPath = Path.Combine(settingsFolder, "grasshopper_gui.xml");
        if (!File.Exists(guiPath))
            throw new FileNotFoundException($"Could not find '{guiPath}'");

        XDocument doc = XDocument.Load(guiPath);
        int changed = 0;

        foreach (XmlColorOverride ov in overrides)
        {
            string resolvedName = ResolveOverrideName(ov.Name);
            XElement? item = FindItemByName(doc, resolvedName);
            if (item is null)
            {
                missing.Add(resolvedName);
                continue;
            }

            XElement? argb = item.Element("ARGB");
            if (argb is null)
            {
                missing.Add(resolvedName);
                continue;
            }

            argb.Value = $"{ov.Value.A};{ov.Value.R};{ov.Value.G};{ov.Value.B}";
            changed++;
        }

        doc.Save(guiPath);

        // Reload only. Do not SaveSkin here: SaveSkin rewrites the full skin payload and can
        // unintentionally normalize fields beyond the explicitly patched XML keys.
        GH_Skin.LoadSkin();
        return (changed, missing);
    }

    private static string ResolveOverrideName(string keyOrName)
    {
        // Support short keys as aliases, while still allowing raw XML item names.
        return keyOrName.Trim().ToUpperInvariant() switch
        {
            "BG" => "canvas_backcolor",
            "CF" => "normal.std.fill",
            "CSF" => "normal.sel.fill",
            "CE" => "normal.std.edge",
            "CNT" => "normal.std.text",
            "ST" => "hidden.std.text",
            "CST" => "normal.sel.text",
            "OF" => "warning.std.fill",
            "OSF" => "warning.sel.fill",
            "WD" => "wire_default",
            "WSI" => "wire_selected_a",
            "WSO" => "wire_selected_b",
            _ => keyOrName.Trim()
        };
    }

    private static bool TryParseArgb(string raw, out Color color)
    {
        color = Color.Empty;
        string[] parts = (raw ?? string.Empty).Split(';');
        if (parts.Length != 4)
            return false;

        if (!byte.TryParse(parts[0], out byte a))
            return false;
        if (!byte.TryParse(parts[1], out byte r))
            return false;
        if (!byte.TryParse(parts[2], out byte g))
            return false;
        if (!byte.TryParse(parts[3], out byte b))
            return false;

        color = Color.FromArgb(a, r, g, b);
        return true;
    }
}
