using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Input
{
	public partial class GestureRecognizer 
	{
		private readonly ILogger _log;
		private IDictionary<uint, List<PointerPoint>> _activePointers = new Dictionary<uint, List<PointerPoint>>();
		private GestureSettings _gestureSettings;

		public GestureSettings GestureSettings
		{
			get => _gestureSettings;
			set
			{
				_gestureSettings = value;
				_isManipulationEnabled = (value & GestureSettingsHelper.Manipulations) != 0;
			}
		}

		public bool IsActive => _activePointers.Count > 0;

		/// <summary>
		/// This is the owner provided in the ctor. It might be `null` if none provided.
		/// It's purpose it to allow usage of static event handlers.
		/// </summary>
		internal object Owner { get; }

		public GestureRecognizer()
		{
			_log = this.Log();
		}

		internal GestureRecognizer(object owner)
			: this()
		{
			Owner = owner;
		}

		public void ProcessDownEvent(PointerPoint value)
		{
			if (TryRecognizeMultiTap(value))
			{
				// This is how UWP behaves, it will fire the double tap
				// on Down for a double tap instead of the Up.

				return;
			}

			_activePointers[value.PointerId] = new List<PointerPoint>(16) { value };

			if (_isManipulationEnabled)
			{
				if (_manipulation == null)
				{
					_manipulation = new Manipulation(this, value);
				}
				else
				{
					_manipulation.Add(value);
				}
			}
		}

		public void ProcessMoveEvents(IList<PointerPoint> value) => ProcessMoveEvents(value, true);

		internal void ProcessMoveEvents(IList<PointerPoint> value, bool isRelevant)
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

			_manipulation?.Update(value);
		}

		public void ProcessUpEvent(PointerPoint value) => ProcessUpEvent(value, true);

		internal void ProcessUpEvent(PointerPoint value, bool isRelevant)
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

				// We need to process only events that are bubbling natively to this control (i.e. isIrrelevant == false),
				// if they are bubbling in managed it means that they where handled a child control,
				// so we should not use them for gesture recognition.

				Recognize(points, pointerUp: value);

				_manipulation?.Remove(value);
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

			_manipulation?.Complete();
		}

		private void Recognize(List<PointerPoint> points, PointerPoint pointerUp)
		{
			if (points.Count == 0)
			{
				return;
			}

			var recognized = TryRecognizeRightTap(points, pointerUp) // We check right tap first as for touch a right tap is a press and hold of the finger :)
				|| TryRecognizeTap(points, pointerUp);

			if (!recognized && _log.IsEnabled(LogLevel.Information))
			{
				_log.Info("No gesture recognized");
			}
		}

		#region Manipulations
		internal event TypedEventHandler<GestureRecognizer, ManipulationStartingEventArgs> ManipulationStarting; // This is not on the public API!
		public event TypedEventHandler<GestureRecognizer, ManipulationCompletedEventArgs> ManipulationCompleted;
#pragma warning disable  // Event not raised: intertia is not supported yet
		public event TypedEventHandler<GestureRecognizer, ManipulationInertiaStartingEventArgs> ManipulationInertiaStarting;
