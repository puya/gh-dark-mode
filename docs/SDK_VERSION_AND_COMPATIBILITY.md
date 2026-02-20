# SDK version and backward compatibility

## Why the plugin didn’t load

Grasshopper only loads a .gha if it was **built against the same or an older minor SDK version** than the one running. Your Rhino had **Local SDK = 8.15.25019.13002**, and the plugin was built with **Referenced SDK = 8.28.26041.11001** (from the NuGet package), so Grasshopper refused to load it.

## What we changed

The project now **builds against the Grasshopper (and RhinoCommon) assemblies from your installed Rhino 8 app** instead of a fixed NuGet version:

- **Path used:** `$(RhinoAppPath)/Contents/Frameworks/RhCore.framework/Versions/A/Resources/ref/net48/`
- **Default `RhinoAppPath`:** `/Applications/Rhino 8.app`
- So the built .gha matches **your** SDK (e.g. 8.15) and loads in your Rhino.

That gives you:

- **Backward compatibility:** The plugin is built against the same SDK as the Rhino you have installed, so it loads.
- **Compatibility with older installs:** If you open the project on another Mac with an older Rhino 8 (e.g. 8.12), building there uses that install’s assemblies, so the resulting .gha works on that machine.

## Custom Rhino location

If Rhino 8 is not in `/Applications/Rhino 8.app`, set the path when building:

```bash
dotnet build -c Release -p:RhinoAppPath="/path/to/Rhino 8.app"
```

Or set the `RhinoAppPath` property in the `.csproj` or in a `Directory.Build.props`.

## Fallback (NuGet)

If the Rhino app path is missing (e.g. on a build server), the project falls back to the **Grasshopper NuGet package** version **8.26.25349.19001**. That build will only load in Rhino 8.26 or newer. For your 8.15 install, the important case is building with the Rhino app references as above.

## Summary

| Build against              | Resulting .gha loads in        |
|----------------------------|---------------------------------|
| Your Rhino 8 app (default) | Your installed Rhino (e.g. 8.15) |
| NuGet 8.26 (fallback)       | Rhino 8.26+ only                |

Build and install as usual with `./scripts/build-and-install.sh`; the script uses your Rhino app’s SDK so the plugin loads in your current version and stays backward compatible with the build machine’s SDK.
