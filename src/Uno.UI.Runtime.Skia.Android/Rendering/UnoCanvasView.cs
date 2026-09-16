using Uno.UI.Composition.Drawing;
using System;
using System.Diagnostics.CodeAnalysis;
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
using Uno.UI.Xaml.Controls;
using Windows.Graphics.Display;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed partial class UnoCanvasView : GLSurfaceView, IUnoRenderView
{
	public UnoExploreByTouchHelper ExploreByTouchHelper { get; }
	public TextInputPlugin TextInputPlugin { get; }

	// Matches the Vulkan render loop's wake interval, so both backends retry a skipped frame
	// at the same cadence while the window isn't ready.
	private const long RenderRetryDelayMs = 100;

	private readonly ApplicationActivity _activity;
	private readonly InternalRenderer _renderer;

	public UnoCanvasView(ApplicationActivity activity) : base(activity)
	{
		_activity = activity;
		SetEGLContextClientVersion(2);
		SetEGLConfigChooser(8, 8, 8, 8, 0, 8);
		SetRenderer(_renderer = new InternalRenderer(this));
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

	public void TeardownRenderer()
	{
		// GLSurfaceView drives IRenderer.OnDrawFrame on its own GL thread, so freeing the Skia and
		// GL state from the UI thread can race a frame in flight. Queue the teardown there and wait
		// for it; the renderer refuses to rebuild its context afterwards.
		var torndown = new ManualResetEventSlim(false);

		QueueEvent(new Java.Lang.Runnable(() =>
		{
			try
			{
				_renderer.TeardownOnRenderThread();
			}
			finally
			{
				torndown.Set();
			}
		}));

		if (torndown.Wait(TimeSpan.FromSeconds(2)))
		{
			torndown.Dispose();
		}
		else if (this.Log().IsEnabled(LogLevel.Warning))
		{
			// Either the GL thread is gone (the surface was destroyed first, so the queued work never
			// runs) or it is merely slow and will still signal this event, which is why it is left to
			// the GC rather than disposed from under it.
			this.Log().Warn("The GL thread did not run the renderer teardown within the timeout.");
		}
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
				textBox.Text = autofillValue.TextValue ?? string.Empty;
			}
		}
	}

	public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
		=> TextInputPlugin.OnCreateInputConnection(outAttrs!);

	// Copied from https://github.com/mono/SkiaSharp/blob/main/source/SkiaSharp.Views/SkiaSharp.Views/Platform/Android/SKGLSurfaceView.cs
	// and modified to also add rendering without OpenGL
	private class InternalRenderer(UnoCanvasView view) : Java.Lang.Object, IRenderer
	{
		private readonly UnoCanvasView _view = view;
		private readonly ApplicationActivity _activity = view._activity;

		private bool _torndown;

		private ISwapChain? _context;
		private IDrawingFactory? _renderer;

		void IRenderer.OnDrawFrame(IGL10? gl)
		{
			if (_torndown)
			{
				// A frame queued before the teardown ran would rebuild the context below, leaving GL
				// state behind with nothing left to present it.
				return;
			}

			GLES20.GlClear(GLES20.GlColorBufferBit | GLES20.GlDepthBufferBit | GLES20.GlStencilBufferBit);

			// Negotiating lazily here keeps a lost race self-healing: ResetContext() runs on the UI thread when the
			// activity re-parents this view, and it can land after OnSurfaceCreated has already fired on the GL
			// thread — without this every later frame would return and the app would freeze on its last frame.
			if (_context is null)
			{
				EnsureContext();
			}

			if (_activity.RootElement?.Visual.CompositionTarget is not CompositionTarget compositionTarget)
			{
				// The window isn't ready (e.g. mid teardown during activity re-creation). Skipping is
				// only safe if we re-arm: OnNativePlatformFrameRequested below is the only thing that
				// clears the target's RenderRequested flag, so a bare return would make every later
				// RequestNewFrame a no-op and the window would never repaint again. RenderMode is
				// WhenDirty, so re-request on a delay rather than spinning the GL thread.
				_view.PostDelayed(_view.RequestRender, RenderRetryDelayMs);
				return;
			}

			// The context wraps the ambient EGL context; the backend renders into the default framebuffer and
			// GLSurfaceView swaps implicitly (Present is a no-op).
			compositionTarget.Renderer = _renderer!;
			var nativeClipPath = compositionTarget.OnNativePlatformFrameRequested(_context);

			if (_activity.NativeLayerHost is { } nativeLayerHost)
			{
				nativeLayerHost.Path = nativeClipPath;
			}

			if (_activity.Wrapper.TryReleaseFirstFrameGate())
			{
				// Trigger OnPreDraw re-evaluation so the splash can dismiss once the first frame is on screen
				_activity.RelativeLayout?.Post(() =>
					_activity.RelativeLayout?.Invalidate());
			}
		}

		void IRenderer.OnSurfaceChanged(IGL10? gl, int width, int height)
		{
			GLES20.GlViewport(0, 0, width, height);
		}

		void IRenderer.OnSurfaceCreated(IGL10? gl, Javax.Microedition.Khronos.Egl.EGLConfig? config)
		{
			if (_torndown)
			{
				return;
			}

			// Fires again after a genuine EGL context loss (despite PreserveEGLContextOnPause), so the previous
			// backend and context must go before re-negotiating against the new one.
			FreeContext();
			EnsureContext();
		}

		/// <summary>
		/// Negotiates the backend on the GL thread, where GLSurfaceView has made its EGL context current — the
		/// backend's GRContext-GLES has to be built against that current context. UseOpenGLOnSkiaAndroid picks
		/// GLES (ambient context) vs CPU raster blitted to the GL framebuffer.
		/// </summary>
		[MemberNotNull(nameof(_context), nameof(_renderer))]
		private void EnsureContext()
		{
			var useGL = FeatureConfiguration.Rendering.UseOpenGLOnSkiaAndroid;
			GraphicsRegistry.ContextFactory = kind => System.Threading.Tasks.Task.FromResult<ISwapChain?>(
				useGL
					? (kind == GraphicsContextKind.OpenGLES ? new AndroidGLGraphicsContext() : null)
					: (kind == GraphicsContextKind.Software ? new AndroidSoftwareGraphicsContext() : null));
			var init = GraphicsRegistry.Initialize();
			_context = init.Context;
			_renderer = init.Renderer;
			// Effect brushes read this while recording, so it must be set as soon as the renderer is known.
			Microsoft.UI.Composition.Compositor.GetSharedCompositor().IsSoftwareRenderer = init.Context.Kind == GraphicsContextKind.Software;
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
			// The backend holds the GRContext-GLES built over this context, so it goes first; leaving it behind
			// leaks a GPU context per re-negotiation.
			(_renderer as IDisposable)?.Dispose();
			_renderer = null;

			_context?.Dispose();
			_context = null;
		}

		internal void ResetContext() => FreeContext();

		/// <summary>
		/// Frees the GL and Skia state from the thread that owns it. Must run on the GL thread,
		/// which is where every other access to these objects happens.
		/// </summary>
		internal void TeardownOnRenderThread()
		{
			_torndown = true;
			FreeContext();
			Dispose();
		}
	}
}
