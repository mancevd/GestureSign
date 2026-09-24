---
type: overview
title: Quickstart
description: How to restore, build, and run GestureSign, and where the wiki documents capture, plugins, and persistence.
tags: [quickstart, routing, build]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-1601dc4304e3854313f15d32
    resource: repo://Directory.Build.props
  - id: openwiki-source-14f15b71d176f6cb46739ce4
    resource: repo://GestureSign.ControlPanel/App.xaml.cs
  - id: openwiki-source-f2aebf9e5a1384f07a81ba06
    resource: repo://GestureSign.Daemon/Program.cs
  - id: openwiki-source-23775c3de52f3ab95a13cb8b
    resource: repo://README.md
  - id: openwiki-source-b3fc614a0aeef09a063adea5
    resource: repo://setup.ps1
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Quickstart

GestureSign is a Windows gesture recognizer: draw with fingers, pen, or mouse; matched strokes run plugin commands. The product is two processes (`GestureSign.exe` daemon, `GestureSign.ControlPanel.exe` editor) described in [System overview](architecture/overview.md). **This repository has no automated test projects.**

## Build and run

1. Run `setup.ps1` to restore NuGet packages and junction local .NET Framework reference assemblies (needed because `Directory.Build.props` bypasses installed targeting packs). Details: [Build and configurations](operations/build.md).
2. Build `GestureSign.sln` with MSBuild, for example Debug Any CPU. Output is `bin\Debug\` (`GestureSign.exe` + `GestureSign.ControlPanel.exe` + `GestureSign.CorePlugins.dll`). Extra plugins copy into `Plugins\`.
3. Start **Control Panel**; on load it starts the daemon if the daemon mutex is free. A second daemon launch opens the Control Panel instead. Privileged/Store variants: [Privileges and uiAccess](operations/privileges.md).

User data lives in roaming/local AppData, or `{exe}\AppData` in Portable builds. Files: `GestureSign.config`, `Gestures.gest`, `Actions.gsa`. See [Configuration and persistence](operations/configuration.md).

## Where to read next

| Task | Page |
| --- | --- |
| Process split, assemblies, stroke-to-action | [System overview](architecture/overview.md) |
| Always-running capture host, tray, load order | [Daemon process](architecture/daemon.md) |
| WPF tabs, mutex, Store redirect | [Control panel](architecture/control-panel.md) |
| Singletons, HostControl, vendored libs | [Shared runtime](architecture/shared-runtime.md) |
| HID/mouse capture state machine | [Input capture](workflows/input-capture.md) |
| Match window → gesture → plugins | [Recognition and execution](workflows/recognition-and-execution.md) |
| Teach a new stroke | [Teaching and editing](workflows/teaching.md) |
| Hotkey / mouse / continuous triggers | [Non-stroke triggers](workflows/triggers.md) |
| IPlugin discovery and CorePlugins | [Plugin system](concepts/plugins.md) |
| IPC command map | [Named-pipe IPC](integrations/ipc.md) |
| Win32 hooks and SendInput | [Windows input and windowing](integrations/windows-apis.md) |
| Languages and GestureSign.log | [Localization and logging](integrations/localization.md) |

README lists the built-in action categories (window control, keyboard/mouse simulation, volume, Store apps, and so on); those implementations live in `GestureSign.CorePlugins`.
