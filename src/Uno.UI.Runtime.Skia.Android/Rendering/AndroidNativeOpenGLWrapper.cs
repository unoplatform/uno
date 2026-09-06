#nullable enable

using System;
using System.Runtime.InteropServices;
using Android.Opengl;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Graphics;

namespace Uno.WinUI.Runtime.Skia.Android;

// GLCanvasElement renders into its own offscreen FBO and reads it back, so an offscreen EGL
// pbuffer surface is all that's needed - the surface is never presented. The managed
// Android.Opengl.EGL14 binding is used for context management (no P/Invoke to libEGL needed).
internal class AndroidNativeOpenGLWrapper : INativeOpenGLWrapper
{
	private static readonly Type _type = typeof(AndroidNativeOpenGLWrapper);

	// Core GLES entry points must be resolved by dlsym on the GLES libraries; eglGetProcAddress
	// is only reliable for extension functions on some drivers. Try the libraries first (newest
	// first, so ES3 entry points resolve), then fall back to eglGetProcAddress.
	private static readonly Lazy<IntPtr> _libGLESv3 = new(() => TryLoad("libGLESv3.so"));
	private static readonly Lazy<IntPtr> _libGLESv2 = new(() => TryLoad("libGLESv2.so"));

	private EGLDisplay? _eglDisplay;
	private EGLSurface? _pBufferSurface;
	private EGLContext? _glContext;

	public AndroidNativeOpenGLWrapper(XamlRoot xamlRoot)
	{
		_eglDisplay = EGL14.EglGetDisplay(EGL14.EglDefaultDisplay);
		if (_eglDisplay is null || _eglDisplay.NativeHandle == 0L) // EGL_NO_DISPLAY
		{
			throw new InvalidOperationException($"{nameof(EGL14.EglGetDisplay)} failed.");
		}

		var versions = new int[2];
		if (!EGL14.EglInitialize(_eglDisplay, versions, 0, versions, 1))
		{
			throw new InvalidOperationException($"{nameof(EGL14.EglInitialize)} failed.");
		}

		int[] configAttribs =
		{
			EGL14.EglRenderableType, EGL14.EglOpenglEs2Bit, // ES2-renderable configs are also ES3-renderable
			EGL14.EglSurfaceType, EGL14.EglPbufferBit,
			EGL14.EglRedSize, 8,
			EGL14.EglGreenSize, 8,
			EGL14.EglBlueSize, 8,
			EGL14.EglAlphaSize, 8,
			EGL14.EglDepthSize, 24,
			EGL14.EglStencilSize, 8,
			EGL14.EglNone
		};
		var configs = new EGLConfig[1];
		var numConfigs = new int[1];
		if (!EGL14.EglChooseConfig(_eglDisplay, configAttribs, 0, configs, 0, configs.Length, numConfigs, 0) || numConfigs[0] < 1)
		{
			throw new InvalidOperationException($"{nameof(EGL14.EglChooseConfig)} failed.");
		}

		// Request an OpenGL ES 3.0 context (GLCanvasElement's minimum). >95% of Android devices
		// support ES 3.0: https://developer.android.com/about/dashboards#OpenGL
		int[] contextAttribs = { EGL14.EglContextClientVersion, 3, EGL14.EglNone };
		_glContext = EGL14.EglCreateContext(_eglDisplay, configs[0], EGL14.EglNoContext, contextAttribs, 0);
		if (_glContext is null || _glContext.NativeHandle == 0L) // EGL_NO_CONTEXT
		{
			throw new InvalidOperationException("OpenGL ES context creation failed.");
		}

		// 1x1 pbuffer: GLCanvasElement always renders into its own FBO, so the surface dimensions
		// are irrelevant - it only exists to satisfy eglMakeCurrent.
		int[] surfaceAttribs = { EGL14.EglWidth, 1, EGL14.EglHeight, 1, EGL14.EglNone };
		_pBufferSurface = EGL14.EglCreatePbufferSurface(_eglDisplay, configs[0], surfaceAttribs, 0);
		if (_pBufferSurface is null || _pBufferSurface.NativeHandle == 0L) // EGL_NO_SURFACE
		{
			throw new InvalidOperationException("EGL pbuffer surface creation failed.");
		}

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info($"Created an {nameof(AndroidNativeOpenGLWrapper)} instance using EGL {versions[0]}.{versions[1]}.");
		}
	}

	// android.opengl.EGL14 does not expose eglGetProcAddress, so P/Invoke it directly. It resolves
	// extension entry points; core GLES functions come from the dlsym path above.
	[DllImport("libEGL.so", EntryPoint = "eglGetProcAddress", CharSet = CharSet.Ansi)]
	private static extern IntPtr eglGetProcAddress(string procname);

	private static IntPtr TryLoad(string name)
		=> NativeLibrary.TryLoad(name, _type.Assembly, DllImportSearchPath.UserDirectories, out var handle) ? handle : IntPtr.Zero;

	public IntPtr GetProcAddress(string proc)
	{
		if (TryGetProcAddress(proc, out var addr))
		{
			return addr;
		}

		throw new InvalidOperationException($"A procedure named {proc} was not found in the GLES libraries.");
	}

	public bool TryGetProcAddress(string proc, out IntPtr addr)
	{
		if (_libGLESv3.Value != IntPtr.Zero && NativeLibrary.TryGetExport(_libGLESv3.Value, proc, out addr))
		{
			return true;
		}

		if (_libGLESv2.Value != IntPtr.Zero && NativeLibrary.TryGetExport(_libGLESv2.Value, proc, out addr))
		{
			return true;
		}

		addr = eglGetProcAddress(proc);
		return addr != IntPtr.Zero;
	}

	public IDisposable MakeCurrent()
	{
		var previousContext = EGL14.EglGetCurrentContext();
		var previousDisplay = EGL14.EglGetCurrentDisplay();
		var previousReadSurface = EGL14.EglGetCurrentSurface(EGL14.EglRead);
		var previousDrawSurface = EGL14.EglGetCurrentSurface(EGL14.EglDraw);

		if (!EGL14.EglMakeCurrent(_eglDisplay, _pBufferSurface, _pBufferSurface, _glContext))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(EGL14.EglMakeCurrent)} failed.");
			}
		}

		return Disposable.Create(() =>
		{
			// Restoring requires a display; eglGetCurrentDisplay returns EGL_NO_DISPLAY (handle 0)
			// when there was no current context, in which case our own display is the right one
			// to unbind on.
			var restoreDisplay = previousDisplay is { } d && d.NativeHandle != 0L ? d : _eglDisplay;
			if (!EGL14.EglMakeCurrent(restoreDisplay, previousDrawSurface, previousReadSurface, previousContext))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EGL14.EglMakeCurrent)} (restore) failed.");
				}
			}
		});
	}

	public void Dispose()
	{
		if (_eglDisplay is { } display)
		{
			if (_pBufferSurface is { } surface)
			{
				EGL14.EglDestroySurface(display, surface);
			}
			if (_glContext is { } context)
			{
				EGL14.EglDestroyContext(display, context);
			}
		}

		_pBufferSurface = null;
		_glContext = null;
		_eglDisplay = null;
	}

	public static void Register() => ApiExtensibility.Register<XamlRoot>(typeof(INativeOpenGLWrapper), xamlRoot => new AndroidNativeOpenGLWrapper(xamlRoot));
}
