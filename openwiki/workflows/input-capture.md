---
type: workflow
title: Input capture
description: HID and mouse-hook points flow through MessageWindow, PointEventTranslator, PointCapture state machine, optional touch blocking, and SurfaceForm.
tags: [capture, hid, mouse, state-machine]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-eac1f50cc585fc40731e5594
    resource: repo://GestureSign.Common/Input/CaptureState.cs
  - id: openwiki-source-816e133919eefa077450d747
    resource: repo://GestureSign.Common/Input/Devices.cs
  - id: openwiki-source-796a689b4606b05332e31f2a
    resource: repo://GestureSign.Daemon/Filtration/PointerInputTargetWindow.cs
  - id: openwiki-source-5467a0a77f93db372cc439a4
    resource: repo://GestureSign.Daemon/Input/InputProvider.cs
  - id: openwiki-source-548934ef6dc1c13ac8271fba
    resource: repo://GestureSign.Daemon/Input/PointCapture.cs
  - id: openwiki-source-23eb7715017b3e2f62055694
    resource: repo://GestureSign.Daemon/Input/PointEventTranslator.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Input capture

Capture lives only in the daemon. `PointCapture` constructs `InputProvider` + `PointEventTranslator` in its constructor; `Load()` is a no-op used for singleton side effects.

## Sources

`InputProvider` always creates `MessageWindow` (Raw Input HID). `Devices` flags: `TouchScreen`, `TouchPad`, `Mouse`, `Pen`; `TouchDevice` is screen|pad.

`PointEventTranslator` maps:

- HID `PointsIntercepted` → down/move/up with a source device. It refuses mixing sources except allowing Pen to take over.
- `LowLevelMouseHook` when `DrawingButton` matches: synthetic contact id `1`, `Devices.Mouse`. The hook can mark the message `Handled`.

Session lock sets `CaptureState.Disabled`; unlock/logon/remote connect returns `Ready`. Power resume and config change re-enumerate devices.

## State machine

| State | Meaning |
| --- | --- |
| `Ready` | Idle; PointDown may start a gesture |
| `CapturingInvalid` | Down accepted but stroke not yet long enough (`MinimumPointDistance`) |
| `Capturing` | Recording points |
| `Disabled` | Session lock, or mouse click-through after invalid stroke |
| `TriggerFired` | A non-stroke trigger consumed the interaction |

**PointDown** (Ready / Capturing / CapturingInvalid): raise process priority, arm `InitialTimeout`. `TryBeginCapture` fires `CaptureStarted` (application matching may `Cancel` or set `BlockTouchInputThreshold`). If not cancelled, state becomes `CapturingInvalid` and the first points are stored (optionally ordered by X if `IsOrderByLocation`). `Handled` is set unless `UserDisabled`.

**PointMove**: append if capturing; skip samples closer than `MinimumPointDistance`; first accepted move promotes Invalid → Capturing. `PointCaptured` fires.

**PointUp**: `EndCapture` → `CaptureEnded`, state `Ready`, `BeforePointsCaptured` (gesture name). Training (not a single tap) sends `GotGesture` to Control Panel. If `GestureName` is set, `GestureRecognized`. Then `AfterPointsCaptured`.

Invalid **mouse** up synthesizes a button click via `InputSimulator` so the original click still happens. `InitialTimeout` can inject mouse down or temporarily disable pointer blocking if the user never moved enough.

`UserDisabled` still records for matching in some paths but does not mark HID/mouse as handled (except after a trigger). `TemporarilyDisableCapture` restores Normal after the next up.

## Feedback and filtration

`SurfaceForm` is a layered alpha window that draws the stroke with `AppConfig` pen color/width.

With `UiAccess`, `PointerInputTargetWindow` receives `BlockTouchInputThreshold` (debounced 100 ms). Values ≥ 2 register as a pointer target so touch is consumed. Foreground `UserApp` max threshold and CaptureStarted both update it; `UserDisabled` zeros it.

See [Windows input and windowing](../integrations/windows-apis.md) and [Recognition and execution](recognition-and-execution.md).
