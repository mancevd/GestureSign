---
type: concept
title: Applications and actions
description: Window matching (UserApp, GlobalApp, IgnoredApp), per-gesture actions with commands and conditions, persisted as Actions.gsa.
tags: [applications, actions, matching, persistence]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-f22ab0bac1473270102e7318
    resource: repo://GestureSign.Common/Applications/Action.cs
  - id: openwiki-source-45017e839ae965b1eeded497
    resource: repo://GestureSign.Common/Applications/ApplicationManager.cs
  - id: openwiki-source-bc6ad47bc7c7b0509a1d75ec
    resource: repo://GestureSign.Common/Applications/Command.cs
  - id: openwiki-source-3a8e39c3f0aa30703f85e995
    resource: repo://GestureSign.Common/Applications/GlobalApp.cs
  - id: openwiki-source-564055b14325fdad3e7ff909
    resource: repo://GestureSign.Common/Applications/IgnoredApp.cs
  - id: openwiki-source-5358a1580ddaed8584ce8d43
    resource: repo://GestureSign.Common/Applications/MatchUsing.cs
  - id: openwiki-source-071c8acc30b01890d10f7a66
    resource: repo://GestureSign.Common/Applications/UserApp.cs
  - id: openwiki-source-1e2714647cf248fd3947a707
    resource: repo://GestureSign.Common/Constants.cs
  - id: openwiki-source-e9423083c49453fdfef1670f
    resource: repo://GestureSign.Common/Plugins/PluginManager.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Applications and actions

An **application** is a match rule plus a list of **actions**. An action binds a gesture name (or hotkey / mouse / continuous trigger) to one or more **commands**, each pointing at an `IPlugin` class in a DLL.

## Application kinds

All kinds share `ApplicationBase`: `Name`, `MatchUsing`, `MatchString`, `IsRegEx`, `MatchActivated`, `Group`, `Actions`.

| Type | Role |
| --- | --- |
| `GlobalApp` | Catch-all. Name and `MatchUsing.All` are fixed. `MatchActivated` is always false. Default finger limit is at least 2. |
| `UserApp` | Window-specific. Extra `LimitNumberOfFingers` (default 2) and `BlockTouchInputThreshold`. |
| `IgnoredApp` | If `IsEnabled` and the window matches, capture is cancelled at `CaptureStarted`. |

`MatchUsing` is class name, window title, executable filename, or `All`. Matching compares `MatchString` to the window (plain or regex). `MatchUsing.All` is skipped in the per-window matcher.

`GetApplicationFromWindow` prefers matching User/Ignored apps; if none match it returns the global app (creating an empty `GlobalApp` if missing).

On **CaptureStarted** (not training): resolve the window under the first captured point. Enabled ignored apps cancel. `AppConfig.IgnoreFullScreen` can cancel for `GlobalApp` when the point is in a full-screen window. Touch capture is cancelled if contact count is below the max `LimitNumberOfFingers`. The max `BlockTouchInputThreshold` among matched `UserApp`s is passed back for pointer filtration.

On **BeforePointsCaptured**, if any `UserApp` has `MatchActivated`, the **foreground** window is tried first; otherwise matching uses the window under the first point again. Windows 10 `ApplicationFrameWindow` hosts are unwrapped via `GetRealWindow`.

## Actions and commands

`Action` fields:

- `GestureName` — stroke match key.
- `Commands` — ordered plugin invocations.
- `Condition` — optional expression over finger start/end coordinates (and `%` of virtual screen) and contact `ID`. Empty/whitespace means true. Evaluated with `DataTable.Compute`; `EvaluateException` fails the action.
- `ActivateWindow` — if null, uses the plugin’s `ActivateWindowDefault`; if true, foregrounds `CaptureWindow` before the first command.
- `Hotkey`, `MouseHotkey`, `ContinuousGesture` — non-stroke triggers.
- `IgnoredDevices` — skip when capture source overlaps this mask.

`Command` stores `PluginClass`, `PluginFilename`, serialized `CommandSettings`, display `Name`, and `IsEnabled`.

`GetRecognizedDefinedAction(gestureName)` looks up enabled commands on `_recognizedApplication`, then falls back to global actions with the same gesture name.

## Persistence

The list is JSON at `AppConfig.ApplicationDataPath` + `Actions.gsa` (`Constants.ActionFileName`) with Newtonsoft type names. Load tries the live file, then backup, then `Defaults\Actions.gsa` beside the exe. `SaveApplications` raises `ApplicationSaved` so the Control Panel can tell the daemon to reload.

See [Recognition and execution](../workflows/recognition-and-execution.md) and [Plugin system](plugins.md).