#pragma warning restore 67
		public event TypedEventHandler<GestureRecognizer, ManipulationStartedEventArgs> ManipulationStarted;
		public event TypedEventHandler<GestureRecognizer, ManipulationUpdatedEventArgs> ManipulationUpdated;

		private bool _isManipulationEnabled;
		private Manipulation _manipulation;

		internal Manipulation PendingManipulation => _manipulation;
		#endregion

		#region Tap (includes DoubleTap and RightTap)
		internal const ulong MultiTapMaxDelayTicks = TimeSpan.TicksPerMillisecond * 1000;
		internal const ulong HoldMinDelayTicks = TimeSpan.TicksPerMillisecond * 800;
		internal const float HoldMinPressure = .75f;
		internal const int TapMaxXDelta = 10;
		internal const int TapMaxYDelta = 10;

		private (ulong id, ulong ts, Point position) _lastSingleTap;

		public event TypedEventHandler<GestureRecognizer, TappedEventArgs> Tapped;
		public event TypedEventHandler<GestureRecognizer, RightTappedEventArgs> RightTapped;

		public bool CanBeDoubleTap(PointerPoint value)
			=> _gestureSettings.HasFlag(GestureSettings.DoubleTap) && IsMultiTapGesture(_lastSingleTap, value);

		private bool TryRecognizeTap(List<PointerPoint> points, PointerPoint pointerUp = null)
		{
			if (IsTapGesture(LeftButton, points, pointerUp, out var start, out _))
			{
				// pointerUp is not null here!

				_lastSingleTap = (start.id, pointerUp.Timestamp, pointerUp.Position);
				Tapped?.Invoke(this, new TappedEventArgs(start.point.PointerDevice.PointerDeviceType, start.point.Position, tapCount: 1));

				return true;
			}
			else
			{
				return false;
			}
		}

		private bool TryRecognizeMultiTap(PointerPoint pointerDown)
		{
			if (_gestureSettings.HasFlag(GestureSettings.DoubleTap) && IsMultiTapGesture(_lastSingleTap, pointerDown))
			{
				_lastSingleTap = default; // The Recognizer supports only double tap, even on UWP
				Tapped?.Invoke(this, new TappedEventArgs(pointerDown.PointerDevice.PointerDeviceType, pointerDown.Position, tapCount: 2));

				return true;
			}
			else
			{
				return false;
			}
		}

		private bool TryRecognizeRightTap(List<PointerPoint> points, PointerPoint pointerUp = null)
		{
			if (_gestureSettings.HasFlag(GestureSettings.RightTap) && IsRightTapGesture(points, pointerUp, out var start))
			{
				RightTapped?.Invoke(this, new RightTappedEventArgs(start.point.PointerDevice.PointerDeviceType, start.point.Position));

				return true;
			}
			else
			{
				return false;
			}
		}

		#region Actual Tap gestures recognition (static)
		// The beginning of a Tap gesture is: 1 down -> * moves close to the down with same buttons pressed
		private static bool IsBeginningTapGesture(CheckButton isExpectedButton, List<PointerPoint> points, out (PointerPoint point, ulong id) start, out bool isForceTouch)
		{
			var startPoint = points[0]; // down
			start = (startPoint, GetPointerIdentifier(startPoint));
			isForceTouch = false;

			if (!isExpectedButton(start.point)) // We validate only the start as for other points we validate the full pointer identifier
			{
				return false;
			}

			// Validate tap gesture
			// Note: There is no limit for the duration of the tap!
			for (var i = 1; i < points.Count; i++)
			{
				var point = points[i];
				var pointIdentifier = GetPointerIdentifier(point);

				if (
					// The pointer changed (left vs right click)
					pointIdentifier != start.id

					// Pointer moved to far
					|| IsOutOfTapRange(point.Position, start.point.Position)
				)
				{
					return false;
				}

				isForceTouch |= point.Properties.Pressure >= HoldMinPressure;
			}

			return true;
		}

		// A Tap gesture is: 1 down -> * moves close to the down with same buttons pressed -> 1 up
		private static bool IsTapGesture(CheckButton isExpectedButton, List<PointerPoint> points, PointerPoint pointerUp, out (PointerPoint point, ulong id) start, out bool isForceTouch)
		{
			if (pointerUp == null) // no tap if no up!
			{
				start = default;
				isForceTouch = false;
				return false;
			}

			// Validate that all the intermediates points are valid
			if (!IsBeginningTapGesture(isExpectedButton, points, out start, out isForceTouch))
			{
				return false;
			}

			// For the pointer up, we check only the distance, as it's expected that the pressed button changed!
			if (IsOutOfTapRange(pointerUp.Position, start.point.Position))
			{
				return false;
			}

			return true;
		}

		private static bool IsMultiTapGesture((ulong id, ulong ts, Point position) previousTap, PointerPoint down)
		{
			if (previousTap.ts == 0) // i.s. no previous tap to compare with
			{
				return false;
			}

			var currentId = GetPointerIdentifier(down);
			var currentTs = down.Timestamp;
			var currentPosition = down.Position;

			return previousTap.id == currentId
				&& currentTs - previousTap.ts <= MultiTapMaxDelayTicks
				&& !IsOutOfTapRange(previousTap.position, currentPosition);
		}

		private static bool IsRightTapGesture(List<PointerPoint> points, PointerPoint pointerUp, out (PointerPoint point, ulong id) start)
		{
			switch (points[0].PointerDevice.PointerDeviceType)
			{
				case PointerDeviceType.Touch:
					var isLeftTap = IsTapGesture(LeftButton, points, pointerUp, out start, out var isForceTouch);
					if (isLeftTap && pointerUp.Timestamp - start.point.Timestamp > HoldMinDelayTicks)
					{
						return true;
					}
#if __IOS__
					if (Uno.WinRTFeatureConfiguration.GestureRecognizer.InterpretForceTouchAsRightTap
						&& isLeftTap
						&& isForceTouch)
					{
						return true;
					}
#endif
					return false;

				case PointerDeviceType.Pen:
					if (IsTapGesture(BarrelButton, points, pointerUp, out start, out _))
					{
						return true;
					}

					// Some pens does not have a barrel button, so we also allow long press (and anyway it's the UWP behavior)
					if (IsTapGesture(LeftButton, points, pointerUp, out start, out _)
						&& pointerUp.Timestamp - start.point.Timestamp > HoldMinDelayTicks)
					{
						return true;
					}

					return false;

				case PointerDeviceType.Mouse:
					if (IsTapGesture(RightButton, points, pointerUp, out start, out _))
					{
						return true;
					}
#if __ANDROID__
					// On Android, usually the right button is mapped to back navigation. So, unlike UWP,
					// we also allow a long press with the left button to be more user friendly.
					if (Uno.WinRTFeatureConfiguration.GestureRecognizer.InterpretMouseLeftLongPressAsRightTap
						&& IsTapGesture(LeftButton, points, pointerUp, out start, out _)
						&& pointerUp.Timestamp - start.point.Timestamp > HoldMinDelayTicks)
					{
						return true;
					}
#endif
					return false;

				default:
					start = default;
					return false;
			}
		}
		#endregion

		#region Tap helpers
		private delegate bool CheckButton(PointerPoint point);

		private static readonly CheckButton LeftButton = (PointerPoint point) => point.Properties.IsLeftButtonPressed;
		private static readonly CheckButton RightButton = (PointerPoint point) => point.Properties.IsRightButtonPressed;
		private static readonly CheckButton BarrelButton = (PointerPoint point) => point.Properties.IsBarrelButtonPressed;

		private static bool IsOutOfTapRange(Point p1, Point p2)
			=> Math.Abs(p1.X - p2.X) > TapMaxXDelta
			|| Math.Abs(p1.Y - p2.Y) > TapMaxYDelta;
		#endregion

		#endregion

		private static ulong GetPointerIdentifier(PointerPoint point)
		{
			// For mouse, the PointerId is the same, no matter the button pressed.
			// The only thing that changes are flags in the properties.
			// Here we build a "PointerIdentifier" that fully identifies the pointer used

			var props = point.Properties;

			ulong identifier = point.PointerId;

			// Mouse
			if (props.IsLeftButtonPressed)
			{
				identifier |= 1L << 32;
			}
			if (props.IsMiddleButtonPressed)
			{
				identifier |= 1L << 33;
			}
			if (props.IsRightButtonPressed)
			{
				identifier |= 1L << 34;
			}
			if (props.IsHorizontalMouseWheel)
			{
				identifier |= 1L << 35;
			}
			if (props.IsXButton1Pressed)
			{
				identifier |= 1L << 36;
			}
			if (props.IsXButton2Pressed)
			{
				identifier |= 1L << 37;
			}

			// Pen
			if (props.IsBarrelButtonPressed)
			{
				identifier |= 1L << 38;
			}
			if (props.IsEraser)
			{
				identifier |= 1L << 39;
			}

			return identifier;
		}
	}
}
