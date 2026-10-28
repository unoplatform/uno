#nullable enable

using System;
using System.Numerics;
using System.Threading;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Composition.Drawing;

namespace Uno.WinUI.Graphics3DWebGpu;

/// <summary>
/// A <see cref="FrameworkElement"/> that displays what its inheritor renders with Silk.NET.WebGPU, on the device the app
/// renders with.
/// </summary>
/// <remarks>
/// Only available when the app runs the WebGPU backend. Rendering goes into a texture on the backend's own device,
/// which is then composited with no copy. With any other backend <see cref="IsWebGpuInitialized"/> becomes false
/// and <see cref="OnWebGpuUnavailable"/> is called.
/// </remarks>
public abstract partial class WebGpuCanvasElement : Grid
{
	private WebGpuCanvasVisual? _canvasVisual;

	// Valid while initialized: from the first paint with a WebGPU backend until the element is unloaded.
	private IWebGpuTextureInterop? _interop;
	private WebGpuCanvasContext? _context;
	private ITexture? _target;

	private bool _renderPending = true;
	private bool _failed;
	private bool _unavailableReported;
	private bool _changingInitialized;
	private int _invalidatePending;

	protected WebGpuCanvasElement()
	{
		Unloaded += (_, _) =>
		{
			Destroy();
			_failed = false;
			_unavailableReported = false;
			IsWebGpuInitialized = null;
		};
	}

	/// <summary>
	/// Use this function for the initial setup, e.g. creating pipelines, buffers and bind groups.
	/// </summary>
	/// <remarks>
	/// <see cref="Init"/> might be called multiple times. Every call except the first one is preceded by a call to
	/// <see cref="OnDestroy"/>. The target texture doesn't exist yet: <see cref="WebGpuCanvasContext.Target"/> is null.
	/// </remarks>
	protected abstract void Init(WebGpuCanvasContext context);

	/// <summary>
	/// Use this function to release the resources created in <see cref="Init"/>.
	/// </summary>
	/// <remarks>
	/// Every call to <see cref="OnDestroy"/> is preceded by a call to <see cref="Init"/>.
	/// </remarks>
	protected abstract void OnDestroy(WebGpuCanvasContext context);

	/// <summary>
	/// The rendering logic goes here: encode commands that render into <see cref="WebGpuCanvasContext.TargetView"/>
	/// and submit them to <see cref="WebGpuCanvasContext.Queue"/>.
	/// </summary>
	/// <remarks>
	/// The target texture is sized to the element in physical pixels and is displayed as premultiplied-alpha color.
	/// Its content is kept between calls, so clear it with the render pass's load operation if needed.
	/// <see cref="RenderOverride"/> is called once after each <see cref="Invalidate"/> and after the element is
	/// resized. To animate, call <see cref="Invalidate"/> from within <see cref="RenderOverride"/>.
	/// </remarks>
	protected abstract void RenderOverride(WebGpuCanvasContext context);

	/// <summary>
	/// Called when this element cannot render because the app isn't running the WebGPU backend. An inheritor with
	/// another way to draw switches to it here.
	/// </summary>
	protected virtual void OnWebGpuUnavailable() { }

