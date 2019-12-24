using System;
using System.Collections.Generic;
using System.Linq;
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
			if (isRelevant)
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

				if (isRelevant)
				{
					// We need to process only events that are bubbling natively to this control (i.e. isIrrelevant == false),
					// if they are bubbling in managed it means that they where handled a child control,
					// so we should not use them for gesture recognition.

					Recognize(points, pointerUp: value);
				}

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

			var recognized = TryRecognizeTap(points, pointerUp);

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

		#region Tap (includes DoubleTap)
		internal const ulong MultiTapMaxDelayTicks = TimeSpan.TicksPerMillisecond * 1000;
		internal const int TapMaxXDelta = 10;
		internal const int TapMaxYDelta = 10;

		public event TypedEventHandler<GestureRecognizer, TappedEventArgs> Tapped;

		private (ulong, ulong, Point) _lastSingleTap;

		public bool CanBeDoubleTap(PointerPoint value)
		{
			if (!_gestureSettings.HasFlag(GestureSettings.DoubleTap))
			{
				return false;
			}

			var (lastTapId, lastTapTs, lastTapLocation) = _lastSingleTap;
			if (lastTapTs == 0)
			{
				return false;
			}

			var currentId = GetPointerIdentifier(value);
			var currentTs = value.Timestamp;
			var currentPosition = value.Position;

			return lastTapId == currentId
				&& currentTs - lastTapTs <= MultiTapMaxDelayTicks
				&& !IsOutOfTapRange(lastTapLocation, currentPosition);
		}

		private bool TryRecognizeTap(List<PointerPoint> points, PointerPoint pointerUp = null)
		{
			if (pointerUp == null)
			{
				return false;
			}

			var start = points[0]; // down
			var end = pointerUp;
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
					|| IsOutOfTapRange(point.Position, start.Position)
				)
				{
					return false;
				}
			}
			// For the pointer up, we check only the distance, as it's expected that the IsLeftButtonPressed changed!
			if (IsOutOfTapRange(end.Position, start.Position))
			{
				return false;
			}

			_lastSingleTap = (startIdentifier, end.Timestamp, end.Position);
			Tapped?.Invoke(this, new TappedEventArgs(start.PointerDevice.PointerDeviceType, start.Position, tapCount: 1));

			return true;
		}

		private static bool IsOutOfTapRange(Point p1, Point p2)
			=> Math.Abs(p1.X - p2.X) > TapMaxXDelta
			|| Math.Abs(p1.Y - p2.Y) > TapMaxYDelta;

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
