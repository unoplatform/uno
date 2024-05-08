using System;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Loader;
using Uno.UI.Runtime.Skia.Wpf.Rendering;
namespace Uno.UI.Runtime.Skia.Wpf.Extensions;

// https://sharovarskyi.com/blog/posts/csharp-win32-opengl-silknet/
internal class WpfGlNativeContext : INativeContext
{
	private readonly UnmanagedLibrary _l = new UnmanagedLibrary("opengl32.dll");

	public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
	{
		if (_l.TryLoadFunction(proc, out addr))
		{
			return true;
		}

		addr = OpenGLWpfRenderer.NativeMethods.wglGetProcAddress(proc);
		return addr != IntPtr.Zero;
	}

	public nint GetProcAddress(string proc, int? slot = null)
	{
		if (TryGetProcAddress(proc, out var address, slot))
		{
			return address;
		}

		throw new InvalidOperationException("No function was found with the name " + proc + ".");
	}

	public void Dispose() => _l.Dispose();
}
