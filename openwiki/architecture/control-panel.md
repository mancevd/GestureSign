---
type: system
title: Control panel
description: WPF MahApps UI process that edits gestures and actions, launches the daemon, and reloads it over named pipes.
tags: [control-panel, wpf, ipc, ui]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-c39da0394d06b00f690f046e
    resource: repo://GestureSign.Common/Configuration/AppConfig.cs
  - id: openwiki-source-14f15b71d176f6cb46739ce4
    resource: repo://GestureSign.ControlPanel/App.xaml.cs
  - id: openwiki-source-ea42920460f88e067feab32d
    resource: repo://GestureSign.ControlPanel/MainWindow.xaml
  - id: openwiki-source-eb463386b4ca7b3ae25c788c
    resource: repo://GestureSign.ControlPanel/MainWindow.xaml.cs
  - id: openwiki-source-944c52c217c2e91d9e2115fe
    resource: repo://GestureSign.ControlPanel/MessageProcessor.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Control panel

`GestureSign.ControlPanel` is the WPF configuration process (`GestureSign.ControlPanel.exe`). It owns the MahApps.Metro `MainWindow` and loads the same gesture, application, and plugin managers as the daemon, but with a **null host** so plugins are listed for editing rather than bound to live capture.

## Startup, single instance, and Store redirect

`App.Application_Startup` opens the log file, loads Control Panel localization (XML file, then embedded English), and takes a mutex named `Constants.ControlPanel` (`GestureSignControlPanel`).

If this process created the mutex:

1. When `AppConfig.UiAccess` is true and the OS is Windows 10 or later, `TryLaunchStoreVersion` looks up Store package `41908Transpy.GestureSign` for the current user. If found, it starts `explorer.exe shell:AppsFolder\41908Transpy.GestureSign_f441wk0cxr8zc!GestureSign` and shuts down. `UiAccess` is set only in `uiAccess` builds (`AppConfig` static constructor).
2. Otherwise it loads `GestureManager`, `PluginManager`, and `ApplicationManager` with `null` (no `IPointCapture` / `IHostControl`).
3. It starts a named-pipe server on `Constants.ControlPanel` with `MessageProcessor`.
4. It shows `MainWindow`.

A second Control Panel instance does not open a second window. It finds the other process by name, restores a minimized main window via `ManagedWinapi` `SystemWindow`, foregrounds it, then shuts down on idle.

See [System overview](overview.md) and [Teaching and editing](../workflows/teaching.md).

## Reloading the daemon after saves

After the first instance is live, Control Panel pushes configuration into the running daemon:

- `ApplicationManager.ApplicationSaved` → `IpcCommands.LoadApplications`
- `GestureManager.GestureSaved` → `IpcCommands.LoadGestures`
- `AppConfig.ConfigChanged` → `IpcCommands.LoadConfiguration`

The inbound pipe handles `IpcCommands.Exit` (shutdown) and `IpcCommands.GotGesture` (stroke captured while teaching). `GotGesture` payload is `Point[][][]`, converted to `PointPattern[]` and raised as `MessageProcessor.GotNewPattern` on the UI dispatcher.

See [Named-pipe IPC](../integrations/ipc.md) and [Configuration and persistence](../operations/configuration.md).

## Main window surfaces

`MainWindow` is a `TouchWindow` (MahApps Metro) with five tabs:

| Tab | Control | Role |
| --- | --- | --- |
| Actions | `AvailableActions` | Per-application actions, commands, export/import |
| Ignored | `IgnoredApplications` | `IgnoredApp` list, enable/disable, import/export |
| Gestures | `AvailableGestures` | Named point patterns; add/edit/delete via `GestureDefinition` |
| Options | `Options` | AppConfig-backed settings (startup, devices, feedback) |
| About | inline | Version, homepage, Store link, feedback/log send |

A title-bar help button opens `About.HelpPageUrl`. About also links to GitHub and the Microsoft Store product page (`9n45wqvk2qqw`).

On `Loaded`, Control Panel:

- Warns if AppCompat `RUNASADMIN` is set for either exe (compatibility with elevated vs unelevated capture).
- Starts `GestureSign.exe` if the daemon mutex is free. If Control Panel is itself elevated, it uses `Verb = "runas"`. Missing daemon binary shows a localized error.
- Optionally prompts to send a crash report when `AppConfig.SendErrorReport` is on and a newer `.NET Runtime` Application Event Log error mentioning GestureSign exists.

Export/import of applications and gestures is shared through `ExportImportDialog`; ignored-app and action lists both host it. `DownloadWindow` can import from a file or remote source into the same dialog.

## Failure behavior

Unhandled AppDomain, dispatcher, and unobserved task exceptions are logged. `FileWriteException` appends a localized file-write message. The WPF dispatcher handler marks the exception handled then `Environment.Exit(0)`. Process exit disposes the named-pipe instance and the mutex.
