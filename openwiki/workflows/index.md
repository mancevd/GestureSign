# Files

- [Input capture](input-capture.md) - HID and mouse-hook points flow through MessageWindow, PointEventTranslator, PointCapture state machine, optional touch blocking, and SurfaceForm.
- [Recognition and execution](recognition-and-execution.md) - End-to-end from CaptureStarted window match through gesture probability to sequential plugin Gestured calls.
- [Teaching and editing](teaching.md) - Training-mode IPC, GestureSelector stacking, GestureDefinition/ActionDialog/CommandDialog saves that reload the daemon.
- [Non-stroke triggers](triggers.md) - Hotkey, mouse-button, and continuous-direction triggers that reuse PluginManager.ExecuteAction without a named stroke match.