	public static DependencyProperty IsWebGpuInitializedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsWebGpuInitialized),
			typeof(bool?),
			typeof(WebGpuCanvasElement),
			new PropertyMetadata(null, (PropertyChangedCallback)((d, _) =>
			{
				var element = (WebGpuCanvasElement)d;
				if (!element._changingInitialized)
				{
					throw new InvalidOperationException($"{nameof(WebGpuCanvasElement)}.{nameof(IsWebGpuInitializedProperty)} is read-only.");
				}

				element._changingInitialized = false;
			})));

	/// <summary>
	/// Whether the element is rendering with WebGPU. Null until the element is first drawn and after it is unloaded.
	/// False when the app isn't running the WebGPU backend, or after an exception escaped <see cref="Init"/> or
	/// <see cref="RenderOverride"/>, in which case the element stops rendering until it is reloaded.
	/// </summary>
	public bool? IsWebGpuInitialized
	{
		get => (bool?)GetValue(IsWebGpuInitializedProperty);
		private set
		{
			_changingInitialized = true;
			SetValue(IsWebGpuInitializedProperty, value);
		}
	}

	private protected override ContainerVisual CreateElementVisual()
		=> _canvasVisual = new WebGpuCanvasVisual(this, ElementVisualCompositor);

	internal override bool IsViewHit() => true;

	/// <summary>
	/// Queues a call to <see cref="RenderOverride"/> for the next frame.
	/// </summary>
	/// <remarks>
	/// Safe to call from any thread. Off the UI thread the request is posted, and several requests made before it
	/// runs are coalesced into one.
	/// </remarks>
	public void Invalidate()
	{
		if (DispatcherQueue is not { } dispatcher || dispatcher.HasThreadAccess)
		{
			InvalidateCore();
			return;
		}

		if (Interlocked.Exchange(ref _invalidatePending, 1) == 0)
		{
			dispatcher.TryEnqueue(() =>
			{
				Volatile.Write(ref _invalidatePending, 0);
				InvalidateCore();
			});
		}
	}

	private void InvalidateCore()
	{
		_renderPending = true;
		_canvasVisual?.Invalidate();
	}

	// Runs while the frame is recorded, so commands submitted here reach the queue before that frame does.
	internal void Paint(IDrawingSession session, Vector2 size)
	{
		if (_failed || size.X < 1 || size.Y < 1)
		{
			return;
		}

		if (session.Factory is not IWebGpuTextureInterop interop)
		{
			ReportUnavailable(session.Factory);
			return;
		}

		if (!ReferenceEquals(interop, _interop))
		{
			Destroy();
			if (!TryInitialize(interop))
			{
				return;
			}
		}

		var m = session.TotalMatrix;
		var scaleX = MathF.Max(1e-3f, new Vector2(m.M11, m.M12).Length());
		var scaleY = MathF.Max(1e-3f, new Vector2(m.M21, m.M22).Length());
		var width = Math.Max(1, (int)MathF.Ceiling(size.X * scaleX));
		var height = Math.Max(1, (int)MathF.Ceiling(size.Y * scaleY));

		var context = _context!;
		if (_target is null || context.PixelWidth != width || context.PixelHeight != height)
		{
			// A frame recorded earlier may still draw the old texture; disposing defers the release past it.
			_target?.Dispose();
			_target = context.CreateTarget(interop, width, height);
			_renderPending = true;
		}

		if (_renderPending)
		{
			_renderPending = false;
			try
			{
				RenderOverride(context);
			}
			catch (Exception e)
			{
				Fail($"{nameof(WebGpuCanvasElement)} rendering failed. The element will no longer render.", e);
				return;
			}
		}

		session.Save();
		session.Scale(1f / scaleX, 1f / scaleY);
		session.DrawImage(_target, 0f, 0f);
		session.Restore();
	}

	private bool TryInitialize(IWebGpuTextureInterop interop)
	{
		var context = new WebGpuCanvasContext(interop);
		try
		{
			Init(context);
		}
		catch (Exception e)
		{
			context.Release();
			Fail($"{nameof(WebGpuCanvasElement)} initialization failed. The element will not render.", e);
			return false;
		}

		_interop = interop;
		_context = context;
		_renderPending = true;
		SetInitializedAfterPaint(true);
		return true;
	}

	private void Destroy()
	{
		if (_context is { } context)
		{
			try
			{
				OnDestroy(context);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(WebGpuCanvasElement)}.{nameof(OnDestroy)} failed.", e);
				}
			}

			context.Release();
		}

		_target?.Dispose();
		_target = null;
		_context = null;
		_interop = null;
	}

	private void Fail(string message, Exception e)
	{
		if (this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error(message, e);
		}

		_failed = true;
		SetInitializedAfterPaint(false);
	}

	private void ReportUnavailable(IDrawingFactory factory)
	{
		if (_unavailableReported)
		{
			return;
		}

		_unavailableReported = true;
		if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"{nameof(WebGpuCanvasElement)} requires the WebGPU backend, but the app renders with {factory.GetType().Name}. The element will not render.");
		}

		SetInitializedAfterPaint(false, notifyUnavailable: true);
	}

	// Paint runs while the frame is recorded; property changes and user callbacks wait until it is done.
	private void SetInitializedAfterPaint(bool value, bool notifyUnavailable = false)
		=> DispatcherQueue?.TryEnqueue(() =>
		{
			if (!IsLoaded)
			{
				return;
			}

			IsWebGpuInitialized = value;
			if (notifyUnavailable)
			{
				OnWebGpuUnavailable();
			}
		});
}
