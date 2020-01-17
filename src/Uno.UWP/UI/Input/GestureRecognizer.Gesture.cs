using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Windows.Devices.Input;
using Windows.System;
using Microsoft.Extensions.Logging;
using Uno.Logging;

namespace Windows.UI.Input
{
	public partial class GestureRecognizer
	{
		/// <summary>
		/// This is the state machine which handles the gesture ([Double|Right]Tapped and Holding gestures)
		/// </summary>
		private class Gestur
		{
			private readonly GestureRecognizer _recognizer;
			private DispatcherQueueTimer _holdingTimer;
			private GestureSettings _settings;
			private HoldingState? _holdingState;

			public ulong PointerIdentifier { get; }

			public PointerDeviceType PointerType { get; }

			public PointerPoint Down { get; }

			public PointerPoint Up { get; private set; }

			public bool IsCompleted { get; private set; }

			public bool HasMovedOutOfTapRange { get; private set; }

			public bool HasChangedPointerIdentifier { get; private set; }

			public bool HasExceedMinHoldPressure { get; private set; }

			public Gesture(GestureRecognizer recognizer, PointerPoint down)
			{
				_recognizer = recognizer;
				_settings = recognizer._gestureSettings & GestureSettingsHelper.SupportedGestures; // Keep only flags of supported gestures, so we can more quickly disable us if possible
				_settings |= GestureSettings.Tap; // On WinUI, Tap is always raised no matter the flag set on the recognizer

				Down = down;
				PointerIdentifier = GetPointerIdentifier(down);
				PointerType = down.PointerDevice.PointerDeviceType;

				// This is how WinUI behaves: it will fire the double tap
				// on Down for a double tap instead of the Up.
				if (TryRecognizeMultiTap())
				{
					IsCompleted = true;
				}
				else
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

			public void PreventTap()
			{
				_settings &= ~GestureSettings.Tap;
				if ((_settings & GestureSettingsHelper.SupportedGestures) == GestureSettings.None)
				{
					IsCompleted = true;
				}
			}

			public void PreventDoubleTap()
			{
				_settings &= ~GestureSettings.DoubleTap;
				if ((_settings & GestureSettingsHelper.SupportedGestures) == GestureSettings.None)
				{
					IsCompleted = true;
				}
			}

			public void PreventRightTap()
			{
				_settings &= ~GestureSettings.RightTap;
				if ((_settings & GestureSettingsHelper.SupportedGestures) == GestureSettings.None)
				{
					IsCompleted = true;
				}
			}

			public void PreventHolding()
			{
				StopHoldingTimer();
				_settings &= ~(GestureSettings.Hold | GestureSettings.HoldWithMouse);
				if ((_settings & GestureSettingsHelper.SupportedGestures) == GestureSettings.None)
				{
					IsCompleted = true;
				}
			}

			private void TryRecognize()
			{
				var recognized = TryRecognizeRightTap() // We check right tap first as for touch a right tap is a press and hold of the finger :)
					|| TryRecognizeTap();

				if (!recognized && _recognizer._log.IsEnabled(LogLevel.Information))
				{
					_recognizer._log.Info("No gesture recognized");
				}
			}

			private bool TryRecognizeTap()
			{
				if (_settings.HasFlag(GestureSettings.Tap) && IsTapGesture(LeftButton, this))
				{
					// Note: Up cannot be 'null' here!

					_recognizer._lastSingleTap = (PointerIdentifier, Up.Timestamp, Up.Position);
					_recognizer.Tapped?.Invoke(_recognizer, new TappedEventArgs(PointerType, Down.Position, tapCount: 1));

					return true;
				}
				else
				{
					return false;
				}
			}

			private bool TryRecognizeMultiTap()
			{
				if (_settings.HasFlag(GestureSettings.DoubleTap) && IsMultiTapGesture(_recognizer._lastSingleTap, Down))
				{
					_recognizer._lastSingleTap = default; // The Recognizer supports only double tap, even on UWP
					_recognizer.Tapped?.Invoke(_recognizer, new TappedEventArgs(PointerType, Down.Position, tapCount: 2));

					return true;
				}
				else
				{
					return false;
				}
			}

			private bool TryRecognizeRightTap()
			{
				if (_settings.HasFlag(GestureSettings.RightTap) && IsRightTapGesture(this, out var isLongPress))
				{
					_recognizer.RightTapped?.Invoke(_recognizer, new RightTappedEventArgs(PointerType, Down.Position));

					return true;
				}
				else
				{
					return false;
				}
			}

			private void TryUpdateHolding(PointerPoint current = null, bool timeElapsed = false)
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
					&& !_holdingState.HasValue
					&& (timeElapsed || IsLongPress(this, current))
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
			{
				switch (PointerType)
				{
					case PointerDeviceType.Mouse: return _settings.HasFlag(GestureSettings.HoldWithMouse);
					default: return _settings.HasFlag(GestureSettings.Hold);
				}
			}

			private bool NeedsHoldingTimer()
			{
				// When possible we don't start a timer for the Holding event, instead we rely on the fact that
				// we get a lot of small moves due to the lack of precision of the capture device (pen and touch).

				switch (PointerType)
				{
					case PointerDeviceType.Mouse: return _settings.HasFlag(GestureSettings.HoldWithMouse);
					default: return false;
				}
			}

			private void StartHoldingTimer()
			{
				if (NeedsHoldingTimer())
				{
					_holdingTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
					_holdingTimer.Interval = TimeSpan.FromTicks(HoldMinDelayTicks);
					_holdingTimer.State = this;
					_holdingTimer.Tick += OnHoldingTimerTick;
					_holdingTimer.Start();
				}
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
		}
	}
}
