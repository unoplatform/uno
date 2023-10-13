namespace Uno.UI.Runtime.Skia.Gtk.UI.Controls;

internal record PendingWindowStateChangedInfo(Gdk.WindowState newState, Gdk.WindowState changedMask);
