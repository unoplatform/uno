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

internal sealed class UnoSKCanvasView : GLSurfaceView
{
	private readonly SkiaRenderHelper.FpsHelper _fpsHelper = new();
	private SKPicture? _picture;

	internal UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	internal TextInputPlugin TextInputPlugin { get; }

	internal static UnoSKCanvasView? Instance { get; private set; }

	private readonly InternalRenderer _renderer;

	public UnoSKCanvasView(Context context) : base(context)
	{
		SetEGLContextClientVersion(2);
		SetEGLConfigChooser(8, 8, 8, 8, 0, 8);
		SetRenderer(_renderer = new InternalRenderer(this));
		Instance = this;
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

		// Start the render thread paused
		RenderMode = Rendermode.WhenDirty;

		CompositionTarget.RenderingActiveChanged += OnCompositionRenderingActiveChanged;
	}

	private void OnCompositionRenderingActiveChanged()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"OnCompositionRenderingActiveChanged: {CompositionTarget.IsRenderingActive}");
		}

		RenderMode = CompositionTarget.IsRenderingActive
			? Rendermode.Continuously
			: Rendermode.WhenDirty;
	}

	internal void ResetRendererContext()
	{
		_renderer.ResetContext();
	}

	internal void InvalidateRender()
	{
		if (Microsoft.UI.Xaml.Window.CurrentSafe is not { RootElement: { } root } window)
		{
			return;
		}

		if (!SkiaRenderHelper.CanRecordPicture(root))
		{
			root?.XamlRoot?.QueueInvalidateRender();
			return;
		}

		ExploreByTouchHelper.InvalidateRoot();

		var (picture, path) = SkiaRenderHelper.RecordPictureAndReturnPath(
			(int)window.Bounds.Width,
			(int)window.Bounds.Height,
			root,
			invertPath: false);

		var scale = root.XamlRoot is { } xamlRoot
			? xamlRoot.RasterizationScale
			: 1.0f;

		_fpsHelper.Scale = (float)scale;

		if (ApplicationActivity.NativeLayerHost is { } nativeLayerHost)
		{
			nativeLayerHost.Path = path;
			nativeLayerHost.Invalidate();
		}

		Interlocked.Exchange(ref _picture, picture);

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
	private class InternalRenderer(UnoSKCanvasView surfaceView) : Java.Lang.Object, IRenderer
	{
		private const SKColorType ColorType = SKColorType.Rgba8888;
		private const GRSurfaceOrigin SurfaceOrigin = GRSurfaceOrigin.BottomLeft;

		private readonly bool _hardwareAccelerated = FeatureConfiguration.Rendering.UseOpenGLOnSkiaAndroid;

		private GRContext? _context;
		private GRGlFramebufferInfo _glInfo;
		private GRBackendRenderTarget? _renderTarget;

		private SKSurface? _glBackedSurface;
		private SKSurface? _softwareSurface;

		private SKSizeI _lastSize;
		private SKSizeI _newSize;

		void IRenderer.OnDrawFrame(IGL10? gl)
		{
			var currentPicture = Volatile.Read(ref surfaceView._picture);

			GLES20.GlClear(GLES20.GlColorBufferBit | GLES20.GlDepthBufferBit | GLES20.GlStencilBufferBit);

			if (currentPicture is null)
			{
				return;
			}

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
				_glBackedSurface?.Dispose();
				_softwareSurface?.Dispose();
				_glBackedSurface = null;
				_softwareSurface = null;

				// re-create the render target
				_renderTarget?.Dispose();
				_renderTarget = new GRBackendRenderTarget(_newSize.Width, _newSize.Height, samples, buffer[1], _glInfo);
			}

			// create the surface
			if (_glBackedSurface == null)
			{
				_glBackedSurface = SKSurface.Create(_context, _renderTarget, SurfaceOrigin, ColorType);
			}

			if (!_hardwareAccelerated && _softwareSurface is null)
			{
				var info = new SKImageInfo(_newSize.Width, _lastSize.Height, ColorType);
				_softwareSurface = SKSurface.Create(info);
			}

			var surface = _hardwareAccelerated ? _glBackedSurface : _softwareSurface;

			if (surface is not null)
			{
				SkiaRenderHelper.RenderPicture(
					surface,
					currentPicture,
					SKColors.Transparent,
					surfaceView._fpsHelper);
			}

			if (!_hardwareAccelerated)
			{
				var glBackedCanvas = _glBackedSurface.Canvas;
				glBackedCanvas.Clear(SKColors.Transparent);
				glBackedCanvas.DrawSurface(_softwareSurface, 0, 0);
				glBackedCanvas.Flush();
			}
			// else : we already drew directly on the OpenGL-backed canvas

			_context!.Flush();

			if (!CompositionTarget.IsRenderingActive && ReferenceEquals(currentPicture, Volatile.Read(ref surfaceView._picture)))
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace("Suspend drawing");
				}

				surfaceView.RenderMode = Rendermode.WhenDirty;
			}

			Microsoft.UI.Xaml.Window.CurrentSafe?.RootElement?.XamlRoot?.InvokeFrameRendered();
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
			_glBackedSurface?.Dispose();
			_glBackedSurface = null;
			_renderTarget?.Dispose();
			_renderTarget = null;
			_context?.Dispose();
			_context = null;
		}

		internal void ResetContext() => FreeContext();
	}
}
