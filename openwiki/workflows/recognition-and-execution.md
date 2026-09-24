---
type: workflow
title: Recognition and execution
description: End-to-end from CaptureStarted window match through gesture probability to sequential plugin Gestured calls.
tags: [recognition, execution, plugins]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-45017e839ae965b1eeded497
    resource: repo://GestureSign.Common/Applications/ApplicationManager.cs
  - id: openwiki-source-e9423083c49453fdfef1670f
    resource: repo://GestureSign.Common/Plugins/PluginManager.cs
  - id: openwiki-source-548934ef6dc1c13ac8271fba
    resource: repo://GestureSign.Daemon/Input/PointCapture.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Recognition and execution

This is the live path after [input capture](input-capture.md) accepts a stroke. Teaching mode shares the first half but does not run plugins.

## 1. Window match (`CaptureStarted`)

`ApplicationManager` (when loaded with `PointCapture`) handles `CaptureStarted`:

- Ignore if `Training`.
- Resolve `CaptureWindow` from the first point (`GetWindowFromPoint` / `GetRealWindow`).
- Set `_recognizedApplication` from that window.
- Enabled `IgnoredApp` → `e.Cancel = true`.
- `GlobalApp` + `IgnoreFullScreen` + full-screen window → cancel.
- Touch with fewer contacts than `LimitNumberOfFingers` → cancel.
- Copy max `UserApp.BlockTouchInputThreshold` into the event for uiAccess filtration.

`BeforePointsCaptured` may replace `_recognizedApplication` with foreground-matched `MatchActivated` UserApps.

## 2. Pattern match

Same `BeforePointsCaptured`, `GestureManager` writes `GestureName` using stacked `GetGestureSetNameMatch` (probability > 80, 800 ms stack timeout). See [Gestures](../concepts/gestures.md).

## 3. Recognize

`EndCapture` raises `BeforePointsCaptured` first (so `GestureName` is current). If `Training` and the stroke is not a one-point tap, it sends `GotGesture` and does **not** rely on plugins. If `GestureName` is non-null it raises `GestureRecognized` with points, first points, and contact ids.

## 4. Execute

`PluginManager.PointCapture_GestureRecognized` loads `GetRecognizedDefinedAction(gestureName)` (user apps, else global) and calls `ExecuteAction`.

`ExecuteAction` returns immediately when `mode == Training`. Otherwise a single chained `Task` runs:

For each action: skip null, `IgnoredDevices` overlap, empty commands, or failed `Condition`. For each enabled command: skip all plugins except `GestureSign.CorePlugins.ToggleDisableGestures` when `UserDisabled`. `CaptureWindow.WaitForIdle(200)`. Resolve plugin by class+filename. On the first command, foreground the window if `ActivateWindow` is true or (null and plugin `ActivateWindowDefault`). `Deserialize` then `Gestured(PointInfo)`.

Exceptions on the task are logged. Concurrent gestures queue behind `_lastActionTask`.

Non-stroke [triggers](triggers.md) call the same `ExecuteAction` with a one-point fake stroke.

See [Applications and actions](../concepts/applications-actions.md) and [Plugin system](../concepts/plugins.md).
