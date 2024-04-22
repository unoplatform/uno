---
uid: Uno.UI.CommonIssues.Skia
---

# Issues related to Skia-based projects

## System.DllNotFoundException: Gtk: libgtk-3-0.dll

When running the Skia.GTK project head, the following error may happen:

```console
Unhandled exception. System.TypeInitializationException: The type initializer for 'Gtk.Application' threw an exception.
---> System.DllNotFoundException: Gtk: libgtk-3-0.dll, libgtk-3.so.0, libgtk-3.0.dylib, gtk-3.dll
```

## Linux

[!include[linux-setup](includes/additional-linux-setup-inline.md)]
