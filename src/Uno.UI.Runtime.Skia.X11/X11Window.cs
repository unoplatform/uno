using System;

namespace Uno.WinUI.Runtime.Skia.X11;

internal record struct X11Window(IntPtr Display, IntPtr Window, (int stencilBits, int sampleCount, IntPtr context)? glXInfo)
{
	public X11Window(IntPtr Display, IntPtr Window) : this(Display, Window, null)
	{
	}

	public readonly void Deconstruct(out IntPtr Display, out IntPtr Window, out (int stencilBits, int sampleCount, IntPtr context)? GLXInfo)
	{
		Display = this.Display;
		Window = this.Window;
		GLXInfo = this.glXInfo;
	}
}
