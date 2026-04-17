# SDK version and backward compatibility

This plugin targets **Rhino 8** with **Grasshopper 1** on **Windows and Mac**. The same **`.gha`** runs on both; build machines use either the **Rhino app’s ref assemblies** (typical on Mac with the default `.csproj` paths) or the **NuGet Grasshopper** package (common on Windows when the Mac-style path is missing).

## Why the plugin didn’t load

Grasshopper only loads a .gha if it was **built against the same or an older minor SDK version** than the one running. Your Rhino had **Local SDK = 8.15.25019.13002**, and the plugin was built with **Referenced SDK = 8.28.26041.11001** (from the NuGet package), so Grasshopper refused to load it.

## What we changed

The project now **builds against the Grasshopper (and RhinoCommon) assemblies from your installed Rhino 8 app** instead of a fixed NuGet version:

- **Path used:** `$(RhinoAppPath)/Contents/Frameworks/RhCore.framework/Versions/A/Resources/ref/net48/`
- **Default `RhinoAppPath`:** `/Applications/Rhino 8.app`
- So the built .gha matches **your** SDK (e.g. 8.15) and loads in your Rhino.

That gives you:

- **Backward compatibility:** The plugin is built against the same SDK as the Rhino you have installed, so it loads.
- **Compatibility with older installs:** If you open the project on another machine with an older Rhino 8 (e.g. 8.12), building there against **that** install’s assemblies yields a `.gha` matched to that SDK.

## Windows vs macOS paths

- **macOS:** Default **`RhinoAppPath`** is `/Applications/Rhino 8.app`; **`RhinoRefPath`** points at `Contents/Frameworks/RhCore.framework/.../ref/net48` (see `.csproj`).
- **Windows:** There is no `Rhino 8.app` layout. Unless you override **`RhinoRefPath`** / **`RhinoAppPath`** in MSBuild to your Windows Rhino **ref/net48** directory, **`Exists('$(RhinoRefPath)/Grasshopper.dll')`** is false and the project uses the **NuGet** fallback (Rhino **8.26+** for that binary).

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

On **macOS**, `./scripts/build-and-install.sh` uses your Rhino app’s SDK so the plugin loads in your installed Rhino version. On **Windows**, run `dotnet build` and copy **`build/GHDarkMode.dll`** to **`GHDarkMode.gha`** in Grasshopper **Libraries** (see root **README.md**).
