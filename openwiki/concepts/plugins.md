---
type: concept
title: Plugin system
description: IPlugin discovery from CorePlugins.dll and Plugins\\*.dll, host injection, serialization, and sequential Gestured execution.
tags: [plugins, extension, execution]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-831e38d57603780df3a41ea2
    resource: repo://GestureSign.Common/Plugins/IPlugin.cs
  - id: openwiki-source-e9423083c49453fdfef1670f
    resource: repo://GestureSign.Common/Plugins/PluginManager.cs
  - id: openwiki-source-e50f0ec316507ed4365a2548
    resource: repo://GestureSign.ExtraPlugins/ClipboardMatch/ClipboardMatch.cs
  - id: openwiki-source-efe9cefc015fb1276b2e6bf8
    resource: repo://GestureSign.ExtraPlugins/ClipboardMatch/ClipboardMatch.csproj
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Plugin system

Plugins implement `IPlugin`: display metadata (`Name`, `Category`, `Description`, `Icon`, `GUI`), `IsAction`, `ActivateWindowDefault`, `Initialize`, `Serialize`/`Deserialize` of command settings, and `Gestured(PointInfo)`. The host injects `IHostControl` before `Initialize`.

`PluginInfo` records the live instance, full class name, and DLL file name — the same pair stored on `Command`.

## Discovery

`PluginManager.LoadPlugins` clears the list, then:

1. Loads `GestureSign.CorePlugins.dll` beside the executing assembly (Common’s directory at runtime is the app output folder).
2. If a `Plugins` subdirectory exists, loads every `*.dll` there.

Assemblies are `Assembly.Load(File.ReadAllBytes(...))` to avoid file-lock `NotSupportedException`. Every exported type whose interface list contains `"IPlugin"` is constructed, given `HostControl`, and `Initialize`d. The assembly is also registered with `LocalizationProvider`. `LoadPlugins` returns `true` only if **no** plugin file was found.

Extra plugins (`ClipboardMatch`, `TextCopyer`) post-build `copy $(TargetPath) "$(TargetDir)\Plugins"`. They are optional drop-ins, not referenced by the daemon project.

## Execution

On `GestureRecognized`, the manager asks `ApplicationManager` for actions for `e.GestureName` and calls `ExecuteAction`.

`ExecuteAction` returns immediately in `CaptureMode.Training`. Otherwise it queues work on a single chained `Task` (`_lastActionTask`) so commands run **one pipeline after another**, with faulted tasks logged.

For each action:

- Skip if `IgnoredDevices` overlaps the capture source, or `Compute(Condition)` is false.
- For each enabled command: if mode is `UserDisabled`, skip unless `PluginClass` is `GestureSign.CorePlugins.ToggleDisableGestures`.
- `CaptureWindow.WaitForIdle(200)`.
- Resolve plugin by class + filename; skip if missing.
- Before the **first** command of an action, optionally foreground the target window (`ActivateWindow` or plugin default).
- `Deserialize(CommandSettings)` then `Gestured(pointInfo)`.

`PointInfo` carries first points, full strokes, target `SystemWindow`, and a `SynchronizationContext` (`Invoke` uses `Send`). `Window` re-resolves from the first point if the foreground HWND changed.

## Core plugin surface

`GestureSign.CorePlugins` covers the README action set: activate/next/previous window, min/max, topmost, touch keyboard, hotkeys and key down/up, mouse actions, send keystrokes, default browser, brightness, volume, run command, launch Store app, send message, open file, delay, notify, plus GestureSign-specific disable (`TemporarilyDisable`, `ToggleDisableGestures`). Extra plugins add clipboard match and text copy.

See [Recognition and execution](../workflows/recognition-and-execution.md) and [Shared runtime](../architecture/shared-runtime.md).
