// #define FPS_DISPLAY

using System;
using Windows.Graphics.Display;
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
using Javax.Microedition.Khronos.Opengles;
using Microsoft.UI.Xaml;
using SkiaSharp;
using SkiaSharp.Views.Android;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class UnoSKCanvasView : GLSurfaceView
{
#if FPS_DISPLAY
	private long _counter;
	private DateTime _time = DateTime.UtcNow;
	private string _fpsText = "0";
#endif

	private SKPicture? _picture;

	internal UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	internal TextInputPlugin TextInputPlugin { get; }

	internal static UnoSKCanvasView? Instance { get; private set; }

	public UnoSKCanvasView(Context context) : base(context)
	{
		SetEGLContextClientVersion(2);
		SetEGLConfigChooser(8, 8, 8, 8, 0, 8);

		var renderer = new InternalRenderer(this);
		SetRenderer(renderer);

		Instance = this;
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
	}

	protected override void OnDraw(Canvas c)
	{
		base.OnDraw(c);

		if (Microsoft.UI.Xaml.Window.CurrentSafe is not { RootElement: { } root } window)
		{
			return;
		}

		ExploreByTouchHelper.InvalidateRoot();

		var recorder = new SKPictureRecorder();
		var canvas = recorder.BeginRecording(new SKRect(-9999, -9999, 9999, 9999));
		using (new SKAutoCanvasRestore(canvas, true))
		{
			canvas.Clear(SKColors.Transparent);
			var scale = DisplayInformation.GetForCurrentViewSafe()!.RawPixelsPerViewPixel;
			canvas.Scale((float)scale);
			var negativePath = SkiaRenderHelper.RenderRootVisualAndReturnNegativePath((int)window.Bounds.Width,
				(int)window.Bounds.Height, root.Visual, canvas);
			if (ApplicationActivity.Instance.NativeLayerHost is { } nativeLayerHost)
			{
				nativeLayerHost.Path = negativePath;
				nativeLayerHost.Invalidate();
			}

#if FPS_DISPLAY
			// This naively calculates the difference in time every 100 frames, so to get
			// a usable number, open a sample with a continuously-running animation.
			_counter++;
			if (_counter % 100 == 0)
			{
				var newTime = DateTime.UtcNow;
				_fpsText = $"{100 / (newTime - _time).TotalSeconds}";
				_time = newTime;
			}
			canvas.DrawText(
				_fpsText,
				(float)(window.Bounds.Width / 2),
				(float)(window.Bounds.Height / 2),
				new SKFont(SKTypeface.Default, size: 20F),
				new SKPaint { Color = SKColors.Red});
#endif

			_picture = recorder.EndRecording();
		}
	}

	public override bool OnCheckIsTextEditor()
		// Required for the InputConnection to be created
		=> true;

	protected override bool DispatchHoverEvent(MotionEvent? e)
	{
		if (e is null)
		{
			return base.DispatchHoverEvent(e);
		}

		return ExploreByTouchHelper.DispatchHoverEvent(e) ||
			base.DispatchHoverEvent(e);
	}

	public override bool DispatchKeyEvent(KeyEvent? e)
	{
		if (e is null)
		{
			return base.DispatchKeyEvent(e);
		}

		return ExploreByTouchHelper.DispatchKeyEvent(e) ||
			base.DispatchKeyEvent(e);
	}

	protected override void OnFocusChanged(bool gainFocus, [GeneratedEnum] FocusSearchDirection direction, Rect? previouslyFocusedRect)
	{
		base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);

		try
		{
			ExploreByTouchHelper.OnFocusChanged(gainFocus, (int)direction, previouslyFocusedRect);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"{nameof(UnoSKCanvasView)}.{nameof(OnFocusChanged)} failed", e);
			}
		}
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
		=> TextInputPlugin.OnCreateInputConnection(outAttrs!);

	// Copied from https://github.com/mono/SkiaSharp/blob/main/source/SkiaSharp.Views/SkiaSharp.Views/Platform/Android/SKGLSurfaceView.cs
	private class InternalRenderer(UnoSKCanvasView surfaceView) : Java.Lang.Object, IRenderer
	{
		private const SKColorType ColorType = SKColorType.Rgba8888;
		private const GRSurfaceOrigin SurfaceOrigin = GRSurfaceOrigin.BottomLeft;

		private GRContext? _context;
		private GRGlFramebufferInfo _glInfo;
		private GRBackendRenderTarget? _renderTarget;
		private SKSurface? _surface;
		private SKCanvas? _canvas;

		private SKSizeI _lastSize;
		private SKSizeI _newSize;

		void IRenderer.OnDrawFrame(IGL10? gl)
		{
			GLES20.GlClear(GLES20.GlColorBufferBit | GLES20.GlDepthBufferBit | GLES20.GlStencilBufferBit);

			// create the contexts if not done already
			if (_context == null)
			{
				var glInterface = GRGlInterface.Create();
				_context = GRContext.CreateGl(glInterface);
			}

			// manage the drawing surface
			if (_renderTarget == null || _lastSize != _newSize || !_renderTarget.IsValid)
			{
				// create or update the dimensions
				_lastSize = _newSize;

				// read the info from the buffer
				var buffer = new int[3];
				GLES20.GlGetIntegerv(GLES20.GlFramebufferBinding, buffer, 0);
				GLES20.GlGetIntegerv(GLES20.GlStencilBits, buffer, 1);
				GLES20.GlGetIntegerv(GLES20.GlSamples, buffer, 2);
				var samples = buffer[2];
				var maxSamples = _context.GetMaxSurfaceSampleCount(ColorType);
				if (samples > maxSamples)
				{
					samples = maxSamples;
				}

				_glInfo = new GRGlFramebufferInfo((uint)buffer[0], ColorType.ToGlSizedFormat());

				// destroy the old surface
				_surface?.Dispose();
				_surface = null;
				_canvas = null;

				// re-create the render target
				_renderTarget?.Dispose();
				_renderTarget = new GRBackendRenderTarget(_newSize.Width, _newSize.Height, samples, buffer[1], _glInfo);
			}

			// create the surface
			if (_surface == null)
			{
				_surface = SKSurface.Create(_context, _renderTarget, SurfaceOrigin, ColorType);
				_canvas = _surface.Canvas;
			}

			using (new SKAutoCanvasRestore(_canvas, true))
			{
				// start drawing
				var e = new SKPaintGLSurfaceEventArgs(_surface, _renderTarget, SurfaceOrigin, ColorType);
				if (surfaceView._picture is { } picture)
				{
					e.Surface.Canvas.DrawPicture(picture);
				}
			}

			// flush the SkiaSharp contents to GL
			_canvas?.Flush();
			_context.Flush();
		}

		void IRenderer.OnSurfaceChanged(IGL10? gl, int width, int height)
		{
			GLES20.GlViewport(0, 0, width, height);

			// get the new surface size
			_newSize = new SKSizeI(width, height);
		}

		void IRenderer.OnSurfaceCreated(IGL10? gl, Javax.Microedition.Khronos.Egl.EGLConfig? config)
		{
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				FreeContext();
			}
			base.Dispose(disposing);
		}

		private void FreeContext()
		{
			_surface?.Dispose();
			_surface = null;
			_renderTarget?.Dispose();
			_renderTarget = null;
			_context?.Dispose();
			_context = null;
		}
	}
}
