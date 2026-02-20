/*
 * GH Dark Mode — Single component: probe GH_Skin API.
 * Tab: Params → Util. Input: M (bool) to trigger probe.
 * This scaffold verifies that GH_Skin.LoadSkin(), GH_Skin.SaveSkin(), and one field (canvas_back) can be called on Mac.
 * Expected: message appears and Out shows status; canvas colors do not change until we implement dark/light theme logic.
 */

using System;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.GUI.Canvas;

namespace GHDarkMode;

/// <summary>
/// Probe component: calls GH_Skin.LoadSkin(), reads GH_Skin.canvas_back, calls GH_Skin.SaveSkin(),
/// and reports success/failure so we confirm the API is available on Mac before implementing dark/light themes.
/// </summary>
public class GHDarkModeComponent : GH_Component
{
    public GHDarkModeComponent()
        : base(
            name: "GH Dark Mode",
            nickname: "DarkMode",
            description: "Toggle Grasshopper GUI between Dark Mode and Light Mode. (Probe: verifies API; no canvas change yet. Route Out to Panel for full message.)",
            category: "Params",
            subCategory: "Util")
    {
    }

    public override Guid ComponentGuid => new Guid("B1C2D3E4-F5A6-4780-BCDE-F12345678901");

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddBooleanParameter("M", "Mode", "Set true to run (probe: test GH_Skin API).", GH_ParamAccess.item, false);
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

        if (!run)
        {
            Message = "Set M=true to run";
            da.SetData(0, "Set M = true to run.");
            return;
        }

        try
        {
            GH_Skin.LoadSkin();
            Color canvasBack = GH_Skin.canvas_back;
            GH_Skin.SaveSkin();

            // Short message for component bubble; full message for Out (e.g. to Panel).
            string msg = $"OK: LoadSkin + canvas_back={canvasBack} + SaveSkin";
            Message = "OK";
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, msg);
            da.SetData(0, msg);
        }
        catch (Exception ex)
        {
            string err = $"GH_Skin probe failed: {ex.Message}";
            Message = "Error";
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, err);
            da.SetData(0, err);
        }
    }
}
