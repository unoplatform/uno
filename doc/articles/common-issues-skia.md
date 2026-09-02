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

## Failed to load ICU on Linux

At startup, the following error may happen (`Failed to load icuuc.` on older Uno Platform versions):

```console
Unhandled exception. System.Reflection.TargetInvocationException: Exception has been thrown by the target of an invocation.
 ---> System.Exception: Failed to load ICU on Linux. Attempted: [...]
```

Uno Platform uses the system ICU libraries on Linux for text rendering, and the machine running the app doesn't have them installed — common on minimal, server, or embedded images. Install the distribution's ICU package (e.g. `sudo apt install libicu74`), or ship ICU inside the app with app-local ICU. See [Publishing Your App for Linux](xref:uno.publishing.desktop.linux) for both options. Setting `InvariantGlobalization` to `true` does not remove this requirement.

## Linux

[!include[linux-setup](includes/additional-linux-setup-inline.md)]

## Additional troubleshooting

You can get additional build [troubleshooting information here](uno-builds-troubleshooting.md).
