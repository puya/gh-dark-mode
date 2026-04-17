using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace GHDarkMode;

/// <summary>
/// Modular override: pick a skin color key from a curated dropdown (favorites + manifest keys),
/// or optionally type a raw XML item name. Emits a token for GH Dark Mode <c>OVR</c> input.
/// </summary>
public class GHDarkModeOverrideComponent : GH_Component
{
    public GHDarkModeOverrideComponent()
        : base(
            name: "GH Dark Mode Override",
            nickname: "DMOverride",
            description: "Pick a UI color to tweak (dropdown) and a color; wire into GH Dark Mode’s OVR input for custom theming.",
            category: "Params",
            subCategory: "Util")
    {
    }

    public override Guid ComponentGuid => new Guid("0B67BA80-7A70-4F34-9B44-0EDE97D453E4");

    /// <summary>Gear icon (24×24 PNG + alpha) — pairs with the main component’s dark-state icon.</summary>
    protected override Bitmap Icon => ComponentIcons.Gear;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        // Use AddIntegerParameter (then cast) so Grasshopper attaches the standard integer UI, including
        // the named-value picker. A bare new Param_Integer() + AddParameter() can omit that wiring on
        // some hosts (e.g. Mac), so the canvas shows only a numeric field with no dropdown.
        int targetIdx = pManager.AddIntegerParameter(
            "Target",
            "T",
            "Color key: favorites first, then all gh_drawing_color keys from embedded manifest.",
            GH_ParamAccess.item,
            0);
        if (pManager[targetIdx] is not Param_Integer target)
            throw new InvalidOperationException("Expected Param_Integer for Target input.");

        foreach (SkinKeysCatalog.SkinKeyEntry e in SkinKeysCatalog.GetMergedEntries())
            target.AddNamedValue(e.Label, e.Index);

        pManager.AddColourParameter("Color", "C", "Override color.", GH_ParamAccess.item, Color.FromArgb(255, 38, 38, 38));
        pManager.AddTextParameter("Custom key", "K", "Optional: raw XML item name (overrides Target when non-empty).", GH_ParamAccess.item);
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Override", "OVR", "Override token for GH Dark Mode OVR input.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        int targetIndex = 0;
        Color color = Color.FromArgb(255, 38, 38, 38);

        if (!da.GetData(0, ref targetIndex))
            return;
        if (!da.GetData(1, ref color))
            return;

        string customKey = string.Empty;
        da.GetData(2, ref customKey);

        string key = !string.IsNullOrWhiteSpace(customKey)
            ? customKey.Trim()
            : SkinKeysCatalog.GetXmlKey(targetIndex);

        if (string.IsNullOrWhiteSpace(key))
        {
            Message = "?";
            da.SetData(0, string.Empty);
            return;
        }

        string token = $"{key}|{color.A}|{color.R}|{color.G}|{color.B}";
        Message = key;
        da.SetData(0, token);
    }
}
