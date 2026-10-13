using System;
using System.Threading;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics;
using Windows.UI.Core;
using Uno.Extensions;
using Uno.UI.Dispatching;
using Uno.UI.Runtime.Skia;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

internal class FrameBufferWindowWrapper : NativeWindowWrapperBase
{
	private static FrameBufferWindowWrapper? _instance;
	internal static FrameBufferWindowWrapper Instance => _instance!;
	internal static FrameBufferWindowWrapper? InstanceOrNull => _instance;

	public static void Init(DisplayOrientations orientation) => _instance = new(orientation);

	public override object? NativeWindow => null;

	// Orientation and the unrotated screen size are swapped together, so the render and input threads never pair a
	// new orientation with a stale size.
	private DisplayState _displayState;

	private FrameBufferWindowWrapper(DisplayOrientations orientation)
	{
		if (_instance != null)
		{
			throw new InvalidOperationException($"{nameof(FrameBufferWindowWrapper)} should be created once.");
		}
		_instance = this;

		_displayState = new(orientation, default);
	}

	internal DisplayState CurrentDisplayState => Volatile.Read(ref _displayState);

	public DisplayOrientations Orientation => CurrentDisplayState.Orientation;

	internal void SetSize(Size rawScreenSize)
	{
		// The renderer reads the physical size from its first frame on, ahead of the window being attached.
		UpdateDisplayState(state => state with { PhysicalSize = rawScreenSize });

		if (XamlRoot is { })
		{
			ApplyBounds();
		}
		else
		{
			NativeDispatcher.Main.Enqueue(() => SetSize(rawScreenSize));
		}
	}

	private void UpdateDisplayState(Func<DisplayState, DisplayState> update)
	{
		DisplayState current, updated;
		do
		{
			current = CurrentDisplayState;
			updated = update(current);
		}
		while (!ReferenceEquals(Interlocked.CompareExchange(ref _displayState, updated, current), current));
	}

	/// <summary>Rotates the window to <paramref name="orientation"/>. Must be called on the UI thread.</summary>
	internal void SetOrientation(DisplayOrientations orientation)
	{
		NativeDispatcher.CheckThreadAccess();

		if (Orientation == orientation)
		{
			return;
		}

		UpdateDisplayState(state => state with { Orientation = orientation });

		// Until the window is attached there is no layout to update or listener to notify, and until the screen size
		// is known SetSize has yet to run: either applies the bounds for whichever orientation is current by then.
		if (XamlRoot is { } xamlRoot)
		{
			if (CurrentDisplayState.HasPhysicalSize)
			{
				ApplyBounds();

				// Bounds are unchanged on a square screen or a 180° turn, so nothing else would redraw with the new rotation.
				xamlRoot.VisualTree.RootElement.Visual.CompositionTarget?.RequestNewFrame();
			}

			DisplayInformation.GetForCurrentViewSafe().NotifyOrientationChanged();
		}
	}

	private void ApplyBounds()
	{
		var state = CurrentDisplayState;
		var scale = RasterizationScale = (float)DisplayInformation.GetForCurrentViewSafe().RawPixelsPerViewPixel;
		var orientedSize = state.OrientedSize;
		var bounds = new Rect(0, 0, orientedSize.Width / scale, orientedSize.Height / scale);
		SetBoundsAndVisibleBounds(bounds, bounds);
		var fullSize = new SizeInt32((int)orientedSize.Width, (int)orientedSize.Height);
		SetSizes(fullSize, fullSize);
		FrameBufferPointerInputSource.Instance.MousePosition = bounds.GetCenter();
	}

	/// <param name="PhysicalSize">The screen size in raw pixels, before rotation.</param>
	internal sealed record DisplayState(DisplayOrientations Orientation, Size PhysicalSize)
	{
		public bool HasPhysicalSize => PhysicalSize.Width > 0 && PhysicalSize.Height > 0;

		public bool IsPortrait => Orientation is DisplayOrientations.Portrait or DisplayOrientations.PortraitFlipped;

		/// <summary>The screen size in raw pixels as the app sees it, after rotation.</summary>
		public Size OrientedSize => IsPortrait ? new Size(PhysicalSize.Height, PhysicalSize.Width) : PhysicalSize;
	}

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosing();
}
