#if WINAPPSDK

using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Loader;
using Silk.NET.OpenGL;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.WinUI.Graphics;

public abstract partial class GLCanvasElement
{
	internal interface INativeOpenGLWrapper
	{
		public delegate IntPtr GLGetProcAddress(string proc);

		/// <summary>
		/// Creates an OpenGL context for a native window/surface that the
		/// <param name="element"></param> belongs to. The <see cref="NativeOpenGLWrapper"/>
		/// will be associated with this element until a corresponding call to <see cref="DestroyContext"/>.
		/// </summary>
		public void CreateContext(UIElement element);

		/// <remarks>This should be cast to a Silk.NET.GL</remarks>
		public object CreateGLSilkNETHandle();

		/// <summary>
		/// Destroys the context created in <see cref="CreateContext"/>. This is only called if a preceding
		/// call to <see cref="CreateContext"/> is made (after the last call to <see cref="DestroyContext"/>).
		/// </summary>
		public void DestroyContext();

		/// <summary>
		/// Makes the OpenGL context created in <see cref="CreateContext"/> the current context for the thread.
		/// </summary>
		/// <returns>A disposable that restores the OpenGL context to what it was at the time of this method call.</returns>
		public IDisposable MakeCurrent();
	}

	internal class WinUINativeOpenGLWrapper(Func<Window> getWindowFunc) : INativeOpenGLWrapper
	{
		private nint _hdc;
		private nint _glContext;

