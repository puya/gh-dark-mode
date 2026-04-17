using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace GHDarkMode;

/// <summary>
/// Modular override component that emits one override token for GH Dark Mode.
/// Connect multiple instances to the main component's OVR input.
/// </summary>
public class GHDarkModeOverrideComponent : GH_Component
{
    public GHDarkModeOverrideComponent()
        : base(
            name: "GH Dark Mode Override",
            nickname: "DMOverride",
            description: "Emit a color override token for GH Dark Mode. Key can be a short alias (BG, WD, etc.) or a raw grasshopper_gui.xml item name.",
            category: "Params",
            subCategory: "Util")
    {
    }

    public override Guid ComponentGuid => new Guid("0B67BA80-7A70-4F34-9B44-0EDE97D453E4");

    protected override Bitmap? Icon => null;

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("Key", "K", "Override key/target. Examples: BG, CF, CE, CNT, WD or raw XML key like normal.std.text.", GH_ParamAccess.item, "BG");
        pManager.AddColourParameter("Color", "C", "Override color.", GH_ParamAccess.item, Color.FromArgb(255, 38, 38, 38));
        pManager.AddBooleanParameter("Enable", "E", "If false, override token output is empty.", GH_ParamAccess.item, true);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Override", "OVR", "Override token for GH Dark Mode OVR input.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string key = "BG";
        Color color = Color.FromArgb(255, 38, 38, 38);
        bool enabled = true;

        if (!da.GetData(0, ref key))
            return;
        if (!da.GetData(1, ref color))
            return;
        da.GetData(2, ref enabled);

        if (!enabled || string.IsNullOrWhiteSpace(key))
        {
            Message = "Off";
            da.SetData(0, string.Empty);
            return;
        }

        string token = $"{key.Trim()}|{color.A}|{color.R}|{color.G}|{color.B}";
        Message = key.Trim();
        da.SetData(0, token);
    }
}

