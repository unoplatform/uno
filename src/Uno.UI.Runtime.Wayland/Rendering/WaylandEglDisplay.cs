#nullable enable

using System;
using System.Collections.Generic;

namespace Uno.UI.Runtime.Wayland.Rendering;

internal static class WaylandEglDisplay
{
	private static readonly Dictionary<IntPtr, int> _references = new();
	public static IntPtr Acquire(IntPtr nativeDisplay)
	{
		lock (_references)
		{
			var display = WaylandRenderNative.eglGetPlatformDisplay(0x31D8, nativeDisplay, IntPtr.Zero);
			if (display == IntPtr.Zero)
			{
				throw new InvalidOperationException("eglGetPlatformDisplay failed.");
			}
			if (_references.TryGetValue(display, out var count))
			{
				_references[display] = count + 1;
			}
			else
			{
				if (WaylandRenderNative.eglInitialize(display, out _, out _) == 0)
				{
					throw new InvalidOperationException("eglInitialize failed.");
				}
				_references.Add(display, 1);
			}
			return display;
		}
	}
	public static void Release(IntPtr display)
	{
		lock (_references)
		{
			var count = _references[display];
			if (count > 1)
			{
				_references[display] = count - 1;
			}
			else
			{
				_references.Remove(display);
				_ = WaylandRenderNative.eglTerminate(display);
			}
		}
	}
}
