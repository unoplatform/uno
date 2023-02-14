#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElmSharp;
using Windows.Devices.Input;
using Windows.UI.Core;
using Windows.UI.Input;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using TizenWindow = ElmSharp.Window;
using Windows.System;
using System.Threading;
using SkiaSharp.Views.Tizen;
using Windows.Graphics.Display;
using Windows.Foundation;
using Tizen.NUI;

namespace Uno.UI.Runtime.Skia
{
	internal partial class TizenCoreWindowExtension : ICoreWindowExtension
	{
		private static int _currentFrameId;

		private readonly Windows.UI.Core.CoreWindow _owner;
		private readonly ICoreWindowEvents _ownerEvents;
		private readonly DisplayInformation _displayInformation;
		private readonly GestureLayer _gestureLayer;
		private readonly UnoCanvas _canvas;

		private PointerEventArgs? _previous;

		public CoreCursor PointerCursor
		{
			get => new CoreCursor(CoreCursorType.Arrow, 0);
			set
			{
				// ignored
			}
		}

		public TizenCoreWindowExtension(object owner, UnoCanvas canvas)
		{
			_owner = (CoreWindow)owner;
			_ownerEvents = (ICoreWindowEvents)owner;
			_canvas = canvas;
			_displayInformation = DisplayInformation.GetForCurrentView();

			canvas.KeyUp += OnKeyUp;
			canvas.KeyDown += OnKeyDown;

			_gestureLayer = new GestureLayer(canvas);
			_gestureLayer.Attach(canvas);
			_gestureLayer.IsEnabled = true;
			SetupTapGesture();
			SetupMomentumGesture();
		}

		/// <inheritdoc />
		public void SetPointerCapture(PointerIdentifier pointer)
			=> this.Log().Warn("Pointer capture is not supported on Tizen");

		/// <inheritdoc />
		public void ReleasePointerCapture(PointerIdentifier pointer)
			=> this.Log().Warn("Pointer capture release is not supported on Tizen");

		private void SetupTapGesture()
		{
			_gestureLayer.SetTapCallback(GestureLayer.GestureType.Tap, GestureLayer.GestureState.Start, OnTapStart);
			_gestureLayer.SetTapCallback(GestureLayer.GestureType.Tap, GestureLayer.GestureState.End, OnTapEnd);
		}

		private void SetupMomentumGesture()
		{
			_gestureLayer.SetMomentumCallback(GestureLayer.GestureState.Move, OnMove);
		}

		private void OnMove(GestureLayer.MomentumData data)
		{
			try
			{
				var properties = BuildProperties(true, false).SetUpdateKindFromPrevious(_previous?.CurrentPoint.Properties);
				var modifiers = VirtualKeyModifiers.None;
				var point = GetPoint(data.X2, data.Y2);

				_ownerEvents.RaisePointerMoved(
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

		private static uint GetNextFrameId() => (uint)Interlocked.Increment(ref _currentFrameId);

		private void OnTapStart(GestureLayer.TapData data)
		{
			try
			{
				var properties = BuildProperties(true, false).SetUpdateKindFromPrevious(_previous?.CurrentPoint.Properties);
				var modifiers = VirtualKeyModifiers.None;
				var point = GetPoint(data.X, data.Y);

				_ownerEvents.RaisePointerPressed(
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

				_ownerEvents.RaisePointerReleased(
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

		private Windows.Foundation.Point GetPoint(int x, int y)
		{
			var scale = _displayInformation.LogicalDpi / 160f;
			return new Windows.Foundation.Point(x / scale, y / scale);
		}

		private PointerPointProperties BuildProperties(bool left, bool right)
			=> new PointerPointProperties
			{
				IsLeftButtonPressed = left,
				IsRightButtonPressed = right,
				IsPrimary = true,
				IsInRange = true,
			};
	}
}
