---
type: integration
title: Windows input and windowing
description: How GestureSign uses Raw Input, low-level mouse hooks, WinEvents, SystemWindow, pointer injection, and SendInput.
tags: [win32, raw-input, hooks, sendinput]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-10c2b1b53bb859e2728bd41d
    resource: repo://GestureSign.CorePlugins/HotKey/HotKeyPlugin.cs
  - id: openwiki-source-5467a0a77f93db372cc439a4
    resource: repo://GestureSign.Daemon/Input/InputProvider.cs
  - id: openwiki-source-a1e10f039339c4a0a765740e
    resource: repo://GestureSign.Daemon/Input/MessageWindow.cs
  - id: openwiki-source-548934ef6dc1c13ac8271fba
    resource: repo://GestureSign.Daemon/Input/PointCapture.cs
  - id: openwiki-source-329357af2a6f1a830982863e
    resource: repo://WindowsInput/InputSimulator.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Windows input and windowing

GestureSign does not own a custom kernel driver. Capture and injection sit on user-mode Win32 APIs, mostly through `GestureSign.Daemon.Native`, vendored **ManagedWinapi**, and vendored **WindowsInput**.

## Raw Input and HID

`MessageWindow` is a message-only HWND (`Parent = HWND_MESSAGE`, `WS_EX_NOACTIVATE`, caption `GSMessageWindow`). It `RegisterRawInputDevices` with `RIDEV_INPUTSINK | RIDEV_DEVNOTIFY` so HID reports arrive even when GestureSign is not focused. Touch screen, touchpad, and pen parsers (`TouchScreenDevice`, `TouchPadDevice`, `PenDevice`) turn reports into `RawData` lists raised as `PointsIntercepted`.

Registration is updated when config or handle changes; devices can be removed with `RIDEV_REMOVE`. Session unlock and power resume re-enumerate hardware via the [device-state pipe](ipc.md).

## Mouse hook

When `AppConfig.DrawingButton` is not `None`, `InputProvider` starts ManagedWinapi `LowLevelMouseHook` (WH_MOUSE_LL). That path lets users draw with a mouse button instead of (or in addition to) digitizer contacts. Config reload starts or unhooks the hook.

## WinEvents and windows

`PointCapture` constructor calls `SetWinEventHook` from `EVENT_SYSTEM_FOREGROUND` through `EVENT_SYSTEM_MINIMIZEEND`, out-of-context, skipping its own process. That drives foreground-application updates used with `MatchActivated` and uiAccess touch blocking.

Window identity uses ManagedWinapi `SystemWindow`: class name, title, process path, `FromPointEx` for the window under a gesture start, `ForegroundWindow` for activation and MatchActivated. Windows 10 hosted Store apps unwrap `ApplicationFrameWindow` in `ApplicationManager.GetRealWindow`.

## Pointer injection (uiAccess)

If `AppConfig.UiAccess` is true, `PointerInputTargetWindow` can register as a pointer input target and consume/block touch frames when `BlockTouchInputThreshold >= 2`, so the gesture does not click through. User-disabled mode zeros the threshold.

## SendInput

Core plugins (hotkey, keystrokes, mouse actions, volume keys, next/previous app) construct `WindowsInput.InputSimulator`, which wraps `SendInput`. `PointCapture` also uses it for some mouse-button drawing paths. This is why [uiAccess](../operations/privileges.md) matters for injecting into elevated windows.

See [Input capture](../workflows/input-capture.md) and [Plugin system](../concepts/plugins.md).
