---
type: operations
title: Privileges and uiAccess
description: asInvoker vs uiAccess manifests, RunAsAdmin startup, AppCompat RUNASADMIN warning, and Store package redirect.
tags: [uac, uiaccess, elevation, store]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-c39da0394d06b00f690f046e
    resource: repo://GestureSign.Common/Configuration/AppConfig.cs
  - id: openwiki-source-14f15b71d176f6cb46739ce4
    resource: repo://GestureSign.ControlPanel/App.xaml.cs
  - id: openwiki-source-ed8bdb213bb042669a958efc
    resource: repo://GestureSign.ControlPanel/Common/StartupHelper.cs
  - id: openwiki-source-eb463386b4ca7b3ae25c788c
    resource: repo://GestureSign.ControlPanel/MainWindow.xaml.cs
  - id: openwiki-source-54bb8d6c6d7609aa58789158
    resource: repo://GestureSign.Daemon/GestureSign.Daemon.csproj
  - id: openwiki-source-548934ef6dc1c13ac8271fba
    resource: repo://GestureSign.Daemon/Input/PointCapture.cs
  - id: openwiki-source-cc14990ba0b01e12cc81edc8
    resource: repo://GestureSign.Daemon/Properties/app.uiAccessRelease.manifest
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Privileges and uiAccess

The repository does not document Authenticode signing or installer packaging beyond manifests and a Centennial/`ConvertedDesktopApp` configuration. Windows requires a signed binary in a secure location for `uiAccess="true"` to actually work; that install story is **not** in this source tree.

## Manifests

Daemon `PreBuildEvent` copies:

- `app.uiAccessRelease.manifest` when configuration is `uiAccessRelease` — `requestedExecutionLevel` **asInvoker** with **uiAccess="true"**.
- `app.common.manifest` otherwise — asInvoker, **uiAccess="false"**.

Control Panel’s `app.manifest` is asInvoker with uiAccess false. Neither default manifest requests `requireAdministrator`.

`uiAccessRelease` also defines `uiAccess` in **Common**, so `AppConfig.UiAccess` is set to `VersionHelper.IsWindows8OrGreater()` at static construction. Other builds leave `UiAccess` at default **false**. When true, `PointCapture` creates `PointerInputTargetWindow` for touch consumption.

## Run as admin

`AppConfig.RunAsAdmin` is a persisted bool. `StartupHelper` uses it to prefer a scheduled task (`schtasks` with `Verb = runas`) over a simple Startup shortcut so the daemon can start elevated. Control Panel `StartDaemon` also sets `Verb = "runas"` if the UI process is already an administrator.

On `MainWindow` load, if either exe path is listed in `HKCU\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers` with `RUNASADMIN`, a compatibility warning is shown (mixed integrity capture vs UI).

## Store package

When `AppConfig.UiAccess` is true and the OS is Windows 10 or greater, Control Panel `TryLaunchStoreVersion` looks up package family `41908Transpy.GestureSign` (publisher `CN=AF41F066-0041-4D13-9D95-9DAB66112B0A`). If installed, it launches `explorer.exe shell:AppsFolder\41908Transpy.GestureSign_f441wk0cxr8zc!GestureSign` and shuts down the unpackaged UI. About also links Store product `9n45wqvk2qqw`. Centennial builds define `ConvertedDesktopApp` for that packaging flavor.

See [Build and configurations](build.md) and [Control panel](../architecture/control-panel.md).
