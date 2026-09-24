---
type: concept
title: Gestures
description: Named multi-stroke point patterns matched by angular probability, optionally stacked, stored in Gestures.gest.
tags: [gestures, matching, persistence]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-1e2714647cf248fd3947a707
    resource: repo://GestureSign.Common/Constants.cs
  - id: openwiki-source-cb3cc356a46db893c7979fcb
    resource: repo://GestureSign.Common/Gestures/Gesture.cs
  - id: openwiki-source-8502aeeb8a0f093c3dbfda82
    resource: repo://GestureSign.Common/Gestures/GestureManager.cs
  - id: openwiki-source-934304fc247d4c7443a85114
    resource: repo://GestureSign.Common/Gestures/PointPattern.cs
  - id: openwiki-source-46328d3a25d2fbc0d4dac3f3
    resource: repo://GestureSign.PointPatterns/PointPatternAnalyzer.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Gestures

A **gesture** is a named array of `PointPattern`s. Each pattern is `Point[][]`: one array per simultaneous contact (finger), each contact a sequence of screen points. Extra patterns on the same `IGesture` are **stacked** strokes that must follow within a timeout.

## Matching

`GestureManager` owns a `PointPatternAnalyzer`. On `BeforePointsCaptured` it sets `GestureName` from `GetGestureSetNameMatch`.

Candidates must have a pattern at the current `_gestureLevel` with the **same number of contacts** as the capture. Each contact is compared independently: the analyzer interpolates both paths to `Precision` (100) angular samples, averages angular delta, and converts that to a probability. A candidate is kept only if **every** contact scores **greater than 80** (`ProbabilityThreshold`).

Among remaining candidates:

- If the stored gesture still has a later `PointPatterns` index, it goes into `_gestureMatchResult` (stack continues). `_gestureLevel` increments and `_lastGestureTime` is set.
- Otherwise the **name with the highest summed probability** becomes `GestureName`.

If nothing remains, level and stack reset. `CaptureStarted` resets the stack when more than **800 ms** (`GestureStackTimeout`) elapsed since the last matching stroke.

In **Training** mode, level and stack are cleared each capture so teaching records a single stroke set rather than continuing a live stack.

Single-point vs multi-point length mismatch scores 0; equal length-1 patterns score 100.

## Persistence

Gestures load from `ApplicationDataPath\Gestures.gest`. Parse is a custom JSON walk (name plus nested point arrays as `"x,y"` strings), not the generic `FileManager` deserializer. Failure falls back to `Backup\*.gest` newest-first, then `Defaults\Gestures.gest` next to the exe, else an empty list.

`SaveGestures` uses `FileManager.SaveObject` and raises `GestureSaved` so the Control Panel can IPC-reload the daemon.

See [Recognition and execution](../workflows/recognition-and-execution.md) and [Teaching and editing](../workflows/teaching.md).
