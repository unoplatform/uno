using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Input
{
	public  partial class GestureRecognizer 
	{
		private readonly ILogger _log;
		private IDictionary<uint, List<PointerPoint>> _activePointers = new Dictionary<uint, List<PointerPoint>>();

		public GestureSettings GestureSettings { get; set; }

		public bool IsActive => _activePointers.Count > 0;

		public GestureRecognizer()
		{
			_log = this.Log();
		}

		public void ProcessDownEvent(PointerPoint value)
		{
			if (TryRecognizeMultiTap(value))
			{
				// This is how UWP behaves, it will fire the double tap
				// on Down for a double tap instead of the Up.

				return;
			}

			_activePointers[value.PointerId] = new List<PointerPoint>(16) { value }; ;
		}

		public void ProcessMoveEvents(IList<PointerPoint> value)
		{
			foreach (var point in value)
			{
				if (_activePointers.TryGetValue(point.PointerId, out var points))
				{
					points.Add(point);
				}
				else if (_log.IsEnabled(LogLevel.Information))
				{
					// info: We might get some PointerMove for mouse even if not pressed!
					_log.Info("Received a 'Move' for a pointer which was not considered as down. Ignoring event.");
				}
			}
		}
		public void ProcessUpEvent(PointerPoint value)
		{
#if NET461 || __WASM__
			if (_activePointers.TryGetValue(value.PointerId, out var points))
			{
				_activePointers.Remove(value.PointerId);
#else
			if (_activePointers.Remove(value.PointerId, out var points))
			{
#endif

				// Note: At this point we MAY be IsActive == false, which is the expected behavior (same as UWP)
				//		 even if we will fire some events now.

				Recognize(points, pointerUp: value);
			}
			else if (_log.IsEnabled(LogLevel.Error))
			{
				_log.Error("Received a 'Up' for a pointer which was not considered as down. Ignoring event.");
			}
		}

		public void CompleteGesture()
		{
			if (!IsActive)
			{
				return;
			}

			// Capture the list in order to avoid alteration while enumerating
			var current = _activePointers;
			_activePointers = new Dictionary<uint, List<PointerPoint>>();

			// Note: At this point we are IsActive == false, which is the expected behavior (same as UWP)
			//		 even if we will fire some events now.

			// Complete all pointers
			foreach (var points in current.Values)
			{
				Recognize(points, pointerUp: null);
			}
		}

		private void Recognize(List<PointerPoint> points, PointerPoint pointerUp)
		{
			if (points.Count == 0)
			{
				return;
			}

			var recognized = TryRecognizeTap(points, pointerUp);

			if (!recognized && _log.IsEnabled(LogLevel.Information))
			{
				_log.Info("No gesture recognized");
			}
		}

		#region Tap (includes DoubleTap)
		private const ulong _multiTapMaxDelay = TimeSpan.TicksPerMillisecond * 100;
		private const int _tapMaxXDelta = 15;
		private const int _tapMaxYDelta = 15;

		public event TypedEventHandler<GestureRecognizer, TappedEventArgs> Tapped;

		private (ulong, ulong) _lastSingleTap;

		public bool CanBeDoubleTap(PointerPoint value)
		{
			if (!GestureSettings.HasFlag(GestureSettings.DoubleTap))
			{
				return false;
			}

			var (lastTapId, lastTapTs) = _lastSingleTap;
			if (lastTapTs == 0)
			{
				return false;
			}

			var currentId = GetPointerIdentifier(value);
			var currentTs = value.Timestamp;

			return lastTapId == currentId && currentTs - lastTapTs <= _multiTapMaxDelay;
		}

		private bool TryRecognizeTap(List<PointerPoint> points, PointerPoint pointerUp = null)
		{
			if (pointerUp == null)
			{
				return false;
			}

			var start = points[0];
			var startIdentifier = GetPointerIdentifier(start);

			// Validate tap gesture
			// Note: There is no limit for the duration of the tap!
			for (var i = 1; i < points.Count; i++)
			{
				var point = points[i];
				var pointIdentifier = GetPointerIdentifier(point);

				if (
					// The pointer changed (left vs right click)
					pointIdentifier != startIdentifier

					// Pointer moved to far
					|| Math.Abs(point.Position.X - start.Position.X) > _tapMaxXDelta
					|| Math.Abs(point.Position.Y - start.Position.Y) > _tapMaxYDelta
				)
				{
					return false;
				}
			}
			// For the pointer up, we check only the distance, as it's expected that the IsLeftButtonPressed changed!
			if (Math.Abs(pointerUp.Position.X - start.Position.X) > _tapMaxXDelta
				|| Math.Abs(pointerUp.Position.Y - start.Position.Y) > _tapMaxYDelta)
			{
				return false;
			}

			_lastSingleTap = (startIdentifier, start.Timestamp);
			Tapped?.Invoke(this, new TappedEventArgs(start.PointerDevice.PointerDeviceType, start.Position, tapCount: 1));

			return true;
		}

		private bool TryRecognizeMultiTap(PointerPoint pointerDown)
		{
			if (!CanBeDoubleTap(pointerDown))
			{
				return false;
			}

			_lastSingleTap = default; // The Recognizer supports only double tap, even on UWP
			Tapped?.Invoke(this, new TappedEventArgs(pointerDown.PointerDevice.PointerDeviceType, pointerDown.Position, tapCount: 2));

			return true;
		}
		#endregion

		private ulong GetPointerIdentifier(PointerPoint point)
		{
			// For mouse, the PointerId is the same, no matter the button pressed.
			// The only thing that changes are flags in the properties.
			// Here we build a "PointerIdentifier" that fully identifies the pointer used

			var props = point.Properties;

			ulong identifier = point.PointerId;

			// Mouse
			if (props.IsLeftButtonPressed)
			{
				identifier |= 1 << 32;
			}
			if (props.IsMiddleButtonPressed)
			{
				identifier |= 1 << 33;
			}
			if (props.IsRightButtonPressed)
			{
				identifier |= 1 << 34;
			}
			if (props.IsHorizontalMouseWheel)
			{
				identifier |= 1 << 35;
			}
			if (props.IsXButton1Pressed)
			{
				identifier |= 1 << 36;
			}
			if (props.IsXButton2Pressed)
			{
				identifier |= 1 << 37;
			}

			// Pen
			if (props.IsBarrelButtonPressed)
			{
				identifier |= 1 << 38;
			}
			if (props.IsEraser)
			{
				identifier |= 1 << 39;
			}

			return identifier;
		}
	}
}
