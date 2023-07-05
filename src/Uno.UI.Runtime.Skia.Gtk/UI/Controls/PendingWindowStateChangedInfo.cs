#nullable disable

namespace Uno.UI.Runtime.Skia.GTK.UI.Controls;

internal record PendingWindowStateChangedInfo(Gdk.WindowState newState, Gdk.WindowState changedMask);
