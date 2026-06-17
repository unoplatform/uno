using System;
using System.Threading;
using Android.Content;
using Android.Graphics;
using Android.Opengl;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Autofill;
using Android.Views.InputMethods;
using AndroidX.Core.Graphics;
using AndroidX.Core.View;
using Javax.Microedition.Khronos.Opengles;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed partial class UnoSKCanvasView : GLSurfaceView, IUnoSkiaRenderView
{
	public UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	public TextInputPlugin TextInputPlugin { get; }

	private readonly InternalRenderer _renderer;

	public UnoSKCanvasView(Context context) : base(context)
	{
		SetEGLContextClientVersion(2);
		SetEGLConfigChooser(8, 8, 8, 8, 0, 8);
		SetRenderer(_renderer = new InternalRenderer());
		ExploreByTouchHelper = new UnoExploreByTouchHelper(this);
		TextInputPlugin = new TextInputPlugin(this);
		ViewCompat.SetAccessibilityDelegate(this, ExploreByTouchHelper);
		Focusable = true;
		FocusableInTouchMode = true;
		PreserveEGLContextOnPause = true;
		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
		{
			ImportantForAutofill = ImportantForAutofill.Yes;
		}

		SetWillNotDraw(false);

		RenderMode = Rendermode.WhenDirty;
	}

	public void ResetRendererContext()
	{
		_renderer.ResetContext();
	}

	public void InvalidateRender()
	{
		ExploreByTouchHelper.InvalidateRoot();
		// Request the call of IRenderer.OnDrawFrame for one frame
		RequestRender();
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
	// and modified to also add rendering without OpenGL
	private class InternalRenderer() : Java.Lang.Object, IRenderer
	{
		private const SKColorType ColorType = SKColorType.Rgba8888;
		private const GRSurfaceOrigin SurfaceOrigin = GRSurfaceOrigin.BottomLeft;

		private readonly bool _hardwareAccelerated = FeatureConfiguration.Rendering.UseOpenGLOnSkiaAndroid;

		private GRContext? _context;
		private GRGlFramebufferInfo _glInfo;
		private GRBackendRenderTarget? _renderTarget;

		private SKSurface? _glBackedSurface;
		private SKSurface? _softwareSurface;

		// When hardware accelerated, the GL backbuffer (_glBackedSurface) is a swapchain buffer that is not
		// preserved across eglSwapBuffers, so dirty rectangles render onto this persistent GPU layer which is
		// blitted to the backbuffer each frame. The software path retains its previous frame in _softwareSurface.
		private readonly RetainedLayer _retainedLayer = new();

		void IRenderer.OnDrawFrame(IGL10? gl)
		{
			GLES20.GlClear(GLES20.GlColorBufferBit | GLES20.GlDepthBufferBit | GLES20.GlStencilBufferBit);

			// create the contexts if not done already
			if (_context == null)
			{
				var glInterface = GRGlInterface.Create();
				_context = GRContext.CreateGl(glInterface);
			}

			// The frame is rendered onto a surface that retains the previous frame's contents (the persistent
			// GPU layer when hardware accelerated, the software surface otherwise); it is then blitted to the
			// non-retaining GL backbuffer below. This is what lets dirty rectangles repaint only the changed region.
			var renderSurface = _hardwareAccelerated ? _retainedLayer.Surface : _softwareSurface;
			var nativeClipPath = ((CompositionTarget)Microsoft.UI.Xaml.Window.CurrentSafe!.RootElement!.Visual.CompositionTarget!).OnNativePlatformFrameRequested(renderSurface?.Canvas,
			size =>
			{
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
				_glBackedSurface?.Dispose();
				_softwareSurface?.Dispose();
				_glBackedSurface = null;
				_softwareSurface = null;

				// re-create the render target
				_renderTarget?.Dispose();
				_renderTarget = new GRBackendRenderTarget((int)size.Width, (int)size.Height, samples, buffer[1], _glInfo);

				_glBackedSurface = SKSurface.Create(_context, _renderTarget, SurfaceOrigin, ColorType);

				if (_hardwareAccelerated)
				{
					return _retainedLayer.EnsureSurface(_context, (int)size.Width, (int)size.Height, SKColors.Transparent).Canvas;
				}

				var info = new SKImageInfo((int)size.Width, (int)size.Height, ColorType);
				_softwareSurface = SKSurface.Create(info);
				return _softwareSurface.Canvas;
			}, surfaceRetainsContents: true);

			ApplicationActivity.NativeLayerHost!.Path = nativeClipPath;

			// Blit the retained render surface onto the (non-retaining) GL backbuffer, then present.
			if (_hardwareAccelerated)
			{
				if (_glBackedSurface is not null)
				{
					_retainedLayer.Present(_glBackedSurface);
				}
			}
			else if (_glBackedSurface is not null)
			{
				var glBackedCanvas = _glBackedSurface.Canvas;
				glBackedCanvas.Clear(SKColors.Transparent);
				glBackedCanvas.DrawSurface(_softwareSurface, 0, 0);
				glBackedCanvas.Flush();
			}

			_context!.Flush();
		}

		void IRenderer.OnSurfaceChanged(IGL10? gl, int width, int height)
		{
			GLES20.GlViewport(0, 0, width, height);
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
			_retainedLayer.Dispose();
			_glBackedSurface?.Dispose();
			_glBackedSurface = null;
			_softwareSurface?.Dispose();
			_softwareSurface = null;
			_renderTarget?.Dispose();
			_renderTarget = null;
			_context?.Dispose();
			_context = null;
		}

		internal void ResetContext() => FreeContext();
	}
}
