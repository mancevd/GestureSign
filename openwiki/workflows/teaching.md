---
type: workflow
title: Teaching and editing
description: Training-mode IPC, GestureSelector stacking, GestureDefinition/ActionDialog/CommandDialog saves that reload the daemon.
tags: [teaching, training, ui, ipc]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-14f15b71d176f6cb46739ce4
    resource: repo://GestureSign.ControlPanel/App.xaml.cs
  - id: openwiki-source-13491c7fd147f4b396ace923
    resource: repo://GestureSign.ControlPanel/Dialogs/GestureDefinition.xaml.cs
  - id: openwiki-source-944c52c217c2e91d9e2115fe
    resource: repo://GestureSign.ControlPanel/MessageProcessor.cs
  - id: openwiki-source-8ea2db2f9dd4c8b1e37c98ca
    resource: repo://GestureSign.ControlPanel/UserControls/GestureSelector.xaml.cs
  - id: openwiki-source-548934ef6dc1c13ac8271fba
    resource: repo://GestureSign.Daemon/Input/PointCapture.cs
  - id: openwiki-source-ef096fd78b783df7d3121a25
    resource: repo://GestureSign.Daemon/MessageProcessor.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Teaching and editing

Users define strokes in the Control Panel; the daemon must enter `CaptureMode.Training` so it records raw patterns instead of running actions.

## Training loop

`GestureSelector` (embedded in `GestureDefinition` and action UI) owns teaching:

- On load with no patterns, or **Redraw**, it subscribes to `MessageProcessor.GotNewPattern` and sends `StartTeaching` to the daemon.
- Unload or after a pattern arrives: unsubscribe and `StopTraining` (daemon restores `Normal` unless already `UserDisabled`).
- Daemon `EndCapture` in Training, for strokes that are not a single one-point tap, sends `GotGesture` with `Point[][][]`. Control Panel converts them to `PointPattern[]`.
- If similar name exists (`GetMostSimilarGestureName`), the selector shows “existing” and may reuse that sample unless it is the gesture being edited. Otherwise it builds a nameless `Gesture` with the new patterns.
- Double-click thumbnail while `PointPatterns.Length < 3` stacks another stroke (`_stackUp`) by concatenating patterns, matching live stacked gestures.

If `GotGesture` send fails, the daemon drops back to `Normal`.

## Saving gestures

`GestureDefinition.cmdDone` keeps the old name when editing, optionally deleting a colliding name and renaming actions (`RenameGestures` + `SaveApplications`). `SaveGesture` assigns `GetNewGestureName()` if empty, replaces any existing same-name gesture, `AddGesture`, `SaveGestures`. That raises `GestureSaved` → `LoadGestures` on the daemon.

## Actions and commands

`AvailableActions` / `ActionDialog` edit `IAction` (gesture name, condition, hotkey, mouse, continuous, ignored devices, activate window). `CommandDialog` picks an `IPluginInfo`, serializes settings, and writes `PluginClass` / `PluginFilename`. Application list saves raise `ApplicationSaved` → `LoadApplications`. Options changes raise `AppConfig.ConfigChanged` → `LoadConfiguration`.

Export/import (`ExportImportDialog`, `DownloadWindow`) writes or merges the same `IApplication`/`IGesture` stores and then saves, so the same IPC reload path applies.

See [Control panel](../architecture/control-panel.md), [Named-pipe IPC](../integrations/ipc.md), [Gestures](../concepts/gestures.md).
