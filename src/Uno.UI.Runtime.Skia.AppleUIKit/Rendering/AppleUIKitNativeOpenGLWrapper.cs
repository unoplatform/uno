#nullable enable

// EAGL (the iOS/tvOS OpenGL ES context API) is unavailable on Mac Catalyst, so this whole
// wrapper is excluded there. Registration is additionally gated at runtime to iOS/tvOS.
#if !__MACCATALYST__

using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using OpenGLES;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Graphics;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

// GLCanvasElement renders into its own offscreen FBO and reads it back, so an EAGL context made
// current (without any layer-backed drawable) is sufficient - EAGL has no pbuffer concept; an
// FBO bound while the context is current is the offscreen target.
//
// NOT VALIDATED: this could not be compiled or run in the authoring environment (iOS builds
// require a Mac). The EAGL/OpenGLES API usage below is written from the documented surface.
internal class AppleUIKitNativeOpenGLWrapper : INativeOpenGLWrapper
{
	// On iOS/tvOS the GLES entry points live in the OpenGLES framework; resolve them by dlsym.
	private const string OpenGLESFramework = "/System/Library/Frameworks/OpenGLES.framework/OpenGLES";
	private static readonly Lazy<IntPtr> _openGLES = new(() =>
		NativeLibrary.TryLoad(OpenGLESFramework, out var handle) ? handle : IntPtr.Zero);

	private EAGLContext? _context;

	public AppleUIKitNativeOpenGLWrapper(XamlRoot xamlRoot)
	{
		// >99% of iOS/tvOS devices support OpenGL ES 3.0 (GLCanvasElement's minimum).
		_context = new EAGLContext(EAGLRenderingAPI.OpenGLES3);
		if (_context is null)
		{
			throw new InvalidOperationException("Failed to create an OpenGL ES 3.0 EAGLContext.");
		}

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Created an {nameof(AppleUIKitNativeOpenGLWrapper)} instance (EAGL, OpenGL ES 3.0).");
		}
	}

	public IntPtr GetProcAddress(string proc)
	{
		if (TryGetProcAddress(proc, out var addr))
		{
			return addr;
		}

		throw new InvalidOperationException($"A procedure named {proc} was not found in the OpenGLES framework.");
	}

	public bool TryGetProcAddress(string proc, out IntPtr addr)
	{
		if (_openGLES.Value != IntPtr.Zero && NativeLibrary.TryGetExport(_openGLES.Value, proc, out addr))
		{
			return true;
		}

		addr = IntPtr.Zero;
		return false;
	}

	public IDisposable MakeCurrent()
	{
		var previous = EAGLContext.CurrentContext;
		if (!EAGLContext.SetCurrentContext(_context))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(EAGLContext.SetCurrentContext)} failed.");
			}
		}

		return Disposable.Create(() =>
		{
			if (!EAGLContext.SetCurrentContext(previous))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EAGLContext.SetCurrentContext)} (restore) failed.");
				}
			}
		});
	}

	public void Dispose()
	{
		if (ReferenceEquals(EAGLContext.CurrentContext, _context))
		{
			EAGLContext.SetCurrentContext(null);
		}

		_context?.Dispose();
		_context = null;
	}

	public static void Register()
	{
		// EAGL exists on iOS and tvOS only.
		if (OperatingSystem.IsIOS() || OperatingSystem.IsTvOS())
		{
			ApiExtensibility.Register<XamlRoot>(typeof(INativeOpenGLWrapper), xamlRoot => new AppleUIKitNativeOpenGLWrapper(xamlRoot));
		}
	}
}

#endif
