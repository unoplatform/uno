using System;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics.Display;
using Uno.UI.Composition.Drawing;
using Uno.UI.Hosting;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;
using Color = Windows.UI.Color;
using CompositionTarget = Microsoft.UI.Xaml.Media.CompositionTarget;

namespace Uno.UI.Runtime.Skia;

internal abstract class FrameBufferRenderer
{
	protected readonly IXamlRootHost _host;
	private readonly float _cursorRadius;
	private readonly Color _cursorColor;
	private readonly bool? _cursorVisible;
	private bool _receivedMouseEvent;

	public readonly record struct MouseIndicatorOptions(bool? ShowMouseCursor, float MouseCursorRadius, System.Drawing.Color MouseCursorColor);

	protected FrameBufferRenderer(IXamlRootHost host, MouseIndicatorOptions mouseIndicatorOptions)
	{
		_host = host;
		_cursorRadius = mouseIndicatorOptions.MouseCursorRadius;
		var c = mouseIndicatorOptions.MouseCursorColor;
		_cursorColor = Color.FromArgb(c.A, c.R, c.G, c.B);
		_cursorVisible = mouseIndicatorOptions.ShowMouseCursor;
		_receivedMouseEvent = FrameBufferPointerInputSource.Instance.ReceivedMouseEvent;
		FrameBufferPointerInputSource.Instance.MouseEventReceived += OnMouseEventReceived;
	}

	private void OnMouseEventReceived()
	{
		FrameBufferPointerInputSource.Instance.MouseEventReceived -= OnMouseEventReceived;
		_receivedMouseEvent = true;
	}

	/// <summary>The neutral render target the backend last composed into (recreated by <see cref="CreateTarget"/> on resize).</summary>
	protected abstract IRenderTarget? CurrentTarget { get; }

	private ISwapChain? _swapChain;

	/// <summary>Wires the negotiated context this renderer drives (its <c>AcquireRenderTarget</c> is routed here).</summary>
	internal void SetSwapChain(ISwapChain swapChain) => _swapChain = swapChain;

	protected void Render()
	{
		if (_host.RootElement?.Visual.CompositionTarget is not CompositionTarget ct)
		{
			throw new Exception($"CompositionTarget is not set on the {nameof(IXamlRootHost)} at the point of rendering.");
		}

		using var _ = MakeCurrent();
		var bounds = FrameBufferWindowWrapper.Instance.Size;
		var orientation = FrameBufferWindowWrapper.Instance.Orientation;
		var (degrees, transX, transY) = orientation switch
		{
			DisplayOrientations.None => (0, 0d, 0d),
			DisplayOrientations.Landscape => (0, 0d, 0d),
			DisplayOrientations.Portrait => (90, bounds.Height, 0d),
			DisplayOrientations.LandscapeFlipped => (180, bounds.Width, bounds.Height),
			DisplayOrientations.PortraitFlipped => (-90, 0d, bounds.Width),
			_ => throw new ArgumentOutOfRangeException()
		};

		var rootTransform = BuildOrientationMatrix(degrees, transX, transY);
		Action<IDrawingSession>? overlay = (_cursorVisible ?? _receivedMouseEvent) ? DrawCursor : null;

		// Route the context's acquire to this renderer's orientation-aware target creation, reusing the current
		// target while the physical size is unchanged (portrait swaps width/height for the physical framebuffer).
		((FrameBufferGraphicsContext)_swapChain!).SetAcquire((width, height) =>
		{
			if (orientation is DisplayOrientations.Portrait or DisplayOrientations.PortraitFlipped)
			{
				(width, height) = (height, width);
			}
			return CurrentTarget is { } t && t.Width == width && t.Height == height ? t : CreateTarget(width, height);
		});

		ct.OnNativePlatformFrameRequested(_swapChain!, rootTransform, overlay);
	}

	private void DrawCursor(IDrawingSession session)
	{
		var p = FrameBufferPointerInputSource.Instance.MousePosition;
		var r = _cursorRadius;
		var rect = new Rect(p.X - r, p.Y - r, 2 * r, 2 * r);
		session.DrawRoundedRect(rect, new Vector4(r, r, r, r), _cursorColor, antialias: true);
	}

	// Equivalent to a Skia canvas Translate(transX, transY) followed by RotateDegrees(degrees), packed into the
	// 2D-affine slots the neutral session's Concat(Matrix4x4) reads (M11/M12/M21/M22 = rotation, M41/M42 = translation).
	private static Matrix4x4 BuildOrientationMatrix(int degrees, double transX, double transY)
	{
		var m = Matrix3x2.CreateRotation((float)(degrees * Math.PI / 180.0)) * Matrix3x2.CreateTranslation((float)transX, (float)transY);
		return new Matrix4x4(
			m.M11, m.M12, 0, 0,
			m.M21, m.M22, 0, 0,
			/*  */ 0, /* */ 0, 1, 0,
			m.M31, m.M32, 0, 1);
	}

	public abstract void InvalidateRender();

	protected abstract IDisposable MakeCurrent();

	/// <summary>Creates the neutral render target the backend composes into for the given (physical) size.</summary>
	protected abstract IRenderTarget CreateTarget(int width, int height);

	public virtual void Dispose() { }
}
