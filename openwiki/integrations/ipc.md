---
type: integration
title: Named-pipe IPC
description: Per-user named pipes carrying IpcCommands between daemon and Control Panel, plus a device-state reply pipe.
tags: [ipc, named-pipe, daemon]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-30a5ac61443f952531d50eb1
    resource: repo://GestureSign.Common/InterProcessCommunication/IpcCommands.cs
  - id: openwiki-source-e0587053224ef12231715e87
    resource: repo://GestureSign.Common/InterProcessCommunication/NamedPipe.cs
  - id: openwiki-source-5814d228c6f00ef94a7afb68
    resource: repo://GestureSign.ControlPanel/MainWindowControls/Options.cs
  - id: openwiki-source-944c52c217c2e91d9e2115fe
    resource: repo://GestureSign.ControlPanel/MessageProcessor.cs
  - id: openwiki-source-5467a0a77f93db372cc439a4
    resource: repo://GestureSign.Daemon/Input/InputProvider.cs
  - id: openwiki-source-ef096fd78b783df7d3121a25
    resource: repo://GestureSign.Daemon/MessageProcessor.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Named-pipe IPC

Processes talk through `NamedPipe` / `CustomNamedPipeServer`. Physical pipe names are `pipeName` plus `"-"` and the current user’s SID (`GetUserPipeName`), so sessions do not collide.

Payloads are a one-byte `IpcCommands` followed by an optional `BinaryFormatter` object. Receiving servers allow Builtin Users `ReadWrite`. One inbound connection is processed, then `Disconnect` and wait again. Failures log and restart the receive loop.

`SendMessageAsync` can wait up to one second for the server (`wait: true`) or fail immediately if the pipe is missing (`wait: false` — used for `GotGesture` and second-instance `StartControlPanel`).

## Command map

Enum values in `IpcCommands`:

| Command | Typical sender | Handler |
| --- | --- | --- |
| `StartControlPanel` | Second daemon instance | Daemon: `TrayManager.StartControlPanel` |
| `StartTeaching` | Control Panel teaching UI | Daemon: `PointCapture.Mode = Training` |
| `StopTraining` | Teaching UI | Daemon: `Normal` unless `UserDisabled` |
| `LoadApplications` | `ApplicationSaved` | Daemon: `LoadApplications().Wait()` |
| `LoadGestures` | `GestureSaved` | Daemon: `LoadGestures().Wait()` |
| `LoadConfiguration` | `AppConfig.ConfigChanged` | Daemon: `AppConfig.Reload()` |
| `GotGesture` | `PointCapture` in training | Control Panel: `GotNewPattern` |
| `Exit` | Daemon tray Exit | Control Panel: `Shutdown` |
| `SynDeviceState` | Device-state **send** server | Control Panel Options `GetMessageAsync` |
| `ConfigReload` | defined in enum | **no handler** in either `MessageProcessor` |

Daemon `MessageProcessor` always `Post`s onto the UI `SynchronizationContext` captured at startup. Control Panel `MessageProcessor` uses `Dispatcher.InvokeAsync` at `DispatcherPriority.Input`.

## Device-state pipe

`InputProvider` constructs a second `CustomNamedPipeServer` named `GestureSignDaemonDeviceState` in **outbound** mode: clients `GetMessageAsync` and receive `SynDeviceState` plus `HidDevice.EnumerateDevices()`. The server is refreshed on config change, session unlock/logon, and power resume so Options can show attached pen/touch hardware.

See [System overview](../architecture/overview.md) and [Teaching and editing](../workflows/teaching.md).
