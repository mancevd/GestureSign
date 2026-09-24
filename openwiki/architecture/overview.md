---
type: "Reference"
title: "System overview"
openwiki_generated: true
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-1e2714647cf248fd3947a707
    resource: repo://GestureSign.Common/Constants.cs
  - id: openwiki-source-8502aeeb8a0f093c3dbfda82
    resource: repo://GestureSign.Common/Gestures/GestureManager.cs
  - id: openwiki-source-e9423083c49453fdfef1670f
    resource: repo://GestureSign.Common/Plugins/PluginManager.cs
  - id: openwiki-source-14f15b71d176f6cb46739ce4
    resource: repo://GestureSign.ControlPanel/App.xaml.cs
  - id: openwiki-source-f2aebf9e5a1384f07a81ba06
    resource: repo://GestureSign.Daemon/Program.cs
  - id: openwiki-source-6d1362e22e610442742bfb51
    resource: repo://GestureSign.sln
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---


# System overview

GestureSign is a Windows tablet/mouse gesture recognizer. Users draw strokes; the software matches stored point patterns and runs plugin commands (keyboard, mouse, window control, programs, volume, and more). There is no automated test project in this repository.

## Two processes

| Process | Assembly | Mutex | Role |
| --- | --- | --- | --- |
| Daemon | `GestureSign.exe` from `GestureSign.Daemon` | `GestureSignDaemon` | Global input, recognition, plugin execution, tray |
| Control Panel | `GestureSign.ControlPanel.exe` | `GestureSignControlPanel` | WPF editor for gestures, apps, actions, options |

The Control Panel starts the daemon if it is not running. A second daemon launch asks the first instance to open the Control Panel. A second Control Panel instance restores the existing window.

They share `GestureSign.Common` singletons (`GestureManager`, `ApplicationManager`, `PluginManager`, `AppConfig`) and talk over named pipes (`IpcCommands`). Saves in the UI reload files in the daemon. Teaching mode flips `PointCapture.Mode` on the daemon and returns captured strokes as `GotGesture`.

## Assemblies

- **GestureSign.Common** — persistence, matching, plugin host, IPC, localization, logging.
- **GestureSign.PointPatterns** — angular interpolation matcher used by `GestureManager`.
- **GestureSign.CorePlugins** — built-in `IPlugin` actions, copied next to the exe.
- **GestureSign.ExtraPlugins** — optional DLLs (`ClipboardMatch`, `TextCopyer`) loaded from a `Plugins` folder.
- **ManagedWinapi** / **WindowsInput** — hooks, `SystemWindow`, SendInput simulation.

Solution configurations include Debug, Release, Portable (config next to the exe), `uiAccessRelease` (manifest `uiAccess=true`), and Centennial (`ConvertedDesktopApp`). See [Build and configurations](../operations/build.md).

## Stroke to action

1. [Input capture](../workflows/input-capture.md) records contacts from HID/Raw Input or a low-level mouse hook.
2. [Application matching](../concepts/applications-actions.md) binds the stroke to `GlobalApp`, `UserApp`, or `IgnoredApp` for the window under the first point (or the foreground window when `MatchActivated`).
3. [Gesture matching](../concepts/gestures.md) scores the stroke against `Gestures.gest` (probability ≥ 80, optional stacked gestures within 800 ms).
4. `GestureRecognized` selects actions for that name on the recognized application.
5. [Plugin execution](../concepts/plugins.md) deserializes each enabled command and calls `IPlugin.Gestured`.

Hotkeys, mouse buttons/wheel, and continuous gestures skip full pattern match but reuse `PluginManager.ExecuteAction`. See [Daemon process](daemon.md), [Control panel](control-panel.md), [Shared runtime](shared-runtime.md), and [Named-pipe IPC](../integrations/ipc.md).
