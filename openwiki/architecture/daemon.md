---
type: system
title: Daemon process
description: Always-running GestureSign.exe WinForms host that captures input, recognizes gestures, runs plugins, and serves IPC.
tags: [daemon, runtime, tray, ipc]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-ef096fd78b783df7d3121a25
    resource: repo://GestureSign.Daemon/MessageProcessor.cs
  - id: openwiki-source-f2aebf9e5a1384f07a81ba06
    resource: repo://GestureSign.Daemon/Program.cs
  - id: openwiki-source-03d63b321fce283a3fecbe14
    resource: repo://GestureSign.Daemon/TrayManager.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Daemon process

`GestureSign.Daemon` builds as `GestureSign.exe` (`WinExe`). It is the always-running capture and execution host: a hidden WinForms `Application.Run()` loop with a tray icon, not a visible main form.

## Single instance

`Program.Main` takes a mutex named `Constants.Daemon` (`GestureSignDaemon`). The first instance proceeds through startup. A later instance sends `IpcCommands.StartControlPanel` to the existing daemon (non-blocking wait) and exits, so launching the daemon again opens the Control Panel instead of a second capture stack.

## Startup order

On a new instance the daemon:

1. Hooks `Application.ThreadException` and `Logging.LoggedExceptionOccurred`, then opens the log file.
2. Loads Daemon localization from file, falling back to embedded English (`Properties.Resources.en`).
3. `PointCapture.Instance.Load()` — HID/mouse capture, surface, filtration.
4. Captures `SynchronizationContext.Current` for UI-thread plugin work.
5. `TriggerManager.Instance.Load()` — hotkey, mouse, and continuous-gesture triggers.
6. `GestureManager.Instance.Load(PointCapture.Instance)` and `ApplicationManager.Instance.Load(PointCapture.Instance)` so both subscribe to capture events.
7. Builds a `HostControl` pointing at those managers plus `PluginManager` and `TrayManager`.
8. `PluginManager.Instance.Load(hostControl, uiContext)` — loads `GestureSign.CorePlugins.dll` and `Plugins\*.dll`, then subscribes to `GestureRecognized`.
9. `TrayManager.Instance.Load()` — notify icon according to `AppConfig.ShowTrayIcon`.
10. `NamedPipe.Instance.RunNamedPipeServer(Constants.Daemon, new MessageProcessor(uiContext))`.
11. `Application.Run()`.

Exceptions during this sequence are logged, shown in a message box, and cause `Application.Exit()`.

## Message processor

Daemon IPC is posted onto the captured UI `SynchronizationContext`:

| Command | Effect |
| --- | --- |
| `StartTeaching` | `PointCapture.Mode = Training` |
| `StopTraining` | Restore `Normal` unless already `UserDisabled` |
| `LoadApplications` | Reload `Actions.gsa` |
| `LoadGestures` | Reload `Gestures.gest` |
| `LoadConfiguration` | `AppConfig.Reload()` |
| `StartControlPanel` | `TrayManager.StartControlPanel()` |

See [Named-pipe IPC](../integrations/ipc.md).

## Tray and disable

The notify icon menu can disable gestures, start `GestureSign.ControlPanel.exe` from the same directory, or exit. Exit sends `IpcCommands.Exit` to the Control Panel then `Application.Exit()`. Double-click (when not training) or middle-click toggles `PointCapture.ToggleUserDisablePointCapture()`. Icon art tracks mode: stop when user-disabled, add when training, otherwise the normal daemon icon. `ShowTrayIcon` is reapplied on `AppConfig.ConfigChanged`.

## Shutdown

`ApplicationExit` disposes the named-pipe server and `PointCapture.Instance`. The tray hides its icon on the same event. See [Input capture](../workflows/input-capture.md) and [Non-stroke triggers](../workflows/triggers.md).
