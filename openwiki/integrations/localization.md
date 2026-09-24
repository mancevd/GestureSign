---
type: integration
title: Localization and logging
description: XML language packs with English resource fallback, plugin assembly strings, and GestureSign.log plus optional crash feedback.
tags: [localization, logging, errors]
verified:
  - by: openwiki/0.6.0
    at: 2026-09-24T12:53:26.620Z
sources:
  - id: openwiki-source-7330d1b96eabb73e68a0089f
    resource: repo://GestureSign.Common/Localization/LocalizationProvider.cs
  - id: openwiki-source-7a55b7ee533af91f05026f84
    resource: repo://GestureSign.Common/Log/Logging.cs
  - id: openwiki-source-e9423083c49453fdfef1670f
    resource: repo://GestureSign.Common/Plugins/PluginManager.cs
  - id: openwiki-source-14f15b71d176f6cb46739ce4
    resource: repo://GestureSign.ControlPanel/App.xaml.cs
  - id: openwiki-source-9f61a19232d4f80c701d5b3c
    resource: repo://GestureSign.ControlPanel/Log/Feedback.cs
  - id: openwiki-source-eb463386b4ca7b3ae25c788c
    resource: repo://GestureSign.ControlPanel/MainWindow.xaml.cs
  - id: openwiki-source-f2aebf9e5a1384f07a81ba06
    resource: repo://GestureSign.Daemon/Program.cs
generated: { by: "cursor", at: "2026-09-24T12:53:26.620Z" }
---

# Localization and logging

## Language packs

`LocalizationProvider` is a singleton. Culture is `AppConfig.CultureName` if set, else `CultureInfo.CurrentUICulture` (invalid culture names fall back to current UI culture).

`LoadFromFile(folder)` looks under `Languages\{folder}\*.xml` for a `<language Culture="...">` matching `_cultureInfo.Name`. Keys are dotted XML element paths; values are text nodes. The calling assembly’s name is added to `_assemblyNameList`.

Daemon starts with `LoadFromFile("Daemon")` then `LoadFromResource(Properties.Resources.en)` if the file load fails. Control Panel uses `"ControlPanel"` the same way, then applies `LocalizationProviderEx` flow direction and fonts to WPF resources.

`GetTextValue` looks in the dictionary, then loads embedded XML from registered assemblies for the two-letter culture, then `"en"`, else `""`. `PluginManager` calls `AddAssembly` for each plugin DLL so core and extra plugin strings resolve the same way. Extra plugins ship `en.xml` / `zh.xml` as content.

WPF markup uses `LocalisedText` markup extension.

## Logging

`Logging.OpenLogFile` appends to `LocalApplicationDataPath\GestureSign.log` via a timestamped `StreamWriter` assigned to `Console.Out` and `Console.Error`. Files larger than 100 KiB are deleted before append. `ObjectDisposedException` is not written. `LogAndNotice` logs then raises `LoggedExceptionOccurred` (both processes show a message box; Control Panel appends a localized note for `FileWriteException`).

## Error reports

If `AppConfig.SendErrorReport` is true, Control Panel `MainWindow` scans the Windows Application Event Log for recent `.NET Runtime` errors mentioning GestureSign. The user can export diagnostics (`Feedback.OutputLog`) and send them through SharpRaven (`Feedback.Send`). Failed send offers retry or cancel.

See [Control panel](../architecture/control-panel.md) and [Configuration and persistence](../operations/configuration.md).
