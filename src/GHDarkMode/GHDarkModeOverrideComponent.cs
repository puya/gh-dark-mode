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
            description: "Pick a grasshopper_gui.xml color key (dropdown) and color; emits an override token for GH Dark Mode.",
            category: "Params",
            subCategory: "Util")
    {
    }

    public override Guid ComponentGuid => new Guid("0B67BA80-7A70-4F34-9B44-0EDE97D453E4");

    protected override Bitmap? Icon => null;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        var target = new Param_Integer
        {
            Name = "Target",
            NickName = "T",
            Description = "Color key: favorites first, then all gh_drawing_color keys from embedded manifest.",
        };

        foreach (SkinKeysCatalog.SkinKeyEntry e in SkinKeysCatalog.GetMergedEntries())
            target.AddNamedValue(e.Label, e.Index);

        target.SetPersistentData(0);
        pManager.AddParameter(target);

        pManager.AddColourParameter("Color", "C", "Override color.", GH_ParamAccess.item, Color.FromArgb(255, 38, 38, 38));
        pManager.AddBooleanParameter("Enable", "E", "If false, override token output is empty.", GH_ParamAccess.item, true);
        pManager.AddTextParameter("Custom key", "K", "Optional: raw XML item name (overrides Target when non-empty).", GH_ParamAccess.item);
        pManager[3].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Override", "OVR", "Override token for GH Dark Mode OVR input.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        int targetIndex = 0;
        Color color = Color.FromArgb(255, 38, 38, 38);
        bool enabled = true;

        if (!da.GetData(0, ref targetIndex))
            return;
        if (!da.GetData(1, ref color))
            return;
        da.GetData(2, ref enabled);

        string customKey = string.Empty;
        da.GetData(3, ref customKey);

        if (!enabled)
        {
            Message = "Off";
            da.SetData(0, string.Empty);
            return;
        }

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
