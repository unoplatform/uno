using System;
using Android.Content;
using Android.Graphics;
using Android.Opengl;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Autofill;
using Android.Views.InputMethods;
using AndroidX.Core.View;
using Microsoft.UI.Xaml;
using SkiaSharp;
using Uno.Disposables;
using Uno.Foundation.Logging;
using MUXWindow = Microsoft.UI.Xaml.Window;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class UnoSKCanvasView : SurfaceView, ISurfaceHolderCallback
{
	private const uint DefaultFramebuffer = 0;

	private EGLDisplay? _eglDisplay;
	private EGLContext? _glContext;
	private EGLSurface? _eglWindowSurface;
	private GRContext? _grContext;
	private int _stencil;
	private GRBackendRenderTarget? _renderTarget;
	private SKSurface? _surface;
	private MUXWindow _window;

	public event EventHandler<SKSurface>? PaintSurface;

	internal UIElement? RootElement => _window.RootElement;
	internal UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	internal TextInputPlugin TextInputPlugin { get; }

	internal static UnoSKCanvasView? Instance { get; private set; }

	public UnoSKCanvasView(Context context, MUXWindow window) : base(context)
	{
		Instance = this;
		_window = window;
		ExploreByTouchHelper = new UnoExploreByTouchHelper(this);
		TextInputPlugin = new TextInputPlugin(this);
		ViewCompat.SetAccessibilityDelegate(this, ExploreByTouchHelper);
		Focusable = true;
		FocusableInTouchMode = true;
		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
		{
			ImportantForAutofill = ImportantForAutofill.Yes;
		}

		SetWillNotDraw(false);
		this.Holder?.AddCallback(this);
	}

	internal IDisposable MakeCurrent()
	{
		var glContext = EGL14.EglGetCurrentContext();
		var display = EGL14.EglGetCurrentDisplay();
		var readSurface = EGL14.EglGetCurrentSurface(EGL14.EglRead);
		var drawSurface = EGL14.EglGetCurrentSurface(EGL14.EglDraw);
		if (!EGL14.EglMakeCurrent(_eglDisplay, _eglWindowSurface, _eglWindowSurface, _glContext))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(EGL14.EglMakeCurrent)} failed.");
			}
		}
		return Disposable.Create(() =>
		{
			if (!EGL14.EglMakeCurrent(display, drawSurface, readSurface, glContext))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(EGL14.EglMakeCurrent)} failed.");
				}
			}
		});
	}

	protected override void OnDraw(Canvas canvas)
	{
		base.OnDraw(canvas);

		if (_surface is null)
		{
			return;
		}

		using var _ = MakeCurrent();

		PaintSurface?.Invoke(this, _surface);

		_surface.Flush();
		EGL14.EglSwapBuffers(_eglDisplay, _eglWindowSurface);
	}

	protected override bool DispatchHoverEvent(MotionEvent? e)
	{
		return ExploreByTouchHelper.DispatchHoverEvent(e) ||
			base.DispatchHoverEvent(e);
	}

	public override bool DispatchKeyEvent(KeyEvent? e)
	{
		return ExploreByTouchHelper.DispatchKeyEvent(e) ||
			base.DispatchKeyEvent(e);
	}

	protected override void OnFocusChanged(bool gainFocus, [GeneratedEnum] FocusSearchDirection direction, Rect? previouslyFocusedRect)
	{
		base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);
		ExploreByTouchHelper.OnFocusChanged(gainFocus, (int)direction, previouslyFocusedRect);
	}

	public override void OnProvideAutofillVirtualStructure(ViewStructure? structure, [GeneratedEnum] AutofillFlags flags)
	{
		base.OnProvideAutofillVirtualStructure(structure, flags);

		if (Build.VERSION.SdkInt < BuildVersionCodes.O)
		{
			return;
		}

		TextInputPlugin.OnProvideAutofillVirtualStructure(structure);
	}

	public override void Autofill(SparseArray values)
	{
		var count = values.Size();
		for (int i = 0; i < count; i++)
		{
			var virtualId = values.KeyAt(i);
			if (AndroidSkiaTextBoxNotificationsProviderSingleton.Instance.LiveTextBoxesMap.TryGetValue(virtualId, out var textBox))
			{
				var autofillValue = (AutofillValue)values.ValueAt(i)!;
				textBox.Text = autofillValue.TextValue;
			}
		}
	}

	public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
	{
		TextInputPlugin.OnCreateInputConnection(outAttrs!);
		return base.OnCreateInputConnection(outAttrs);
	}

	void ISurfaceHolderCallback.SurfaceCreated(ISurfaceHolder holder)
	{
		_eglDisplay = EGL14.EglGetDisplay(EGL14.EglDefaultDisplay);
		if (_eglDisplay == EGL14.EglNoDisplay)
		{
			throw new InvalidOperationException($"{nameof(EGL14.EglGetDisplay)} returned no display.");
		}

		int[] major = new int[1];
		int[] minor = new int[1];
		var success = EGL14.EglInitialize(_eglDisplay, major, 0, minor, 0);
		if (!success)
		{
			throw new InvalidOperationException($"{nameof(EGL14.EglInitialize)} failed: {(EglError)EGL14.EglGetError()}");
		}

		int[] pi32ConfigAttribs =
		{
			EGL14.EglRenderableType, EGL14.EglOpenglEs2Bit, // Every Android device MUST implement OpenGL ES 2, and almost all implement ES3 as well, but let's go with the common denominator
			EGL14.EglRedSize, 8,
			EGL14.EglGreenSize, 8,
			EGL14.EglBlueSize, 8,
			EGL14.EglAlphaSize, 8,
			EGL14.EglStencilSize, 1, // necessary for skia, which internally uses the stencil buffer
			EGL14.EglNone
		};
		EGLConfig[] configs = new EGLConfig[1];
		int[] numConfigs = new int[1];
		success = EGL14.EglChooseConfig(_eglDisplay, pi32ConfigAttribs, 0, configs, 0, configs.Length, numConfigs, 0);
		if (!success)
		{
			throw new InvalidOperationException($"{nameof(EGL14.EglChooseConfig)} failed: {(EglError)EGL14.EglGetError()}");
		}
		if (numConfigs[0] == 0)
		{
			throw new InvalidOperationException($"{nameof(EGL14.EglChooseConfig)} returned no compatible configs.");
		}

		_glContext = EGL14.EglCreateContext(_eglDisplay, configs[0], EGL14.EglNoContext, new[] { EGL14.EglContextClientVersion, 2, EGL14.EglNone }, 0);
		if (_glContext is null || _glContext == EGL14.EglNoContext)
		{
			throw new InvalidOperationException($"{nameof(EGL14.EglCreateContext)} returned {(_glContext is null ? "null" : "EglNoContext")} : {(EglError)EGL14.EglGetError()}");
		}

		_eglWindowSurface = EGL14.EglCreateWindowSurface(_eglDisplay, configs[0], this.Holder?.Surface, new[] { EGL14.EglNone }, 0);
		if (_eglWindowSurface is null)
		{
			throw new InvalidOperationException($"{nameof(EGL14.EglCreateWindowSurface)} returned null : {(EglError)EGL14.EglGetError()}");
		}

		using var _ = MakeCurrent();

		var glInterface = GRGlInterface.Create();
		if (glInterface is null)
		{
			throw new InvalidOperationException($"{nameof(GRGlInterface)} creation failed.");
		}

		_grContext = GRContext.CreateGl(glInterface);
		if (_grContext is null)
		{
			throw new InvalidOperationException($"{nameof(GRContext)} creation failed.");
		}

		int[] stencil = new int[1];
		if (EGL14.EglGetConfigAttrib(_eglDisplay, configs[0], EGL14.EglStencilSize, stencil, 0))
		{
			_stencil = stencil[0];
		}
	}

	void ISurfaceHolderCallback.SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
	{
		if (_grContext is not null)
		{
			using var _ = MakeCurrent();

			const SKColorType skColorType = SKColorType.Rgba8888; // this is Rgba8888 regardless of SKImageInfo.PlatformColorType
			const GRSurfaceOrigin grSurfaceOrigin = GRSurfaceOrigin.BottomLeft; // to match OpenGL's origin

			var glInfo = new GRGlFramebufferInfo(DefaultFramebuffer, skColorType.ToGlSizedFormat());

			_renderTarget?.Dispose();
			_renderTarget = new GRBackendRenderTarget(width, height, 0, _stencil, glInfo);
			_surface?.Dispose();
			_surface = SKSurface.Create(_grContext, _renderTarget, grSurfaceOrigin, skColorType);
		}
	}

	// This never gets called, so it's currently untested and just here for safety.
	void ISurfaceHolderCallback.SurfaceDestroyed(ISurfaceHolder holder)
	{
		_stencil = 0;

		_grContext?.Dispose();
		_grContext = null;

		if (_eglDisplay is { } && _eglWindowSurface is { })
		{
			EGL14.EglDestroySurface(_eglDisplay, _eglWindowSurface);
		}
		if (_eglDisplay is { } && _glContext is { })
		{
			EGL14.EglDestroyContext(_eglDisplay, _glContext);
		}

		_eglWindowSurface = null;
		_glContext = null;
		_eglDisplay = null;
	}

	// https://github.com/KhronosGroup/EGL-Registry/blob/29c4314e0ef04c730992d295f91b76635019fbba/api/EGL/egl.h
	private enum EglError
	{
		EGL_SUCCESS = 0x3000,
		EGL_NOT_INITIALIZED,
		EGL_BAD_ACCESS,
		EGL_BAD_ALLOC,
		EGL_BAD_ATTRIBUTE,
		EGL_BAD_CONFIG,
		EGL_BAD_CONTEXT,
		EGL_BAD_CURRENT_SURFACE,
		EGL_BAD_DISPLAY,
		EGL_BAD_MATCH,
		EGL_BAD_NATIVE_PIXMAP,
		EGL_BAD_NATIVE_WINDOW,
		EGL_BAD_PARAMETER,
		EGL_BAD_SURFACE,
		EGL_CONTEXT_LOST
	}
}