		public void CreateContext(UIElement element)
		{
			var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(getWindowFunc());

			_hdc = NativeMethods.GetDC(hwnd);

			NativeMethods.PIXELFORMATDESCRIPTOR pfd = new();
			pfd.nSize = (ushort)Marshal.SizeOf(pfd);
			pfd.nVersion = 1;
			pfd.dwFlags = NativeMethods.PFD_DRAW_TO_WINDOW | NativeMethods.PFD_SUPPORT_OPENGL | NativeMethods.PFD_DOUBLEBUFFER;
			pfd.iPixelType = NativeMethods.PFD_TYPE_RGBA;
			pfd.cColorBits = 32;
			pfd.cRedBits = 8;
			pfd.cGreenBits = 8;
			pfd.cBlueBits = 8;
			pfd.cAlphaBits = 8;
			pfd.cDepthBits = 16;
			pfd.cStencilBits = 1; // anything > 0 is fine, we will most likely get 8
			pfd.iLayerType = NativeMethods.PFD_MAIN_PLANE;

			var pixelFormat = NativeMethods.ChoosePixelFormat(_hdc, ref pfd);

			// To inspect the chosen pixel format:
			// WpfRenderingNativeMethods.PIXELFORMATDESCRIPTOR temp_pfd = default;
			// WpfRenderingNativeMethods.DescribePixelFormat(_hdc, _pixelFormat, (uint)Marshal.SizeOf<WpfRenderingNativeMethods.PIXELFORMATDESCRIPTOR>(), ref temp_pfd);

			if (pixelFormat == 0)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"ChoosePixelFormat failed");
				}
				throw new InvalidOperationException("ChoosePixelFormat failed");
			}

			if (NativeMethods.SetPixelFormat(_hdc, pixelFormat, ref pfd) == 0)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"SetPixelFormat failed");
				}
				throw new InvalidOperationException("ChoosePixelFormat failed");
			}

			_glContext = NativeMethods.wglCreateContext(_hdc);

			if (_glContext == IntPtr.Zero)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"wglCreateContext failed");
				}
				throw new InvalidOperationException("ChoosePixelFormat failed");
			}
		}

		public object CreateGLSilkNETHandle() => GL.GetApi(new WpfGlNativeContext());

		public void DestroyContext()
		{
			NativeMethods.wglDeleteContext(_glContext);
			_glContext = default;
			_hdc = default;
		}

		public IDisposable MakeCurrent()
		{
			var glContext = NativeMethods.wglGetCurrentContext();
			var dc = NativeMethods.wglGetCurrentDC();
			NativeMethods.wglMakeCurrent(_hdc, _glContext);
			return Disposable.Create(() => NativeMethods.wglMakeCurrent(dc, glContext));
		}

		// https://sharovarskyi.com/blog/posts/csharp-win32-opengl-silknet/
		private class WpfGlNativeContext : INativeContext
		{
			private readonly UnmanagedLibrary _l;

			public WpfGlNativeContext()
			{
				_l = new UnmanagedLibrary("opengl32.dll");
				if (_l.Handle == IntPtr.Zero)
				{
					throw new PlatformNotSupportedException("Unable to load opengl32.dll. Make sure you're running on a system with OpenGL support");
				}
			}

			public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
			{
				if (_l.TryLoadFunction(proc, out addr))
				{
					return true;
				}

				addr = NativeMethods.wglGetProcAddress(proc);
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
	}

	// https://sharovarskyi.com/blog/posts/csharp-win32-opengl-silknet/
	private class WinUINativeContext : INativeContext
	{
		private readonly UnmanagedLibrary _l;

		public WinUINativeContext()
		{
			_l = new UnmanagedLibrary("opengl32.dll");
			if (_l.Handle == IntPtr.Zero)
			{
				throw new PlatformNotSupportedException("Unable to load opengl32.dll. Make sure you're running on a system with OpenGL support");
			}
		}

		public bool TryGetProcAddress(string proc, out nint addr, int? slot = null)
		{
			if (_l.TryLoadFunction(proc, out addr))
			{
				return true;
			}

			addr = NativeMethods.wglGetProcAddress(proc);
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

	private static class NativeMethods
	{
		[DllImport("user32.dll")]
		internal static extern IntPtr GetDC(IntPtr hWnd);

		[DllImport("gdi32.dll")]
		internal static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR ppfd);

		[DllImport("gdi32.dll")]
		internal static extern int SetPixelFormat(IntPtr hdc, int iPixelFormat, ref PIXELFORMATDESCRIPTOR ppfd);

		[DllImport("gdi32.dll")]
		internal static extern int DescribePixelFormat(IntPtr hdc, int iPixelFormat, uint nBytes, ref PIXELFORMATDESCRIPTOR ppfd);

		[DllImport("opengl32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
		public static extern IntPtr wglGetProcAddress(string functionName);

		[DllImport("opengl32.dll")]
		internal static extern IntPtr wglCreateContext(IntPtr hdc);

		[DllImport("opengl32.dll")]
		public static extern IntPtr wglGetCurrentDC();

		[DllImport("opengl32.dll")]
		public static extern IntPtr wglGetCurrentContext();

		[DllImport("opengl32.dll")]
		internal static extern int wglDeleteContext(IntPtr hglrc);

		[DllImport("opengl32.dll")]
		internal static extern int wglMakeCurrent(IntPtr hdc, IntPtr hglrc);

		[StructLayout(LayoutKind.Sequential)]
		internal struct PIXELFORMATDESCRIPTOR
		{
			public ushort nSize;
			public ushort nVersion;
			public uint dwFlags;
			public byte iPixelType;
			public byte cColorBits;
			public byte cRedBits;
			public byte cRedShift;
			public byte cGreenBits;
			public byte cGreenShift;
			public byte cBlueBits;
			public byte cBlueShift;
			public byte cAlphaBits;
			public byte cAlphaShift;
			public byte cAccumBits;
			public byte cAccumRedBits;
			public byte cAccumGreenBits;
			public byte cAccumBlueBits;
			public byte cAccumAlphaBits;
			public byte cDepthBits;
			public byte cStencilBits;
			public byte cAuxBuffers;
			public byte iLayerType;
			public byte bReserved;
			public uint dwLayerMask;
			public uint dwVisibleMask;
			public uint dwDamageMask;
		}

		internal const int PFD_DRAW_TO_WINDOW = 0x00000004;
		internal const int PFD_SUPPORT_OPENGL = 0x00000020;
		internal const int PFD_DOUBLEBUFFER = 0x00000001;
		internal const int PFD_TYPE_RGBA = 0;

		internal const int PFD_MAIN_PLANE = 0;
		internal const int WGL_SWAP_MAIN_PLANE = 1;
	}
}

#endif
