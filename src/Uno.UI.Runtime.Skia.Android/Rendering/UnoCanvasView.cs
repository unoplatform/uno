using Uno.UI.Composition.Drawing;
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
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed partial class UnoCanvasView : GLSurfaceView, IUnoRenderView
{
	public UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	public TextInputPlugin TextInputPlugin { get; }

	private readonly InternalRenderer _renderer;

	public UnoCanvasView(Context context) : base(context)
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
				this.Log().Error($"{nameof(UnoCanvasView)}.{nameof(OnFocusChanged)} failed", e);
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
		private ISwapChain? _context;
		private IDrawingFactory? _renderer;

		void IRenderer.OnDrawFrame(IGL10? gl)
		{
			GLES20.GlClear(GLES20.GlColorBufferBit | GLES20.GlDepthBufferBit | GLES20.GlStencilBufferBit);

			if (_context is null)
			{
				return; // context negotiated on OnSurfaceCreated; nothing to render before then
			}

			// The context wraps the ambient EGL context; the backend renders into the default framebuffer and
			// GLSurfaceView swaps implicitly (Present is a no-op).
			var ct = (CompositionTarget)Microsoft.UI.Xaml.Window.CurrentSafe!.RootElement!.Visual.CompositionTarget!;
			ct.Renderer = _renderer!;
			var nativeClipPath = ct.OnNativePlatformFrameRequested(_context);

			ApplicationActivity.NativeLayerHost!.Path = nativeClipPath;
		}

		void IRenderer.OnSurfaceChanged(IGL10? gl, int width, int height)
		{
			GLES20.GlViewport(0, 0, width, height);
		}

		void IRenderer.OnSurfaceCreated(IGL10? gl, Javax.Microedition.Khronos.Egl.EGLConfig? config)
		{
			// GLSurfaceView has just made-current the EGL context on its render thread; negotiate here so the
			// backend's GRContext-GLES is built on the GL thread against the current context. UseOpenGLOnSkiaAndroid
			// picks GLES (ambient context) vs CPU raster blitted to the GL framebuffer.
			var useGL = FeatureConfiguration.Rendering.UseOpenGLOnSkiaAndroid;
			GraphicsRegistry.ContextFactory = kind => System.Threading.Tasks.Task.FromResult<ISwapChain?>(
				useGL
					? (kind == GraphicsContextKind.OpenGLES ? new AndroidGLGraphicsContext() : null)
					: (kind == GraphicsContextKind.Software ? new AndroidSoftwareGraphicsContext() : null));
			var init = GraphicsRegistry.Initialize();
			// OnSurfaceCreated fires again after a genuine EGL context loss (despite PreserveEGLContextOnPause);
			// dispose the previous context before rebinding so re-negotiation doesn't leak it.
			_context?.Dispose();
			_context = init.Context;
			_renderer = init.Renderer;
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
			_context?.Dispose();
			_context = null;
		}

		internal void ResetContext() => FreeContext();
	}
}
