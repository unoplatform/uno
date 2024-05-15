#nullable enable

using System;
using ElmSharp;
using Windows.Devices.Input;
using Windows.UI.Core;
using Windows.UI.Input;
using Uno.Foundation.Logging;
using Windows.System;
using System.Threading;
using Windows.Graphics.Display;
using Windows.Foundation;
using System.Runtime.CompilerServices;

namespace Uno.UI.Runtime.Skia;

internal partial class TizenCorePointerInputSource : IUnoCorePointerInputSource
{
	private static int _currentFrameId;

	private readonly Logger _log;
	private readonly bool _isTraceEnabled;
	private readonly DisplayInformation _displayInformation;
	private readonly GestureLayer _gestureLayer;

	private PointerEventArgs? _previous;

#pragma warning disable CS0067 // Some event are not raised on Tizen ... yet!
	public event TypedEventHandler<object, PointerEventArgs>? PointerCaptureLost;
	public event TypedEventHandler<object, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs>? PointerWheelChanged;
	public event TypedEventHandler<object, PointerEventArgs>? PointerCancelled;
#pragma warning restore CS0067

	public TizenCorePointerInputSource(UnoCanvas canvas)
	{
		_log = this.Log();
		_isTraceEnabled = _log.IsEnabled(LogLevel.Trace);

		_displayInformation = DisplayInformation.GetForCurrentView();

		_gestureLayer = new GestureLayer(canvas);
		_gestureLayer.Attach(canvas);
		_gestureLayer.IsEnabled = true;
		_gestureLayer.SetTapCallback(GestureLayer.GestureType.Tap, GestureLayer.GestureState.Start, OnTapStart);
		_gestureLayer.SetTapCallback(GestureLayer.GestureType.Tap, GestureLayer.GestureState.End, OnTapEnd);
		_gestureLayer.SetMomentumCallback(GestureLayer.GestureState.Move, OnMove);

	}

	/// <inheritdoc />
	[NotImplemented]
	public CoreCursor PointerCursor
	{
		get => new(CoreCursorType.Arrow, 0);
		set => LogNotSupported();
	}

	/// <inheritdoc />
	[NotImplemented]
	public bool HasCapture => false;

	/// <inheritdoc />
	[NotImplemented]
	public Windows.Foundation.Point PointerPosition => default;

	/// <inheritdoc />
	[NotImplemented]
	public void SetPointerCapture(PointerIdentifier pointer)
		=> LogNotSupported();

	/// <inheritdoc />
	[NotImplemented]
	public void SetPointerCapture()
		=> LogNotSupported();

	/// <inheritdoc />
	[NotImplemented]
	public void ReleasePointerCapture(PointerIdentifier pointer)
		=> LogNotSupported();

	/// <inheritdoc />
	[NotImplemented]
	public void ReleasePointerCapture()
		=> LogNotSupported();

	private void OnMove(GestureLayer.MomentumData data)
	{
		try
		{
			var properties = BuildProperties(true, false).SetUpdateKindFromPrevious(_previous?.CurrentPoint.Properties);
			var modifiers = VirtualKeyModifiers.None;
			var point = GetPoint(data.X2, data.Y2);

			RaisePointerMoved(
				_previous = new PointerEventArgs(
					new Windows.UI.Input.PointerPoint(
						frameId: GetNextFrameId(),
						timestamp: Math.Max(data.VerticalSwipeTimestamp, data.HorizontalSwipeTimestamp),
						device: PointerDevice.For(PointerDeviceType.Touch),
						pointerId: 1,
						rawPosition: point,
						position: point,
						isInContact: properties.HasPressedButton,
						properties: properties
					),
					modifiers
				)
			);
		}
		catch (Exception e)
		{
			this.Log().Error("Failed to raise PointerMoved", e);
		}
	}

	private void OnTapStart(GestureLayer.TapData data)
	{
		try
		{
			var properties = BuildProperties(true, false).SetUpdateKindFromPrevious(_previous?.CurrentPoint.Properties);
			var modifiers = VirtualKeyModifiers.None;
			var point = GetPoint(data.X, data.Y);

			RaisePointerPressed(
				_previous = new PointerEventArgs(
					new Windows.UI.Input.PointerPoint(
						frameId: GetNextFrameId(),
						timestamp: (uint)data.Timestamp,
						device: PointerDevice.For(PointerDeviceType.Touch),
						pointerId: 1,
						rawPosition: point,
						position: point,
						isInContact: properties.HasPressedButton,
						properties: properties
					),
					modifiers
				)
			);
		}
		catch (Exception e)
		{
			this.Log().Error("Failed to raise PointerPressed", e);
		}
	}

	private void OnTapEnd(GestureLayer.TapData data)
	{
		try
		{
			var properties = BuildProperties(false, false).SetUpdateKindFromPrevious(_previous?.CurrentPoint.Properties);
			var modifiers = VirtualKeyModifiers.None;
			var point = GetPoint(data.X, data.Y);

			RaisePointerReleased(
				_previous = new PointerEventArgs(
					new Windows.UI.Input.PointerPoint(
						frameId: GetNextFrameId(),
						timestamp: (uint)data.Timestamp,
						device: PointerDevice.For(PointerDeviceType.Touch),
						pointerId: 1,
						rawPosition: point,
						position: point,
						isInContact: properties.HasPressedButton,
						properties: properties
					),
					modifiers
				)
			);
		}
		catch (Exception e)
		{
			this.Log().Error("Failed to raise PointerReleased", e);
		}
	}

	private static uint GetNextFrameId() => (uint)Interlocked.Increment(ref _currentFrameId);

	private Windows.Foundation.Point GetPoint(int x, int y)
	{
		var scale = _displayInformation.LogicalDpi / 160f;
		return new Windows.Foundation.Point(x / scale, y / scale);
	}

	private PointerPointProperties BuildProperties(bool left, bool right)
		=> new()
		{
			IsLeftButtonPressed = left,
			IsRightButtonPressed = right,
			IsPrimary = true,
			IsInRange = true,
		};

	private void RaisePointerPressed(PointerEventArgs ptArgs, [CallerMemberName] string caller = "")
	{
		if (_isTraceEnabled)
		{
			_log.Trace($"[Pressed] ({caller}) {ptArgs}");
		}

		PointerPressed?.Invoke(this, ptArgs);
	}

	private void RaisePointerReleased(PointerEventArgs ptArgs, [CallerMemberName] string caller = "")
	{
		if (_isTraceEnabled)
		{
			_log.Trace($"[Released] ({caller}) {ptArgs}");
		}

		PointerReleased?.Invoke(this, ptArgs);
	}

	private void RaisePointerMoved(PointerEventArgs ptArgs, [CallerMemberName] string caller = "")
	{
		if (_isTraceEnabled)
		{
			_log.Trace($"[Moved] ({caller}) {ptArgs}");
		}

		PointerMoved?.Invoke(this, ptArgs);
	}

	private void LogNotSupported([CallerMemberName] string member = "")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{member} not supported on Tizen.");
		}
	}
}
