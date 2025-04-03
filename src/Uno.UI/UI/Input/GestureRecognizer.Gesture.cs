// On the UWP branch, only include this file in Uno.UWP (as public Window.whatever). On the WinUI branch, include it in both Uno.UWP (internal as Windows.whatever) and Uno.UI (public as Microsoft.whatever)
#if HAS_UNO_WINUI || !IS_UNO_UI_PROJECT
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;

using Uno;
using Uno.Foundation.Logging;

#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
	public partial class GestureRecognizer
	{
		public static bool IsOutOfTapRange(Point p1, Point p2)
			=> Math.Abs(p1.X - p2.X) > TapMaxXDelta
			   || Math.Abs(p1.Y - p2.Y) > TapMaxYDelta;

		/// <summary>
		/// This is the state machine which handles the gesture ([Double|Right]Tapped and Holding gestures)
		/// </summary>
		private class Gesture
		{
			private readonly GestureRecognizer _recognizer;
			private DispatcherQueueTimer? _holdingTimer;
			private HoldingState? _holdingState;

			public ulong PointerIdentifier { get; }

			public PointerDeviceType PointerType { get; }

			public PointerPoint Down { get; }

			public PointerPoint? Up { get; private set; }

			public bool IsCompleted { get; private set; }

			public bool HasMovedOutOfTapRange { get; private set; }

			public bool HasChangedPointerIdentifier { get; private set; }

			public bool HasExceedMinHoldPressure { get; private set; }

			internal GestureSettings Settings
			{
				get;
				private set;
			}

			public Gesture(GestureRecognizer recognizer, PointerPoint down)
			{
				_recognizer = recognizer;
				Settings = recognizer._gestureSettings & GestureSettingsHelper.SupportedGestures; // Keep only flags of supported gestures, so we can more quickly disable us if possible

				Down = down;
				PointerIdentifier = GetPointerIdentifier(down);
				PointerType = (PointerDeviceType)down.PointerDevice.PointerDeviceType;

				// This is how WinUI behaves: it will fire the double tap
				// on Down for a double tap instead of the Up.
				if (TryRecognizeMultiTap())
				{
					IsCompleted = true;
				}
				else if (SupportsHolding())
				{
					StartHoldingTimer();
				}
			}

			public void ProcessMove(PointerPoint point)
			{
				if (IsCompleted)
				{
					return;
				}

				// Update internal state
				HasMovedOutOfTapRange |= IsOutOfTapRange(Down.Position, point.Position);
				HasChangedPointerIdentifier |= PointerIdentifier != GetPointerIdentifier(point);
				HasExceedMinHoldPressure |= point.Properties.Pressure >= HoldMinPressure;

				// Process the pointer
				TryUpdateHolding(point);
			}

			public void ProcessUp(PointerPoint up)
			{
				if (IsCompleted)
				{
					return;
				}

				// Update internal state
				IsCompleted = true;
				Up = up;

				// Process the pointer
				TryEndHolding(HoldingState.Completed);
				TryRecognize();
			}

			public void ProcessComplete()
			{
				if (IsCompleted)
				{
					return;
				}

				// Update internal state
				IsCompleted = true;

				// Process the pointer
				TryEndHolding(HoldingState.Canceled);
				TryRecognize();
			}

			public void PreventGestures(GestureSettings gestures)
			{
				if ((gestures & GestureSettings.Hold) != 0)
				{
					StopHoldingTimer();
				}

				Settings &= ~gestures;
				if (Settings == GestureSettings.None)
				{
					IsCompleted = true;
				}
			}

			private void TryRecognize()
			{
				var recognized = TryRecognizeRightTap() // We check right tap first as for touch a right tap is a press and hold of the finger :)
					|| TryRecognizeTap();

				if (!recognized && _recognizer._log.IsEnabled(LogLevel.Debug))
				{
					_recognizer._log.Debug("No gesture recognized");
				}
			}

			private bool TryRecognizeTap()
			{
				var isTapGesture = IsTapGesture(LeftButton, this);
				if (isTapGesture)
				{
					// Note: Up cannot be 'null' here!
					_recognizer._lastSingleTap = (PointerIdentifier, Up!.Timestamp, Up.Position);

					if (Settings.HasFlag(GestureSettings.Tap))
					{
						_recognizer.Tapped?.Invoke(_recognizer, new TappedEventArgs(Down.PointerId, PointerType, Down.Position, tapCount: 1));
						return true;
					}
				}

				return false;
			}

			private bool TryRecognizeMultiTap()
			{
				if (Settings.HasFlag(GestureSettings.DoubleTap) && IsMultiTapGesture(_recognizer._lastSingleTap, Down))
				{
					_recognizer._lastSingleTap = default; // The Recognizer supports only double tap, even on UWP
					_recognizer.Tapped?.Invoke(_recognizer, new TappedEventArgs(Down.PointerId, PointerType, Down.Position, tapCount: 2));

					return true;
				}
				else
				{
					return false;
				}
			}

			private bool TryRecognizeRightTap()
			{
				if (Settings.HasFlag(GestureSettings.RightTap) && IsRightTapGesture(this, out var isLongPress))
				{
					_recognizer.RightTapped?.Invoke(_recognizer, new RightTappedEventArgs(Down.PointerId, PointerType, Down.Position));

					return true;
				}
				else
				{
					return false;
				}
			}

			private void TryUpdateHolding(PointerPoint? current = null, bool timeElapsed = false)
			{
				Debug.Assert(timeElapsed || current != null);

				if (HasMovedOutOfTapRange)
				{
					StopHoldingTimer();

					var currentState = _holdingState;
					_holdingState = HoldingState.Canceled;

					if (currentState == HoldingState.Started)
					{
						_recognizer.Holding?.Invoke(_recognizer, new HoldingEventArgs(Down.PointerId, PointerType, Down.Position, HoldingState.Canceled));
					}
				}
				else if (SupportsHolding()
					&& _holdingState is null
					&& (timeElapsed || IsLongPress(Down, current!))
					&& IsBeginningOfTapGesture(LeftButton, this))
				{
					StopHoldingTimer();

					_holdingState = HoldingState.Started;
					_recognizer.Holding?.Invoke(_recognizer, new HoldingEventArgs(Down.PointerId, PointerType, Down.Position, HoldingState.Started));
				}
			}

			private void TryEndHolding(HoldingState state)
			{
				Debug.Assert(state != HoldingState.Started);

				StopHoldingTimer();

				if (_holdingState == HoldingState.Started)
				{
					_holdingState = state;
					_recognizer.Holding?.Invoke(_recognizer, new HoldingEventArgs(Down.PointerId, PointerType, Down.Position, state));
				}
			}

			#region Holding timer
			private bool SupportsHolding()
				=> PointerType switch
				{
					PointerDeviceType.Mouse => Settings.HasFlag(GestureSettings.HoldWithMouse),
					_ => Settings.HasFlag(GestureSettings.Hold)
				};

			private void StartHoldingTimer()
			{
				_holdingTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
				_holdingTimer.Interval = TimeSpan.FromMicroseconds(HoldMinDelayMicroseconds);
				_holdingTimer.State = this;
				_holdingTimer.Tick += OnHoldingTimerTick;
				_holdingTimer.Start();
			}

			private void StopHoldingTimer()
			{
				_holdingTimer?.Stop();
				_holdingTimer = null;
			}

			private static void OnHoldingTimerTick(DispatcherQueueTimer timer, object _)
			{
				timer.Stop();
				((Gesture)timer.State).TryUpdateHolding(timeElapsed: true);
			}
			#endregion

			#region Gestures recognition (static helpers that defines the actual gestures behavior)

			// The beginning of a Tap gesture is: 1 down -> * moves close to the down with same buttons pressed
			private static bool IsBeginningOfTapGesture(CheckButton isExpectedButton, Gesture points)
			{
				if (!isExpectedButton(points.Down)) // We validate only the start as for other points we validate the full pointer identifier
				{
					return false;
				}

				// Validate tap gesture
				// Note: There is no limit for the duration of the tap!
				if (points.HasMovedOutOfTapRange || points.HasChangedPointerIdentifier)
				{
					return false;
				}

				return true;
			}

			// A Tap gesture is: 1 down -> * moves close to the down with same buttons pressed -> 1 up
			private static bool IsTapGesture(CheckButton isExpectedButton, Gesture points)
			{
				if (points.Up == null) // no tap if no up!
				{
					return false;
				}

				// Validate that all the intermediates points are valid
				if (!IsBeginningOfTapGesture(isExpectedButton, points))
				{
					return false;
				}

				// For the pointer up, we check only the distance, as it's expected that the pressed button changed!
				if (IsOutOfTapRange(points.Down.Position, points.Up.Position))
				{
					return false;
				}

				return true;
			}

			public static bool IsMultiTapGesture((ulong id, ulong ts, Point position) previousTap, PointerPoint down)
			{
				if (previousTap.ts == 0) // i.s. no previous tap to compare with
				{
					return false;
				}

				var currentId = GetPointerIdentifier(down);
				var currentTs = down.Timestamp;
				var currentPosition = down.Position;

				return previousTap.id == currentId
					&& currentTs - previousTap.ts <= MultiTapMaxDelayMicroseconds
					&& !IsOutOfTapRange(previousTap.position, currentPosition);
			}

			private static bool IsRightTapGesture(Gesture points, out bool isLongPress)
			{
				switch (points.PointerType)
				{
					case PointerDeviceType.Touch:
						var isLeftTap = IsTapGesture(LeftButton, points);
						if (isLeftTap && IsLongPress(points.Down, points.Up!))
						{
							isLongPress = true;
							return true;
						}
#if __APPLE_UIKIT__
						if (Uno.WinRTFeatureConfiguration.GestureRecognizer.InterpretForceTouchAsRightTap
							&& isLeftTap
							&& points.HasExceedMinHoldPressure)
						{
							isLongPress = true; // We handle the pressure exactly like a long press
							return true;
						}
#endif
						isLongPress = false;
						return false;

					case PointerDeviceType.Pen:
						if (IsTapGesture(BarrelButton, points))
						{
							isLongPress = false;
							return true;
						}

						// Some pens does not have a barrel button, so we also allow long press (and anyway it's the UWP behavior)
						if (IsTapGesture(LeftButton, points) && IsLongPress(points.Down, points.Up!))
						{
							isLongPress = true;
							return true;
						}

						isLongPress = false;
						return false;

					case PointerDeviceType.Mouse:
						if (IsTapGesture(RightButton, points))
						{
							isLongPress = false;
							return true;
						}
#if __ANDROID__
						// On Android, usually the right button is mapped to back navigation. So, unlike UWP,
						// we also allow a long press with the left button to be more user friendly.
						if (Uno.WinRTFeatureConfiguration.GestureRecognizer.InterpretMouseLeftLongPressAsRightTap
							&& IsTapGesture(LeftButton, points)
							&& IsLongPress(points.Down, points.Up!))
						{
							isLongPress = true;
							return true;
						}
#endif
						isLongPress = false;
						return false;

					default:
						isLongPress = false;
						return false;
				}
			}

			private static bool IsLongPress(PointerPoint down, PointerPoint current)
				=> current.Timestamp - down.Timestamp > HoldMinDelayMicroseconds;
			#endregion
		}
	}
}
#endif
