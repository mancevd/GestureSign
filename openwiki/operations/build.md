---
type: operations
title: Build and configurations
description: setup.ps1 local .NET Framework refs, MSBuild solution configurations, Portable/uiAccess/Centennial defines, extra-plugin net46.
tags: [build, msbuild, portable, nuget]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-1601dc4304e3854313f15d32
    resource: repo://Directory.Build.props
  - id: openwiki-source-aea8d3a263abd3c5474cb8fb
    resource: repo://Directory.Build.targets
  - id: openwiki-source-5d87cc76d8f3c6f48a7aba2b
    resource: repo://GestureSign.Common/GestureSign.Common.csproj
  - id: openwiki-source-54bb8d6c6d7609aa58789158
    resource: repo://GestureSign.Daemon/GestureSign.Daemon.csproj
  - id: openwiki-source-cc14990ba0b01e12cc81edc8
    resource: repo://GestureSign.Daemon/Properties/app.uiAccessRelease.manifest
  - id: openwiki-source-6d1362e22e610442742bfb51
    resource: repo://GestureSign.sln
  - id: openwiki-source-b3fc614a0aeef09a063adea5
    resource: repo://setup.ps1
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Build and configurations

This is a Visual Studio / MSBuild .NET Framework solution (`GestureSign.sln`). There are **no test projects**. `setup.ps1` is the documented restore path.

## Reference assemblies

`Directory.Build.props` sets `TargetFrameworkRootPath` to `.ref-assemblies\` and `BypassFrameworkInstallChecks` so MSBuild can compile without a machine-wide targeting pack. `setup.ps1` restores the solution with nuget.exe, installs `Microsoft.NETFramework.ReferenceAssemblies` for net45–net461 plus Windows SDK contracts, then junction-links those packs under `.ref-assemblies\.NETFramework\v4.x`.

`Directory.Build.targets` then:

- Forces extra-plugin projects to **v4.6**.
- For CorePlugins and Control Panel, replaces the `Windows` WinMD reference with `packages\Microsoft.Windows.SDK.Contracts...` so Store/WinRT APIs resolve from NuGet.

After setup, the script prints:

`msbuild GestureSign.sln /p:Configuration=Debug /p:Platform="Any CPU"`

## Solution configurations

| Configuration | Notable defines / behavior |
| --- | --- |
| Debug / Release | Standard; daemon `OutputPath` `..\bin\Debug` or `Release`; extra plugins copy into `Plugins`. |
| Portable | **Common** defines `Portable` so `AppConfig` uses `.\AppData` next to the exe. Daemon/Control Panel Portable configs only change output path. |
| uiAccessRelease | **Common** defines `uiAccess` (`AppConfig.UiAccess` on Windows 8+). Daemon pre-build copies `app.uiAccessRelease.manifest` (`uiAccess="true"`) over `app.manifest`; other configs copy `app.common.manifest` (`uiAccess="false"`). |
| Centennial | Daemon and Control Panel (and Common) define `ConvertedDesktopApp` for Desktop Bridge / Store converted desktop. |

Outputs land under `bin\{Configuration}\` for the two exes. Extra plugins’ Debug/Release `OutputPath` is `..\..\bin\{config}\` with a post-build copy into `Plugins`.

See [Quickstart](../quickstart.md) and [Privileges and uiAccess](privileges.md).
