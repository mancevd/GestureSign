# CLAUDE.md - GestureSign Development Guide for AI Assistants

**Last Updated:** 2025-11-17
**Version:** 8.1
**Purpose:** Comprehensive guide for AI assistants working on the GestureSign codebase

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture & Components](#architecture--components)
3. [Technology Stack](#technology-stack)
4. [Project Structure](#project-structure)
5. [Development Setup](#development-setup)
6. [Code Conventions](#code-conventions)
7. [Key Design Patterns](#key-design-patterns)
8. [Common Development Tasks](#common-development-tasks)
9. [Plugin Development](#plugin-development)
10. [Build Configurations](#build-configurations)
11. [Data Persistence](#data-persistence)
12. [Inter-Process Communication](#inter-process-communication)
13. [Git Workflow](#git-workflow)
14. [Important Files & Directories](#important-files--directories)
15. [Testing & Debugging](#testing--debugging)
16. [Troubleshooting](#troubleshooting)

---

## Project Overview

**GestureSign** is a gesture recognition software for Windows tablets and touch-enabled devices. It allows users to automate repetitive tasks by drawing gestures with fingers, pen, or mouse.

### Core Capabilities
- Multi-touch gesture recognition using pattern matching algorithms
- Application-specific gesture mapping (different gestures per app)
- Extensible plugin architecture for actions (keyboard, mouse, window control, etc.)
- Global input capture via Raw Input API
- Background service with UI configuration panel
- Multi-language support

### Key Features
- Activate Window, Window Control, Touch Keyboard Control
- Keyboard/Mouse simulation
- Screen brightness and volume adjustment
- Run commands/programs, Launch Windows Store apps
- Toggle window topmost, Send messages

---

## Architecture & Components

GestureSign follows a modular architecture with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────┐
│                    USER INTERACTION                      │
└─────────────────────┬───────────────────────────────────┘
                      │
        ┌─────────────┴─────────────┐
        │                           │
┌───────▼────────┐         ┌────────▼──────────┐
│  ControlPanel  │◄───IPC──►│     Daemon        │
│   (WPF UI)     │  Named   │ (Background Svc)  │
│                │   Pipe   │                   │
└───────┬────────┘         └────────┬───────────┘
        │                           │
        │ JSON Files                │
        ▼                           ▼
  ┌──────────┐            ┌──────────────────┐
  │ .gest    │            │  Input Capture   │
  │ .gsa     │            │  (Raw Input API) │
  └──────────┘            └────────┬─────────┘
                                   │
                    ┌──────────────┴──────────────┐
                    │                             │
            ┌───────▼────────┐         ┌─────────▼─────────┐
            │ Point Pattern  │         │   Plugin System   │
            │   Analyzer     │         │  (Action Exec)    │
            └────────────────┘         └───────────────────┘
```

### Component Breakdown

#### 1. **GestureSign.Daemon** (Background Service)
- **Purpose:** Runs in background, captures input, recognizes gestures, executes actions
- **Key Classes:**
  - `PointCapture` - Singleton managing all input capture
  - `InputProvider` - Manages raw input devices (touch, pen, touchpad)
  - `TriggerManager` - Manages gesture triggers and execution
  - `MessageProcessor` - Handles IPC messages from ControlPanel
  - Device handlers: `TouchScreenDevice`, `TouchPadDevice`, `PenDevice`, `HidDevice`
- **Entry Point:** `Program.cs`
- **Output:** `GestureSign.exe` (the main daemon executable)

#### 2. **GestureSign.ControlPanel** (Configuration UI)
- **Purpose:** WPF application for configuring gestures and applications
- **Architecture:** MVVM (Model-View-ViewModel)
- **Key Components:**
  - `MainWindow.xaml` - Primary interface with tabs
  - ViewModels: `ApplicationItemProvider`, `GestureItemProvider`
  - Dialogs: `GestureDefinition`, `ActionDialog`, `ApplicationDialog`
  - Uses MahApps.Metro for modern UI theming
- **Entry Point:** `App.xaml.cs`
- **Output:** `GestureSign.ControlPanel.exe`

#### 3. **GestureSign.Common** (Shared Library)
- **Purpose:** Core interfaces, models, and utilities shared across projects
- **Key Namespaces:**
  - `Applications/` - Application matching logic (`IApplication`, `ApplicationManager`)
  - `Gestures/` - Gesture definitions (`Gesture`, `GestureManager`)
  - `Input/` - Input event handling (`IPointCapture`, `RawData`)
  - `Plugins/` - Plugin architecture (`IPlugin`, `PluginManager`)
  - `InterProcessCommunication/` - Named pipe IPC (`NamedPipe`, `IpcCommands`)
  - `Configuration/` - File persistence (`FileManager`, `AppConfig`)
  - `Localization/` - Multi-language support
- **Output:** `GestureSign.Common.dll`

#### 4. **GestureSign.PointPatterns** (Gesture Recognition)
- **Purpose:** Pattern matching and gesture recognition algorithms
- **Key Classes:**
  - `PointPatternAnalyzer` - Core recognition engine
  - `PointPatternMath` - Mathematical operations on point sets
  - `PointsPatternSet` - Container for multiple pattern variations
  - `PointPatternMatchResult` - Match result with confidence score
- **Output:** `GestureSign.PointPatterns.dll`

#### 5. **GestureSign.CorePlugins** (Action Plugins)
- **Purpose:** Built-in action plugins (what gestures can do)
- **Plugins (14 total):**
  1. `ActivateWindow` - Brings window to foreground (regex pattern matching)
  2. `HotKey` - Sends keyboard shortcuts
  3. `KeyDownKeyUp` - Press/release specific keys
  4. `MouseActions` - Mouse clicks, movement
  5. `SendKeystrokes` - Type text
  6. `TouchKeyboard` - Control on-screen keyboard
  7. `Volume` - Adjust system volume
  8. `ScreenBrightness` - Control display brightness
  9. `LaunchApp` - Run executables/URLs
  10. `OpenFile` - Open files
  11. `RunCommand` - Execute shell commands
  12. `SendMessage` - Send window messages
  13. `Delay` - Add delays between actions
  14. Single-action plugins: `Minimize`, `Maximize/Restore`, `NextApplication`, `PreviousApplication`, `ToggleDisableGestures`, `ToggleWindowTopmost`, `TemporarilyDisable`
- **Output:** `GestureSign.CorePlugins.dll`

#### 6. **WindowsInput** (Input Simulation)
- **Purpose:** Wrapper library around Windows SendInput API
- **Key Classes:**
  - `InputSimulator` - Main facade
  - `KeyboardSimulator` - Type keys/text
  - `MouseSimulator` - Move mouse, click buttons
  - `InputBuilder` - Build input sequences
- **Output:** `WindowsInput.dll` (strong-named, signed)

#### 7. **ManagedWinapi** (Windows API Wrapper)
- **Purpose:** Managed wrappers for unmanaged Windows APIs
- **Components:**
  - `SystemWindow` - Window enumeration and manipulation
  - `LowLevelMouseHook` - Mouse input interception
- **Output:** `ManagedWinapi.dll`

#### 8. **GestureSign.ExtraPlugins**
- Extra plugins (not in core):
  - `ClipboardMatch` - Clipboard-based actions
  - `TextCopyer` - Text copying utilities

---

## Technology Stack

### Languages & Frameworks
- **Language:** C# (Primary)
- **Framework:** .NET Framework 4.6 / 4.6.1 (ControlPanel), 4.6 (Daemon), 4.5 (Common)
- **UI Framework:** WPF (Windows Presentation Foundation) with XAML

### NuGet Dependencies
| Package | Version | Usage |
|---------|---------|-------|
| **MahApps.Metro** | 1.4.3 | Modern WPF UI theme/controls (ControlPanel) |
| **Newtonsoft.Json** | 8.0.3 | JSON serialization/deserialization |
| **SharpRaven** | 2.2.0 | Sentry error reporting integration (ControlPanel) |

### Windows APIs
- **Raw Input API** - Touch/pointer input capture
- **HID (Human Interface Device)** - Device detection
- **Win Event Hooks** - Window tracking
- **User32 API** - Window manipulation
- **Keyboard Hooks** - Input interception
- **SendInput API** - Input simulation

### Build Tools
- **MSBuild** (Visual Studio 2012+)
- **ToolsVersion:** 12.0

---

## Project Structure

```
GestureSign/
├── .git/                          # Git repository
├── .gitignore                     # Git ignore rules
├── .gitattributes                 # Git attributes
├── LICENSE                        # Project license
├── README.md                      # Project readme
├── GestureSign.sln                # Visual Studio solution
│
├── bin/                           # Build output (not in repo)
│   ├── Debug/                     # Debug builds
│   ├── Release/                   # Release builds
│   ├── uiAccessRelease/           # Elevated privileges builds
│   ├── Centennial/                # UWP conversion builds
│   └── Portable/                  # Portable builds
│
├── GestureSign.Daemon/            # Background service project
│   ├── Input/                     # Input capture (Raw Input API)
│   │   ├── PointCapture.cs        # Main input capture singleton
│   │   ├── InputProvider.cs       # Device management
│   │   ├── TouchScreenDevice.cs   # Touch screen handler
│   │   ├── TouchPadDevice.cs      # Touchpad handler
│   │   ├── PenDevice.cs           # Pen/stylus handler
│   │   └── HidDevice.cs           # Generic HID device
│   ├── Triggers/                  # Gesture trigger management
│   │   └── TriggerManager.cs      # Executes gestures
│   ├── Surface/                   # Gesture visualization
│   │   └── SurfaceForm.cs         # Shows gesture drawing
│   ├── MessageProcessor.cs        # IPC message handler
│   ├── Program.cs                 # Entry point
│   └── Properties/
│       └── app.manifest           # UAC elevation manifest
│
├── GestureSign.ControlPanel/      # WPF UI project (MVVM)
│   ├── MainWindow.xaml            # Main window
│   ├── MainWindowControls/        # Tab content controls
│   ├── Dialogs/                   # Modal dialogs
│   │   ├── GestureDefinition.xaml # Draw gestures
│   │   ├── ActionDialog.xaml      # Configure actions
│   │   └── ApplicationDialog.xaml # Add/edit apps
│   ├── ViewModel/                 # View models
│   │   ├── GestureItemProvider.cs
│   │   └── ApplicationItemProvider.cs
│   ├── Converters/                # XAML value converters
│   ├── Localization/              # Language support
│   ├── Languages/                 # Language XML files
│   ├── Resources/                 # Images, icons
│   └── App.xaml                   # Application entry
│
├── GestureSign.Common/            # Shared library
│   ├── Applications/              # App matching logic
│   │   ├── IApplication.cs
│   │   ├── ApplicationBase.cs
│   │   └── ApplicationManager.cs
│   ├── Gestures/                  # Gesture definitions
│   │   ├── Gesture.cs
│   │   ├── GestureManager.cs
│   │   └── PointPattern.cs
│   ├── Input/                     # Input interfaces
│   │   ├── IPointCapture.cs
│   │   └── RawData.cs
│   ├── Plugins/                   # Plugin system
│   │   ├── IPlugin.cs
│   │   ├── PluginManager.cs
│   │   └── HostControl.cs
│   ├── InterProcessCommunication/ # IPC (Named Pipes)
│   │   ├── NamedPipe.cs
│   │   └── IpcCommands.cs
│   ├── Configuration/             # File I/O
│   │   ├── FileManager.cs         # JSON save/load
│   │   └── AppConfig.cs
│   ├── Localization/
│   │   └── LocalizationProvider.cs
│   └── Log/                       # Logging
│       └── Logging.cs
│
├── GestureSign.PointPatterns/     # Pattern recognition
│   ├── PointPatternAnalyzer.cs    # Core recognition engine
│   ├── PointPatternMath.cs        # Math operations
│   └── PointsPatternSet.cs        # Pattern container
│
├── GestureSign.CorePlugins/       # Built-in action plugins
│   ├── ActivateWindow/
│   │   ├── ActivateWindowPlugin.cs
│   │   └── ActivateWindowUI.xaml
│   ├── HotKey/
│   ├── MouseActions/
│   ├── SendKeystrokes/
│   ├── Volume/
│   ├── ScreenBrightness/
│   ├── LaunchApp/
│   ├── RunCommand/
│   ├── TouchKeyboard/
│   └── [... other plugins ...]
│
├── WindowsInput/                  # Input simulation library
│   ├── InputSimulator.cs
│   ├── KeyboardSimulator.cs
│   ├── MouseSimulator.cs
│   └── Native/                    # P/Invoke declarations
│
├── ManagedWinapi/                 # Windows API wrappers
│   ├── Windows/
│   │   └── SystemWindow.cs
│   └── Hooks/
│       └── LowLevelMouseHook.cs
│
└── GestureSign.ExtraPlugins/      # Extra plugins
    ├── ClipboardMatch/
    └── TextCopyer/
```

---

## Development Setup

### Prerequisites
- **Visual Studio 2012+** (recommended: VS 2015 or later)
- **.NET Framework 4.6 SDK**
- **Windows OS** (required for Windows-specific APIs)

### Getting Started
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd GestureSign
   ```

2. Open solution:
   ```bash
   # Open in Visual Studio
   start GestureSign.sln
   ```

3. Restore NuGet packages:
   - Visual Studio will auto-restore packages
   - Or manually: `nuget restore GestureSign.sln`

4. Build solution:
   - Build → Build Solution (Ctrl+Shift+B)
   - Or: `msbuild GestureSign.sln /p:Configuration=Debug`

5. Run/Debug:
   - Set `GestureSign.Daemon` or `GestureSign.ControlPanel` as startup project
   - Press F5 to debug

### Build Output Locations
- **Debug:** `bin/Debug/`
- **Release:** `bin/Release/`
- **uiAccessRelease:** `bin/uiAccessRelease/` (elevated UI access)
- **Centennial:** `bin/Centennial/` (Windows Store conversion)
- **Portable:** `bin/Portable/` (portable version)

---

## Code Conventions

### Naming Conventions

#### Interfaces
- **Prefix with `I`**
- Examples: `IPlugin`, `IApplication`, `IPointCapture`, `IDevice`

#### Classes
- **PascalCase** for class names
- Examples: `PointCapture`, `GestureManager`, `PluginManager`

#### Methods
- **PascalCase** for public methods
- **Verb-based naming** for actions
- Examples: `LoadGestures()`, `SaveConfiguration()`, `AnalyzePoints()`

#### Properties
- **PascalCase**
- Examples: `GestureName`, `IsEnabled`, `PointPatterns`

#### Events
- **PascalCase with descriptive names**
- Pattern: `<Noun><Verb>` or `<Noun><Verb>ed/ing`
- Examples: `GestureSaved`, `ApplicationChanged`, `PointsCaptured`

#### Private Fields
- **camelCase with underscore prefix** (common pattern in this codebase)
- Examples: `_pointCapture`, `_gestureManager`, `_namedPipeServer`

#### Constants
- **PascalCase** or **ALL_CAPS** (depending on context)
- Examples: `DefaultTimeout`, `MAX_RETRY_COUNT`

### Namespace Conventions
- **Hierarchical by project and feature**
- Pattern: `GestureSign.<Project>.<Feature>`
- Examples:
  - `GestureSign.Common.Applications`
  - `GestureSign.Common.Gestures`
  - `GestureSign.CorePlugins.ActivateWindow`
  - `GestureSign.Daemon.Input`

### File Organization
- **One class per file** (preferred)
- **File name matches class name**
- **Organize by feature/namespace** in directory structure

### Comments & Documentation
- **XML documentation** for public APIs (especially in WindowsInput)
- **Inline comments** for complex logic
- **TODO comments** for future work
- Example:
  ```csharp
  /// <summary>
  /// Analyzes point patterns to recognize gestures.
  /// </summary>
  /// <param name="points">The captured points to analyze</param>
  /// <returns>Match result with confidence score</returns>
  public PointPatternMatchResult AnalyzePoints(List<Point> points)
  {
      // Implementation...
  }
  ```

### Error Handling
- **Try-catch with logging** for expected errors
- **Use `Logging` static class** for centralized logging
- **Log exceptions** before rethrowing
- Example:
  ```csharp
  try
  {
      // Risky operation
  }
  catch (Exception ex)
  {
      Logging.LogException(ex);
      throw; // or handle gracefully
  }
  ```

### Async/Await
- **Use async/await** for I/O operations (file, IPC)
- **Suffix async methods** with `Async`
- Examples: `SendMessageAsync()`, `GetMessageAsync()`, `LoadGesturesAsync()`

---

## Key Design Patterns

### 1. Singleton Pattern
**Usage:** Single instance managers throughout application lifetime

**Examples:**
```csharp
// Input capture singleton
PointCapture.Instance.CaptureStarted += OnCaptureStarted;

// Gesture management singleton
GestureManager.Instance.LoadGestures("gestures.gest");

// Plugin management singleton
PluginManager.Instance.LoadPlugins();

// Application management singleton
ApplicationManager.Instance.AddApplication(app);
```

**Implementation Pattern:**
```csharp
public class PointCapture
{
    private static PointCapture _instance;
    public static PointCapture Instance => _instance ?? (_instance = new PointCapture());

    private PointCapture() { } // Private constructor
}
```

### 2. Observer/Event-Driven Pattern
**Usage:** Loose coupling between components

**Examples:**
```csharp
// Gesture events
GestureManager.GestureSaved += OnGestureSaved;
GestureManager.GestureChanged += OnGestureChanged;

// Application events
ApplicationManager.ApplicationSaved += OnApplicationSaved;

// Input events
PointCapture.Instance.PointsCaptured += OnPointsCaptured;
PointCapture.Instance.CaptureEnded += OnCaptureEnded;
```

### 3. Plugin/Factory Pattern
**Usage:** Extensible action system

**Plugin Interface:**
```csharp
public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    ImageSource Icon { get; }
    bool IsAction { get; }
    IHostControl HostControl { get; set; }

    void Initialize();
    bool Deserialize(string serializedData);
    string Serialize();
    void ShowGUI(bool hostIsGestureWindow);
}
```

**Loading Plugins:**
```csharp
PluginManager.Instance.LoadPlugins();
var availablePlugins = PluginManager.Instance.Plugins;
```

### 4. Manager Pattern
**Usage:** Centralized management of domain entities

**Managers:**
- `GestureManager` - Manages gesture definitions
- `ApplicationManager` - Manages application configurations
- `PluginManager` - Manages plugin lifecycle
- `TriggerManager` - Manages gesture trigger execution

**Pattern:**
```csharp
public class GestureManager
{
    // Singleton instance
    public static GestureManager Instance { get; }

    // Domain collection
    public List<Gesture> Gestures { get; }

    // CRUD operations
    public void AddGesture(Gesture gesture) { }
    public void RemoveGesture(Gesture gesture) { }
    public void SaveGestures(string filePath) { }
    public void LoadGestures(string filePath) { }

    // Events
    public event EventHandler GestureSaved;
    public event EventHandler GestureChanged;
}
```

### 5. Inter-Process Communication (IPC)
**Usage:** Communication between Daemon and ControlPanel

**Architecture:**
- **Named Pipes** for bidirectional communication
- **Async messaging** with `SendMessageAsync()`
- **Command-based** protocol (`IpcCommands` enum)

**Example:**
```csharp
// In ControlPanel - send command to Daemon
NamedPipe.SendMessageAsync(IpcCommands.ReloadGestures);

// In Daemon - process commands
MessageProcessor.ProcessMessage(message);
```

### 6. MVVM (Model-View-ViewModel)
**Usage:** ControlPanel UI architecture

**Structure:**
- **Models:** Domain classes from Common (Gesture, Application, etc.)
- **ViewModels:** `GestureItemProvider`, `ApplicationItemProvider`
- **Views:** XAML files with data binding

**Example:**
```xml
<!-- View (XAML) -->
<ListBox ItemsSource="{Binding Gestures}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock Text="{Binding Name}" />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

### 7. Strategy Pattern
**Usage:** Pluggable behaviors (different device handlers)

**Example:**
```csharp
// Different strategies for different device types
IDevice device;
if (isTouchScreen)
    device = new TouchScreenDevice();
else if (isTouchPad)
    device = new TouchPadDevice();
else if (isPen)
    device = new PenDevice();

device.StartCapture();
```

### 8. Facade Pattern
**Usage:** Simplified API for complex subsystems

**Examples:**
```csharp
// InputSimulator facade
InputSimulator sim = new InputSimulator();
sim.Keyboard.TextEntry("Hello World");
sim.Mouse.LeftButtonClick();

// NamedPipe facade
NamedPipe.SendMessageAsync(command, data);
```

---

## Common Development Tasks

### Adding a New Plugin

1. **Create plugin folder and class:**
   ```
   GestureSign.CorePlugins/
   └── MyNewPlugin/
       ├── MyNewPlugin.cs
       └── MyNewPluginUI.xaml
   ```

2. **Implement IPlugin interface:**
   ```csharp
   namespace GestureSign.CorePlugins.MyNewPlugin
   {
       public class MyNewPlugin : IPlugin
       {
           public string Name => "My New Plugin";
           public string Description => "Does something useful";
           public ImageSource Icon => /* Load icon */;
           public bool IsAction => true;
           public IHostControl HostControl { get; set; }

           public void Initialize() { }

           public bool Deserialize(string serializedData)
           {
               // Deserialize settings from JSON
               return true;
           }

           public string Serialize()
           {
               // Serialize settings to JSON
               return JsonConvert.SerializeObject(settings);
           }

           public void ShowGUI(bool hostIsGestureWindow)
           {
               // Show UI for configuration
               var ui = new MyNewPluginUI();
               ui.ShowDialog();
           }
       }
   }
   ```

3. **Create XAML UI (if needed):**
   ```xml
   <UserControl x:Class="GestureSign.CorePlugins.MyNewPlugin.MyNewPluginUI"
                xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
       <!-- Configuration UI -->
   </UserControl>
   ```

4. **Add to project file:**
   - Edit `GestureSign.CorePlugins.csproj`
   - Add `<Compile Include="MyNewPlugin\MyNewPlugin.cs" />`
   - Add `<Page Include="MyNewPlugin\MyNewPluginUI.xaml" />`

5. **Rebuild and test:**
   - Plugin will auto-load via `PluginManager`
   - Test in ControlPanel → Available Actions

### Adding Application Matching Logic

1. **Extend ApplicationBase:**
   ```csharp
   public class CustomApplication : ApplicationBase
   {
       public override bool MatchUsing { get; set; } = MatchUsing.ExecutableFilename;

       public override bool IsMatched(string title, string className, string fileName)
       {
           // Custom matching logic
           if (MatchUsing == MatchUsing.ExecutableFilename)
               return fileName.Equals(MatchString, StringComparison.OrdinalIgnoreCase);

           // ... other matching logic
           return false;
       }
   }
   ```

2. **Register in ApplicationManager:**
   ```csharp
   ApplicationManager.Instance.AddApplication(customApp);
   ```

### Modifying Gesture Recognition

1. **Understand PointPatternAnalyzer:**
   - Located: `GestureSign.PointPatterns/PointPatternAnalyzer.cs`
   - Core method: `GetPointPatternMatchResult()`

2. **Adjust recognition parameters:**
   - Modify comparison thresholds
   - Adjust normalization algorithms
   - Tune match confidence scoring

3. **Test extensively:**
   - Draw various gestures
   - Check false positives/negatives
   - Adjust parameters iteratively

### Adding IPC Commands

1. **Add to IpcCommands enum:**
   ```csharp
   // In GestureSign.Common/InterProcessCommunication/IpcCommands.cs
   public enum IpcCommands
   {
       // ... existing commands
       MyNewCommand,
   }
   ```

2. **Handle in MessageProcessor:**
   ```csharp
   // In GestureSign.Daemon/MessageProcessor.cs
   switch (command)
   {
       case IpcCommands.MyNewCommand:
           HandleMyNewCommand(data);
           break;
       // ... other cases
   }
   ```

3. **Send from ControlPanel:**
   ```csharp
   NamedPipe.SendMessageAsync(IpcCommands.MyNewCommand, "data");
   ```

### Adding Localization

1. **Add language XML file:**
   ```xml
   <!-- GestureSign.ControlPanel/Languages/MyLanguage.xml -->
   <Language Culture="my-MY" DisplayName="My Language">
       <ResourceDictionary>
           <system:String x:Key="KeyName">Translated Text</system:String>
           <!-- ... more translations -->
       </ResourceDictionary>
   </Language>
   ```

2. **Reference in code:**
   ```csharp
   string translated = LocalizationProvider.Instance.GetTextValue("KeyName");
   ```

3. **Or in XAML:**
   ```xml
   <TextBlock Text="{Binding Path=LocalizedStrings.KeyName,
                             Source={StaticResource LocalizationProvider}}" />
   ```

### Debugging Input Capture

1. **Enable detailed logging:**
   ```csharp
   Logging.LogLevel = LogLevel.Debug;
   ```

2. **Check input events:**
   - Set breakpoints in `PointCapture.cs`
   - Monitor `InputProvider.ProcessInputMessage()`
   - Inspect `RawData` objects

3. **Visualize gestures:**
   - Use `SurfaceForm` to see gesture drawing
   - Check point coordinates and timing

---

## Plugin Development

### Plugin Lifecycle

1. **Discovery:** `PluginManager.LoadPlugins()` scans assemblies
2. **Instantiation:** Plugins instantiated via reflection
3. **Initialization:** `Initialize()` called
4. **Configuration:** User edits settings via `ShowGUI()`
5. **Serialization:** Settings saved via `Serialize()`
6. **Execution:** Plugin executes action when gesture triggered
7. **Deserialization:** Settings loaded via `Deserialize()` on startup

### Plugin Architecture

```csharp
public interface IPlugin
{
    // Metadata
    string Name { get; }
    string Description { get; }
    ImageSource Icon { get; }
    bool IsAction { get; }

    // Lifecycle
    void Initialize();

    // Serialization
    string Serialize();           // Save settings to JSON
    bool Deserialize(string data); // Load settings from JSON

    // UI
    void ShowGUI(bool hostIsGestureWindow);

    // Host communication
    IHostControl HostControl { get; set; }
}
```

### Plugin Best Practices

#### 1. **Settings Serialization**
```csharp
private MySettings _settings = new MySettings();

public string Serialize()
{
    return JsonConvert.SerializeObject(_settings,
        new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            NullValueHandling = NullValueHandling.Ignore
        });
}

public bool Deserialize(string serializedData)
{
    if (string.IsNullOrWhiteSpace(serializedData))
        return false;

    try
    {
        _settings = JsonConvert.DeserializeObject<MySettings>(serializedData);
        return true;
    }
    catch
    {
        return false;
    }
}
```

#### 2. **Error Handling**
```csharp
public void ExecuteAction()
{
    try
    {
        // Action implementation
    }
    catch (Exception ex)
    {
        Logging.LogException(ex);
        // Optionally notify user via HostControl
        HostControl?.ShowNotification("Error", ex.Message);
    }
}
```

#### 3. **Resource Management**
```csharp
public class MyPlugin : IPlugin, IDisposable
{
    private Timer _timer;

    public void Initialize()
    {
        _timer = new Timer();
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
```

#### 4. **Localization Support**
```csharp
public string Name => LocalizationProvider.Instance.GetTextValue("MyPlugin.Name");
public string Description => LocalizationProvider.Instance.GetTextValue("MyPlugin.Description");
```

### Example Plugin: Simple Key Press

```csharp
using System;
using System.Windows.Media;
using GestureSign.Common.Plugins;
using WindowsInput;
using Newtonsoft.Json;

namespace GestureSign.CorePlugins.SimpleKeyPress
{
    public class SimpleKeyPressPlugin : IPlugin
    {
        private SimpleKeyPressSettings _settings = new SimpleKeyPressSettings();

        public string Name => "Simple Key Press";
        public string Description => "Simulates a single key press";
        public ImageSource Icon => /* Load from resources */;
        public bool IsAction => true;
        public IHostControl HostControl { get; set; }

        public void Initialize() { }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(_settings);
        }

        public bool Deserialize(string serializedData)
        {
            if (string.IsNullOrWhiteSpace(serializedData))
                return false;

            try
            {
                _settings = JsonConvert.DeserializeObject<SimpleKeyPressSettings>(serializedData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void ShowGUI(bool hostIsGestureWindow)
        {
            var ui = new SimpleKeyPressUI(_settings);
            if (ui.ShowDialog() == true)
            {
                _settings = ui.Settings;
            }
        }

        public void ExecuteAction()
        {
            var simulator = new InputSimulator();
            simulator.Keyboard.KeyPress(_settings.Key);
        }
    }

    public class SimpleKeyPressSettings
    {
        public VirtualKeyCode Key { get; set; } = VirtualKeyCode.SPACE;
    }
}
```

---

## Build Configurations

GestureSign supports multiple build configurations for different deployment scenarios:

### Configuration Comparison

| Configuration | Target | Purpose | Special Flags |
|---------------|--------|---------|---------------|
| **Debug** | `bin/Debug/` | Development debugging | `DEBUG;TRACE`, Full symbols |
| **Release** | `bin/Release/` | Standard production | `TRACE`, Optimized |
| **uiAccessRelease** | `bin/uiAccessRelease/` | Elevated UI access | `TRACE`, Special manifest |
| **Centennial** | `bin/Centennial/` | Windows Store (UWP) | `TRACE;ConvertedDesktopApp` |
| **Portable** | `bin/Portable/` | Portable version | Reduced dependencies |

### Building for Different Configurations

```bash
# Debug build
msbuild GestureSign.sln /p:Configuration=Debug

# Release build
msbuild GestureSign.sln /p:Configuration=Release

# uiAccessRelease build (elevated UI access)
msbuild GestureSign.sln /p:Configuration=uiAccessRelease

# Centennial build (Windows Store)
msbuild GestureSign.sln /p:Configuration=Centennial

# Portable build
msbuild GestureSign.sln /p:Configuration=Portable
```

### Configuration-Specific Code

Use conditional compilation:

```csharp
#if DEBUG
    Logging.LogLevel = LogLevel.Debug;
#endif

#if ConvertedDesktopApp
    // Windows Store specific code
#endif
```

### uiAccessRelease Special Considerations

- **Purpose:** Allows UI to interact with elevated applications
- **Manifest:** Modified `app.manifest` with `uiAccess="true"`
- **Signing:** Must be signed with trusted certificate
- **Installation:** Must be installed to Program Files or Windows\System32

---

## Data Persistence

### File Formats

#### Gesture Files (`.gest`)
**Format:** JSON
**Contains:** Array of gesture definitions with point patterns

**Structure:**
```json
[
  {
    "Name": "Circle",
    "PointPatterns": [
      {
        "Points": [
          { "X": 100, "Y": 100 },
          { "X": 105, "Y": 102 },
          ...
        ]
      }
    ]
  }
]
```

#### Action Files (`.gsa`)
**Format:** JSON
**Contains:** Application-to-gesture mappings with plugin settings

**Structure:**
```json
[
  {
    "Name": "Chrome",
    "MatchUsing": "ExecutableFilename",
    "MatchString": "chrome.exe",
    "Actions": [
      {
        "GestureName": "SwipeLeft",
        "PluginClass": "GestureSign.CorePlugins.HotKey.HotKeyPlugin",
        "PluginSettings": "{\"Keys\":[\"Control\",\"W\"]}"
      }
    ]
  }
]
```

### Storage Strategy

**FileManager** handles all file I/O with these features:

#### 1. **Atomic Writes with Backup**
```csharp
public static bool SaveObject<T>(T obj, string filePath)
{
    // Create backup of existing file
    if (File.Exists(filePath))
        File.Copy(filePath, filePath + ".bak", true);

    // Write to temporary file first
    string tempPath = filePath + ".tmp";
    File.WriteAllText(tempPath, JsonConvert.SerializeObject(obj));

    // Move temporary file to final location (atomic)
    File.Delete(filePath);
    File.Move(tempPath, filePath);

    return true;
}
```

#### 2. **File Locking Detection**
- Retries on `IOException`
- Configurable retry count and delay
- Graceful degradation

#### 3. **JSON Serialization Settings**
```csharp
var settings = new JsonSerializerSettings
{
    TypeNameHandling = TypeNameHandling.Auto,
    NullValueHandling = NullValueHandling.Ignore,
    Formatting = Formatting.Indented
};
```

### Configuration Locations

**Default paths** (via `AppConfig`):
- **Gestures:** `%APPDATA%\GestureSign\gestures.gest`
- **Actions:** `%APPDATA%\GestureSign\actions.gsa`
- **Applications:** `%APPDATA%\GestureSign\applications.gsa`
- **Logs:** `%LOCALAPPDATA%\GestureSign\logs\`
- **Backups:** `%APPDATA%\GestureSign\Backup\`

### Loading & Saving

```csharp
// Loading gestures
GestureManager.Instance.LoadGestures("gestures.gest");

// Saving gestures
GestureManager.Instance.SaveGestures("gestures.gest");

// Loading applications
ApplicationManager.Instance.LoadApplications("applications.gsa");

// Saving applications
ApplicationManager.Instance.SaveApplications("applications.gsa");
```

---

## Inter-Process Communication

### Architecture

**Named Pipes** enable bidirectional communication between:
- **Server:** Daemon (always running)
- **Client:** ControlPanel (runs on-demand)

### IPC Commands

```csharp
public enum IpcCommands
{
    // Configuration reload
    ReloadGestures,
    ReloadApplications,
    ReloadPlugins,

    // Runtime control
    StartCapture,
    StopCapture,
    EnableGestures,
    DisableGestures,

    // State queries
    GetStatus,
    GetDevices,

    // Notifications
    ShowNotification,
    SendLog,

    // System control
    Exit,
    Restart,
}
```

### Sending Messages

**From ControlPanel to Daemon:**
```csharp
// Simple command
await NamedPipe.SendMessageAsync(IpcCommands.ReloadGestures);

// Command with data
await NamedPipe.SendMessageAsync(IpcCommands.ShowNotification, "message");
```

### Receiving Messages

**In Daemon:**
```csharp
public class MessageProcessor
{
    public static void ProcessMessage(IpcCommands command, string data)
    {
        switch (command)
        {
            case IpcCommands.ReloadGestures:
                GestureManager.Instance.LoadGestures();
                break;

            case IpcCommands.ShowNotification:
                ShowToast(data);
                break;

            // ... other commands
        }
    }
}
```

### Connection Management

**Daemon side (server):**
```csharp
private NamedPipeServerStream _namedPipeServer;

void StartServer()
{
    _namedPipeServer = new NamedPipeServerStream("GestureSignPipe",
        PipeDirection.InOut, 1, PipeTransmissionMode.Message);

    _namedPipeServer.BeginWaitForConnection(OnClientConnected, null);
}

void OnClientConnected(IAsyncResult ar)
{
    _namedPipeServer.EndWaitForConnection(ar);
    // Read messages...
}
```

**ControlPanel side (client):**
```csharp
using (var client = new NamedPipeClientStream(".", "GestureSignPipe",
    PipeDirection.InOut))
{
    client.Connect(timeout);
    // Send message...
}
```

### Synchronization

**Async/Await Pattern:**
```csharp
public static async Task SendMessageAsync(IpcCommands command, string data = null)
{
    using (var client = new NamedPipeClientStream(".", "GestureSignPipe"))
    {
        await client.ConnectAsync(5000);

        var message = new IpcMessage { Command = command, Data = data };
        var json = JsonConvert.SerializeObject(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        await client.WriteAsync(bytes, 0, bytes.Length);
    }
}
```

### Error Handling

```csharp
try
{
    await NamedPipe.SendMessageAsync(command);
}
catch (TimeoutException)
{
    // Daemon not running or not responding
    Logging.LogException("IPC timeout - is Daemon running?");
}
catch (IOException ex)
{
    // Pipe disconnected
    Logging.LogException(ex);
}
```

---

## Git Workflow

### Commit Message Conventions

**Style:** Lowercase, concise descriptions (no periods)

**Format:**
```
<brief description of change>
```

**Examples:**
```
version 8.1
wait longer for GetMessageAsync
remove SimulateDoubleTap in PointerInputTargetWindow
add Run As Administrator At Startup option
check null for _namedPipeServer
lower SendLog dialog priority
allow ControlPanel to send commands to high privilege daemon
catch start TabTip.exe exceptions
check and show available devices
move log file to AppData\Local
create configuration directory on demand
```

### Commit Best Practices

1. **One logical change per commit**
   - Don't mix feature additions with bug fixes
   - Keep refactoring separate from functional changes

2. **Brief but descriptive**
   - Focus on WHAT changed, not WHY (use code comments for WHY)
   - Keep under 72 characters when possible

3. **Use present tense, imperative mood**
   - "add feature" not "added feature"
   - "fix bug" not "fixes bug"

4. **Group related changes**
   - If fixing a bug requires multiple file changes, commit together
   - Use `git add -p` for selective staging

### Branching Strategy

**Main branches:**
- `master` - Stable releases
- `develop` - Integration branch for features

**Feature branches:**
- Create from `develop`
- Naming: `feature/short-description` or `bugfix/issue-description`
- Merge back to `develop` via PR

**Release branches:**
- Create from `develop` when ready for release
- Naming: `release/vX.Y`
- Merge to both `master` and `develop`

### Example Workflow

```bash
# Start new feature
git checkout develop
git checkout -b feature/new-plugin
# ... make changes ...
git add .
git commit -m "add clipboard monitoring plugin"
git push origin feature/new-plugin

# Create PR: feature/new-plugin → develop

# After merge, update local
git checkout develop
git pull origin develop
git branch -d feature/new-plugin
```

---

## Important Files & Directories

### Root Level

| File/Directory | Purpose |
|----------------|---------|
| `GestureSign.sln` | Visual Studio solution file |
| `.gitignore` | Git ignore rules |
| `.gitattributes` | Git line ending configuration |
| `LICENSE` | Project license (MIT) |
| `README.md` | Project overview and features |

### Configuration Files

| File | Purpose |
|------|---------|
| `packages.config` | NuGet package dependencies |
| `App.config` | .NET runtime configuration |
| `Properties/Settings.settings` | Application settings |
| `Properties/app.manifest` | Windows manifest (UAC, DPI awareness) |

### Key Source Files

#### GestureSign.Daemon
| File | Purpose |
|------|---------|
| `Program.cs` | Entry point, application startup |
| `Input/PointCapture.cs` | **Critical** - Main input capture singleton |
| `Input/InputProvider.cs` | **Critical** - Device management |
| `Triggers/TriggerManager.cs` | **Critical** - Gesture execution |
| `MessageProcessor.cs` | IPC message handling |

#### GestureSign.ControlPanel
| File | Purpose |
|------|---------|
| `App.xaml.cs` | Application entry point |
| `MainWindow.xaml` | **Critical** - Main UI |
| `Dialogs/GestureDefinition.xaml` | Draw gestures UI |
| `Dialogs/ActionDialog.xaml` | Configure actions UI |

#### GestureSign.Common
| File | Purpose |
|------|---------|
| `Gestures/GestureManager.cs` | **Critical** - Gesture management |
| `Applications/ApplicationManager.cs` | **Critical** - App management |
| `Plugins/PluginManager.cs` | **Critical** - Plugin loading |
| `Configuration/FileManager.cs` | **Critical** - File I/O |
| `InterProcessCommunication/NamedPipe.cs` | IPC implementation |
| `Log/Logging.cs` | Centralized logging |

#### GestureSign.PointPatterns
| File | Purpose |
|------|---------|
| `PointPatternAnalyzer.cs` | **Critical** - Gesture recognition engine |
| `PointPatternMath.cs` | Mathematical operations |

### Critical Directories

| Directory | Purpose | Notes |
|-----------|---------|-------|
| `GestureSign.Daemon/Input/` | Input capture implementation | Touch, pen, touchpad devices |
| `GestureSign.CorePlugins/` | Built-in action plugins | 14+ plugins |
| `GestureSign.Common/Gestures/` | Gesture data models | Core abstractions |
| `GestureSign.ControlPanel/Dialogs/` | UI dialogs | XAML + code-behind |
| `WindowsInput/Native/` | P/Invoke declarations | Low-level Windows APIs |

---

## Testing & Debugging

### Manual Testing

#### Testing Gesture Recognition
1. **Run Daemon in debug mode**
   - Set `GestureSign.Daemon` as startup project
   - Press F5 (Start Debugging)

2. **Enable gesture visualization**
   - Gesture drawing should show on `SurfaceForm`
   - Check console output for captured points

3. **Test various gestures**
   - Simple gestures: straight lines, circles
   - Complex gestures: multi-point, sequential
   - Edge cases: very fast, very slow, pressure variation

4. **Verify recognition accuracy**
   - Check match confidence scores in logs
   - Test false positive/negative rates
   - Adjust thresholds in `PointPatternAnalyzer` if needed

#### Testing Plugins
1. **Open ControlPanel**
   - Run `GestureSign.ControlPanel.exe`
   - Navigate to "Available Actions"

2. **Add action to gesture**
   - Select gesture
   - Add plugin action
   - Configure settings
   - Save

3. **Test execution**
   - Perform gesture
   - Verify action executes correctly
   - Check logs for errors

4. **Test edge cases**
   - Invalid settings
   - Missing files/apps
   - Permission issues

#### Testing IPC
1. **Run both Daemon and ControlPanel**
2. **Monitor named pipe communication**
   - Set breakpoints in `MessageProcessor.cs`
   - Watch `IpcCommands` values
3. **Test configuration reload**
   - Modify gestures in ControlPanel
   - Verify Daemon receives reload command
   - Confirm Daemon reloads configuration

### Debugging Tips

#### 1. **Attach to Process**
```
Debug → Attach to Process → GestureSign.exe
```
Useful when Daemon already running.

#### 2. **Enable Detailed Logging**
```csharp
// In App.config or code
Logging.LogLevel = LogLevel.Debug;
```

#### 3. **Log File Locations**
- **Daemon logs:** `%LOCALAPPDATA%\GestureSign\logs\Daemon_YYYYMMDD.log`
- **ControlPanel logs:** `%LOCALAPPDATA%\GestureSign\logs\ControlPanel_YYYYMMDD.log`

#### 4. **Common Breakpoint Locations**
- `PointCapture.OnInputActivity()` - When input detected
- `PointPatternAnalyzer.GetPointPatternMatchResult()` - Recognition
- `TriggerManager.OnGestureRecognized()` - Gesture matched
- `PluginManager.ExecuteAction()` - Plugin execution
- `MessageProcessor.ProcessMessage()` - IPC messages

#### 5. **Watch Window Values**
```
// Useful values to watch
PointCapture.Instance._capturedPoints
GestureManager.Instance.Gestures
ApplicationManager.Instance.Applications
PluginManager.Instance.Plugins
```

#### 6. **Debugging Input Issues**
```csharp
// Add temporary logging in PointCapture
protected override void OnInputActivity(RawData data)
{
    Logging.LogMessage($"Input: Type={data.Type}, X={data.X}, Y={data.Y}");
    // ... rest of method
}
```

### Performance Profiling

#### Visual Studio Profiler
1. **Debug → Performance Profiler**
2. **Select:** CPU Usage, Memory Usage
3. **Start profiling**
4. **Perform gestures**
5. **Stop and analyze**

**Key metrics:**
- Input capture latency (should be <10ms)
- Recognition time (should be <100ms)
- Memory leaks in long-running Daemon

### Unit Testing (Future Enhancement)

**Recommended structure:**
```
GestureSign.Tests/
├── Common.Tests/
│   ├── GestureManagerTests.cs
│   ├── ApplicationManagerTests.cs
│   └── FileManagerTests.cs
├── PointPatterns.Tests/
│   ├── PointPatternAnalyzerTests.cs
│   └── PointPatternMathTests.cs
└── Plugins.Tests/
    └── [PluginName]Tests.cs
```

**Example test:**
```csharp
[TestClass]
public class PointPatternAnalyzerTests
{
    [TestMethod]
    public void TestCircleRecognition()
    {
        // Arrange
        var circlePoints = GenerateCirclePoints(100, 100, 50);
        var analyzer = new PointPatternAnalyzer();

        // Act
        var result = analyzer.GetPointPatternMatchResult(circlePoints);

        // Assert
        Assert.IsTrue(result.Confidence > 0.8);
        Assert.AreEqual("Circle", result.GestureName);
    }
}
```

---

## Troubleshooting

### Common Issues & Solutions

#### 1. Daemon Not Capturing Input

**Symptoms:**
- No gesture visualization
- Input events not firing

**Possible Causes:**
- Raw Input API not initialized
- Insufficient permissions
- Device not detected

**Solutions:**
```csharp
// Check device detection
var devices = InputProvider.GetDevices();
if (devices.Count == 0)
{
    Logging.LogMessage("No input devices detected!");
    // Check Windows Device Manager for touch/pen devices
}

// Verify Raw Input registration
// In PointCapture.cs, check RegisterRawInputDevices() return value
```

**Manual checks:**
- Run as Administrator
- Check Windows Device Manager for HID devices
- Verify touch/pen drivers installed

#### 2. IPC Communication Failure

**Symptoms:**
- ControlPanel can't communicate with Daemon
- Timeout exceptions

**Possible Causes:**
- Daemon not running
- Named pipe name mismatch
- Firewall blocking

**Solutions:**
```csharp
// Check if Daemon is running
var isRunning = Process.GetProcessesByName("GestureSign").Length > 0;

// Increase timeout
NamedPipe.Timeout = 10000; // 10 seconds

// Check pipe name consistency
// Ensure both Daemon and ControlPanel use same pipe name
```

#### 3. Gesture Not Recognized

**Symptoms:**
- Gesture drawn but not triggering action

**Possible Causes:**
- Gesture not saved properly
- Low match confidence
- Application filter mismatch

**Debug steps:**
```csharp
// 1. Check gesture exists
var gestures = GestureManager.Instance.Gestures;
var exists = gestures.Any(g => g.Name == "MyGesture");

// 2. Check match confidence
// Add logging in PointPatternAnalyzer
Logging.LogMessage($"Match confidence: {matchResult.Confidence}");

// 3. Verify application context
var currentApp = ApplicationManager.Instance.GetMatchedApplications();
Logging.LogMessage($"Current app: {currentApp?.Name}");
```

**Solutions:**
- Redraw gesture with clearer pattern
- Lower match threshold (adjust in PointPatternAnalyzer)
- Check application matching rules

#### 4. Plugin Not Loading

**Symptoms:**
- Plugin missing from Available Actions

**Possible Causes:**
- Assembly not compiled
- IPlugin interface not implemented correctly
- Exception during plugin initialization

**Debug steps:**
```csharp
// Check PluginManager.LoadPlugins() for exceptions
try
{
    PluginManager.Instance.LoadPlugins();
}
catch (Exception ex)
{
    Logging.LogException(ex);
}

// List loaded plugins
foreach (var plugin in PluginManager.Instance.Plugins)
{
    Logging.LogMessage($"Loaded: {plugin.Name}");
}
```

#### 5. JSON Serialization Error

**Symptoms:**
- Failed to save/load gestures or actions
- `JsonSerializationException`

**Possible Causes:**
- Circular references
- Type mismatch during deserialization
- Corrupted JSON file

**Solutions:**
```csharp
// Check JSON file validity
try
{
    var json = File.ReadAllText("gestures.gest");
    JsonConvert.DeserializeObject<List<Gesture>>(json);
}
catch (JsonException ex)
{
    // File corrupted - restore from backup
    File.Copy("gestures.gest.bak", "gestures.gest", true);
}

// Add error handling to serialization
var settings = new JsonSerializerSettings
{
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    Error = (sender, args) =>
    {
        Logging.LogMessage($"Serialization error: {args.ErrorContext.Error.Message}");
        args.ErrorContext.Handled = true;
    }
};
```

#### 6. High CPU Usage

**Symptoms:**
- Daemon consuming excessive CPU
- System lag during gesture capture

**Possible Causes:**
- Tight input processing loop
- Memory leaks causing GC pressure
- Inefficient pattern matching

**Solutions:**
```csharp
// Add throttling to input processing
private DateTime _lastProcessTime = DateTime.MinValue;

void ProcessInput(RawData data)
{
    if ((DateTime.Now - _lastProcessTime).TotalMilliseconds < 10)
        return; // Throttle to 100 FPS max

    _lastProcessTime = DateTime.Now;
    // ... process input
}

// Profile with Visual Studio Performance Profiler
// Look for hot paths in PointPatternAnalyzer
```

#### 7. UAC/Elevation Issues

**Symptoms:**
- Can't control elevated applications
- Input capture fails for admin apps

**Possible Causes:**
- GestureSign not running elevated
- uiAccess not configured correctly

**Solutions:**
- Run GestureSign as Administrator (right-click → Run as Administrator)
- Build with `uiAccessRelease` configuration
- Sign with trusted certificate
- Install to Program Files

#### 8. Memory Leaks

**Symptoms:**
- Memory usage grows over time
- Application crashes after extended use

**Debug steps:**
1. Use Visual Studio Memory Profiler
2. Look for:
   - Event handlers not unsubscribed
   - Large collections not cleared
   - Disposable objects not disposed

**Common leak sources:**
```csharp
// BAD: Event handler not unsubscribed
PointCapture.Instance.CaptureStarted += OnCaptureStarted;
// GOOD: Unsubscribe when done
PointCapture.Instance.CaptureStarted -= OnCaptureStarted;

// BAD: Timer not disposed
var timer = new Timer();
// GOOD: Dispose properly
timer?.Dispose();

// BAD: Large list never cleared
_capturedPoints.Add(point);
// GOOD: Clear when capture ends
_capturedPoints.Clear();
```

---

## Best Practices Summary

### For AI Assistants

1. **Before Making Changes:**
   - Read relevant source files first
   - Understand existing patterns
   - Check for similar implementations elsewhere

2. **When Adding Features:**
   - Follow existing architectural patterns (Singleton, Manager, Plugin, etc.)
   - Use established naming conventions
   - Add appropriate error handling and logging
   - Consider multi-language support

3. **When Modifying Core Logic:**
   - Be extremely careful with `PointCapture`, `PointPatternAnalyzer`, `TriggerManager`
   - Test extensively - these are critical paths
   - Consider backward compatibility for gesture/action files

4. **When Writing Code:**
   - Use interfaces for testability
   - Implement IDisposable when managing resources
   - Add XML documentation for public APIs
   - Log exceptions before throwing/handling

5. **When Debugging Issues:**
   - Check log files first (in %LOCALAPPDATA%\GestureSign\logs\)
   - Use breakpoints in critical paths
   - Verify IPC communication
   - Test with different build configurations

6. **When Committing:**
   - Follow commit message conventions (lowercase, concise)
   - One logical change per commit
   - Test before committing
   - Ensure solution builds in both Debug and Release

---

## Quick Reference

### Common Code Patterns

```csharp
// Singleton access
var instance = PointCapture.Instance;

// Event subscription
GestureManager.GestureSaved += OnGestureSaved;

// IPC communication
await NamedPipe.SendMessageAsync(IpcCommands.ReloadGestures);

// Logging
Logging.LogMessage("Info message");
Logging.LogException(ex);

// File I/O
FileManager.SaveObject(obj, path);
var obj = FileManager.LoadObject<T>(path);

// Serialization
var json = JsonConvert.SerializeObject(obj);
var obj = JsonConvert.DeserializeObject<T>(json);

// Localization
var text = LocalizationProvider.Instance.GetTextValue("KeyName");

// Input simulation
var sim = new InputSimulator();
sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
sim.Mouse.LeftButtonClick();
```

### File Paths

```csharp
// Configuration directory
AppConfig.ApplicationDataPath // %APPDATA%\GestureSign\

// Log directory
AppConfig.LocalApplicationDataPath // %LOCALAPPDATA%\GestureSign\

// Gesture file
Path.Combine(AppConfig.ApplicationDataPath, "gestures.gest")

// Action file
Path.Combine(AppConfig.ApplicationDataPath, "actions.gsa")
```

### Build Commands

```bash
# Build Debug
msbuild GestureSign.sln /p:Configuration=Debug

# Build Release
msbuild GestureSign.sln /p:Configuration=Release

# Clean
msbuild GestureSign.sln /t:Clean

# Rebuild
msbuild GestureSign.sln /t:Rebuild
```

---

## Additional Resources

### Documentation Locations
- **README.md** - Project overview and features
- **LICENSE** - MIT License
- **Source code XML comments** - API documentation

### External Dependencies Documentation
- [MahApps.Metro](https://mahapps.com/) - WPF UI framework
- [Newtonsoft.Json](https://www.newtonsoft.com/json/help/html/Introduction.htm) - JSON library
- [WindowsInput](https://inputsimulator.codeplex.com/) - Input simulation (archived)

### Windows API References
- [Raw Input API](https://docs.microsoft.com/en-us/windows/win32/inputdev/raw-input)
- [SendInput API](https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput)
- [HID API](https://docs.microsoft.com/en-us/windows-hardware/drivers/hid/)

---

**End of CLAUDE.md**

*This document should be updated as the codebase evolves. Last updated: 2025-11-17 for version 8.1*
