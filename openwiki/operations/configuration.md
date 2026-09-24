---
type: operations
title: Configuration and persistence
description: Portable vs roaming AppData paths, GestureSign.config, Gestures.gest, Actions.gsa, FileManager backups, and Windows startup.
tags: [config, persistence, portable, startup]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-c39da0394d06b00f690f046e
    resource: repo://GestureSign.Common/Configuration/AppConfig.cs
  - id: openwiki-source-12e8f166799bc79315670989
    resource: repo://GestureSign.Common/Configuration/FileManager.cs
  - id: openwiki-source-1e2714647cf248fd3947a707
    resource: repo://GestureSign.Common/Constants.cs
  - id: openwiki-source-ed8bdb213bb042669a958efc
    resource: repo://GestureSign.ControlPanel/Common/StartupHelper.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Configuration and persistence

## Paths

`AppConfig` static constructor sets directories from compile symbols:

- **Portable** (`#if Portable` on Common): `ApplicationDataPath` and `LocalApplicationDataPath` are `{exe folder}\AppData`. Config and backups live there.
- **Installed**: roaming `%AppData%\GestureSign` for config and gesture/action files; local `%LocalAppData%\GestureSign` for logs and `Backup`.

`ConfigPath` is always `{ApplicationDataPath}\GestureSign.config`. `BackupPath` is `{LocalApplicationDataPath}\Backup`. Directories are created on first access; create failures become `FileWriteException`.

## GestureSign.config

Settings are `appSettings` in a mapped `ExeConfigurationFileMap`. `GetValue`/`SetValue` cache in `_settingCache`. Writes debounce 100 ms on a timer, `WaitFile` the path, `Save(ConfigurationSaveMode.Modified)`, then `ConfigChanged`. `Reload` sets `_loadFlag`, clears cache, and raises `ConfigChanged` (daemon IPC uses this). Save errors also `Reload` and notice a `FileWriteException`.

Notable keys include visual feedback, drawing mouse button, pen buttons, tray visibility, culture, `RunAsAdmin`, `SendErrorReport`, `IgnoreFullScreen`, and last error time.

## Gestures and actions

| File | Constant | Content |
| --- | --- | --- |
| `Gestures.gest` | `GesturesFileName` | Named point patterns |
| `Actions.gsa` | `ActionFileName` | Apps, actions, commands (JSON type names) |
| `*.gest` in Backup | | Timestamped copies from `FileManager` |
| `GestureSign.ges` | `ArchivesName` | Export archive extension |
| `*.gsb` | `BackupFileExtension` | Options UI full backup |

`FileManager.SaveObject` copies the existing file into `Backup\yyMMddHHmmss` + extension, waits up to ~500 ms if locked (sharing violation 32/33), then serializes JSON. Load can optionally backup a corrupt file. Gesture/action loaders also try `Defaults\` next to the exe.

## Startup

`StartupHelper` can:

- Create a Startup-folder `.lnk` to `GestureSign.exe`.
- Register/delete scheduled task `StartGestureSign` via `schtasks.exe /create` with `runas` and an XML template (used when `RunAsAdmin`).
- Query Store `StartupTask` `"GestureSignTask"` for packaged builds.

See [Control panel](../architecture/control-panel.md) and [Privileges and uiAccess](privileges.md).
