---
type: workflow
title: Non-stroke triggers
description: Hotkey, mouse-button, and continuous-direction triggers that reuse PluginManager.ExecuteAction without a named stroke match.
tags: [triggers, hotkey, mouse, continuous]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-67c8fa365fa8cc0d29946c87
    resource: repo://GestureSign.Daemon/Triggers/ContinuousGestureTrigger.cs
  - id: openwiki-source-d1de0fae306134369e1c613a
    resource: repo://GestureSign.Daemon/Triggers/HotKeyManager.cs
  - id: openwiki-source-a207bc7a25d82abe2ea5dbe9
    resource: repo://GestureSign.Daemon/Triggers/MouseTrigger.cs
  - id: openwiki-source-9afe2cdbf51b79ec30dfbc71
    resource: repo://GestureSign.Daemon/Triggers/TriggerManager.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Non-stroke triggers

`TriggerManager.Load` registers three `Trigger` subclasses. Each `TriggerFired` event carries `List<IAction>` and a `Point`. The manager calls `PluginManager.ExecuteAction` with `PointCapture.Mode`, `SourceDevice`, a dummy contact id `1`, and a one-point stroke at `FiredPoint` — the same pipeline as [recognition](recognition-and-execution.md), including Training skip and `UserDisabled` filtering.

## HotKeyManager

On construct, it gathers `Hotkey` actions from the **foreground** window’s apps (not ignored) plus `GlobalApp`, then `Register()` each unique modifier+vk via ManagedWinapi `Hotkey`. Duplicate keys share an action list.

`ForegroundApplicationsChanged` rebuilds the map. `UserDisabled` **unloads** all hotkeys (they are not re-registered until foreground apps change again).

Press: `GetForegroundApplications()` (updates `_recognizedApplication`) then fire with the window rectangle location. `HotkeyAlreadyInUseException` unregisters that key.

## MouseTrigger

Subscribes to the same `LowLevelMouseHook` used for drawing. It only runs when `SourceDevice == Mouse` and state is `CapturingInvalid` or `TriggerFired` (drawing button is down but the stroke is not yet a valid gesture, or a trigger already fired).

- Wheel: `MouseHotkey` WheelForward/Backward, set `TriggerFired`, `handled` unless UserDisabled.
- MouseDown: if a matching `MouseHotkey` exists, mark `handled` (consume) but do not execute yet.
- MouseUp: execute matching `MouseHotkey` actions, set `TriggerFired`.

`GetRecognizedDefinedAction(predicate)` uses the currently recognized apps, then global.

## ContinuousGestureTrigger

On `PointCaptured` while `Capturing` with **at least two** contacts, it looks for actions with `ContinuousGesture`. After a baseline sample, average delta across contacts decides horizontal vs vertical. Distance vs time (`GetRateOfFire`) may fire Left/Right/Up/Down multiple times, matching `ContactCount` and `Gestures` flags. Threshold is `20` CSS pixels scaled by system DPI/96. `CaptureEnded` resets the stopwatch.

See [Daemon process](../architecture/daemon.md).
