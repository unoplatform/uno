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

			UpdateManipulationForPointerAdded(value);
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

			UpdateManipulationForPointersUpdated();
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

				UpdateManipulationForPointerRemoved(value);
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

			UpdateManipulationForCanceled();
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
		internal const int MinManipulationDeltaTranslateX = 15;
		internal const int MinManipulationDeltaTranslateY = 15;

		internal event TypedEventHandler<GestureRecognizer, EventArgs> ManipulationStarting; // This is not on the public API!
		public event TypedEventHandler<GestureRecognizer, ManipulationCompletedEventArgs> ManipulationCompleted;
#pragma warning disable 67
		public event TypedEventHandler<GestureRecognizer, ManipulationInertiaStartingEventArgs> ManipulationInertiaStarting;
#pragma warning restore 67
		public event TypedEventHandler<GestureRecognizer, ManipulationStartedEventArgs> ManipulationStarted;
		public event TypedEventHandler<GestureRecognizer, ManipulationUpdatedEventArgs> ManipulationUpdated;

		private bool _isManipulationEnabled;

		private ManipulationStates _manipulationState = ManipulationStates.None;
		private PointerPoint _manipulationOrigin;
		private PointerPoint _manipulationLast;

		private enum ManipulationStates
		{
			None = 0,
			Starting = 1,
			Started = 2,
			// Inertia = 3
		}

		private void UpdateManipulationForPointerAdded(PointerPoint point)
		{
			if (_isManipulationEnabled && _manipulationState == ManipulationStates.None)
			{
				_manipulationOrigin = _manipulationLast = point;
				_manipulationState = ManipulationStates.Starting;

				ManipulationStarting?.Invoke(this, EventArgs.Empty);
			}
		}

		private void UpdateManipulationForPointersUpdated()
		{
			if (!_isManipulationEnabled
				|| _manipulationOrigin == null // The manipulation was not enabled when pointer went down
				|| !_activePointers.TryGetValue(_manipulationOrigin.PointerId, out var points))
			{
				return;
			}

			var current = points.Last();
			var delta = GetManipulationDelta(_manipulationLast, current);

			if (!IsSignificant(delta))
			{
				return;
			}

			// Make sure to update the _manipulationLast before raising the event, so if an exception is raised
			// or if the manipulation is Completed, the Complete event args can use the updated _manipulationLast.
			_manipulationLast = current;

			switch (_manipulationState)
			{
				case ManipulationStates.Starting:
					_manipulationState = ManipulationStates.Started;

					ManipulationStarted?.Invoke(this, new ManipulationStartedEventArgs(
						_manipulationOrigin.PointerDevice.PointerDeviceType,
						current.Position,
						delta));
					break;

				case ManipulationStates.Started:
					ManipulationUpdated?.Invoke(this, new ManipulationUpdatedEventArgs(
						_manipulationOrigin.PointerDevice.PointerDeviceType,
						current.Position,
						delta,
						GetManipulationDelta(_manipulationOrigin, current),
						isInertial: false));
					break;
			}
		}

		private void UpdateManipulationForPointerRemoved(PointerPoint removed)
		{
			if (!_isManipulationEnabled
				|| _manipulationOrigin == null // The manipulation was not enabled when pointer went down
				|| _manipulationOrigin != removed)
			{
				return;
			}

			CompleteManipulation(removed);
		}

		private void UpdateManipulationForCanceled()
		{
			CompleteManipulation(_manipulationLast);
		}

		private void CompleteManipulation(PointerPoint current)
		{
			// If the manipulation was not started, we just abort the manipulation without any event
			if (_manipulationState == ManipulationStates.Started)
			{
				ManipulationCompleted?.Invoke(this, new ManipulationCompletedEventArgs(
					_manipulationOrigin.PointerDevice.PointerDeviceType,
					current.Position,
					GetManipulationDelta(_manipulationOrigin, current),
					isInertial: false));
			}

			_manipulationOrigin = null;
			_manipulationLast = null;
			_manipulationState = ManipulationStates.None;
		}

		private ManipulationDelta GetManipulationDelta(PointerPoint origin, PointerPoint current)
		{
			var originPosition = origin.Position;
			var currentPosition = current.Position;

			var deltaX = Math.Abs(currentPosition.X - originPosition.X);
			var deltaY = Math.Abs(currentPosition.Y - originPosition.Y);

			return new ManipulationDelta
			{
				Translation = new Point(deltaX, deltaY),
				Scale = 1,
				Rotation = 0,
				Expansion = 0,
			};
		}

		internal bool IsSignificant(ManipulationDelta delta)
		{
			if ((_gestureSettings & (GestureSettings.ManipulationTranslateX | GestureSettings.ManipulationTranslateRailsX)) != 0
				&& delta.Translation.X >= MinManipulationDeltaTranslateX)
			{
				return true;
			}

			if ((_gestureSettings & (GestureSettings.ManipulationTranslateY | GestureSettings.ManipulationTranslateRailsY)) != 0
				&& delta.Translation.Y >= MinManipulationDeltaTranslateY)
			{
				return true;
			}

			return false;
		}
		#endregion

		#region Tap (includes DoubleTap)
		internal const ulong MultiTapMaxDelayTicks = TimeSpan.TicksPerMillisecond * 1000;
		internal const int TapMaxXDelta = 15;
		internal const int TapMaxYDelta = 15;

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
