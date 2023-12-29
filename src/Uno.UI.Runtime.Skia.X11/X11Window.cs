using System;

namespace Uno.WinUI.Runtime.Skia.X11;

internal record struct X11Window(IntPtr Display, IntPtr Window);
