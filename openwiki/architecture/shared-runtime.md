---
type: architecture
title: Shared runtime
description: GestureSign.Common singleton managers, HostControl, JSON file helpers, and vendored matching and input libraries.
tags: [common, singletons, hostcontrol]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-45017e839ae965b1eeded497
    resource: repo://GestureSign.Common/Applications/ApplicationManager.cs
  - id: openwiki-source-8502aeeb8a0f093c3dbfda82
    resource: repo://GestureSign.Common/Gestures/GestureManager.cs
  - id: openwiki-source-b175f457bcf7dad8a46f57c1
    resource: repo://GestureSign.Common/Plugins/HostControl.cs
  - id: openwiki-source-e9423083c49453fdfef1670f
    resource: repo://GestureSign.Common/Plugins/PluginManager.cs
  - id: openwiki-source-46328d3a25d2fbc0d4dac3f3
    resource: repo://GestureSign.PointPatterns/PointPatternAnalyzer.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Shared runtime

`GestureSign.Common` is the in-process library used by both the daemon and the Control Panel. It is not a service; each process constructs its own singletons and optionally wires them to live capture.

## Singleton managers

| Manager | Persistence | Daemon `Load` | Control Panel `Load` |
| --- | --- | --- | --- |
| `GestureManager.Instance` | `Gestures.gest` JSON | `Load(PointCapture)` subscribes to capture | `Load(null)` — edit only |
| `ApplicationManager.Instance` | `Actions.gsa` JSON with type names | same | `Load(null)` |
| `PluginManager.Instance` | none (discovers DLLs) | `Load(host, uiContext)` + `GestureRecognized` | `Load(null)` — GUI metadata only |
| `AppConfig` | `GestureSign.config` | reload via IPC | live editor + `ConfigChanged` |

Constructors start `LoadingTask` (`LoadGestures` / `LoadApplications`) immediately. Until that task completes, `ApplicationManager.Applications` returns an empty list rather than a half-loaded store.

`ILoadable` is a parameterless `Load()` used by daemon-only types (`PointCapture`, `TrayManager`). Gesture and application managers use an overload that takes `IPointCapture` so they can skip event wiring in the UI process.

## HostControl

Plugins see the world through `IHostControl`. The daemon fills `HostControl` public fields with the live managers, capture, and tray. Control Panel never constructs a host (`PluginManager.Load(null)`), so plugin `Gestured` is not hooked to recognition there.

## File helpers

`FileManager` serializes JSON with Newtonsoft. It can wait on locked files, write a backup before overwrite, and deserialize actions with `TypeNameHandling.Objects` plus `ActionConverter`/`CommandConverter`. Failures wrap as `FileWriteException` and go through `Logging.LogAndNotice`.

See [Configuration and persistence](../operations/configuration.md).

## Matching and OS libraries

- **GestureSign.PointPatterns** — product matcher: interpolate each stroke to `Precision` (default 100) angular margins and score probability. Used only by `GestureManager`.
- **ManagedWinapi** — vendored hooks (`LowLevelMouseHook`) and `SystemWindow` for HWND, class, title, and process path.
- **WindowsInput** — vendored SendInput wrappers used by keyboard/mouse plugins.

These last two are in-tree libraries, not NuGet packages. See [Gestures](../concepts/gestures.md), [Applications and actions](../concepts/applications-actions.md), [Plugin system](../concepts/plugins.md), and [Windows input and windowing](../integrations/windows-apis.md).
