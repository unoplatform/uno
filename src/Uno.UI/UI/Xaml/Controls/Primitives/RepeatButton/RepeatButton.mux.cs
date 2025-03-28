// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// RepeatButton_Partial.h, RepeatButton_Partial.cpp

#nullable enable

using System;
using Windows.Devices.Input;
using Windows.System;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class RepeatButton
	{
		private bool _keyboardCausingRepeat;
		private bool _pointerCausingRepeat;
		private DispatcherTimer? _timer;

		/// <summary>
		/// Represents a value indicating whether the RepeatButton reacts to touch input or not.
		/// </summary>
		internal bool IgnoreTouchInput { get; set; }

		/// <summary>
		/// Prepares object's state.
		/// </summary>
		private protected override void Initialize()
		{
			base.Initialize();

			ClickMode = ClickMode.Press;
		}

		/// <summary>
		/// Change to the correct visual state for the repeat button.
		/// </summary>
		/// <param name="useTransitions">Use transitions.</param>
		private protected override void ChangeVisualState(bool useTransitions)
		{
			var isEnabled = IsEnabled;
			var isPressed = IsPressed;
			var isPointerOver = IsPointerOver;
			var focusState = FocusState;

			if (!isEnabled)
			{
				GoToState(useTransitions, "Disabled");
			}
			else if (isPressed)
			{
				GoToState(useTransitions, "Pressed");
			}
			else if (isPointerOver)
			{
				GoToState(useTransitions, "PointerOver");
			}
			else
			{
				GoToState(useTransitions, "Normal");
			}

			if (focusState != FocusState.Unfocused && isEnabled)
			{
				if (focusState == FocusState.Pointer)
				{
					GoToState(useTransitions, "PointerFocused");
				}
				else
				{
					GoToState(useTransitions, "Focused");
				}
			}
			else
			{
				GoToState(useTransitions, "Unfocused");
			}
		}

		/// <summary>
		/// Raises the Click routed event.
		/// </summary>
		private protected override void OnClick()
		{
			var hasAutomationListener = AutomationPeer.ListenerExistsHelper(AutomationEvents.InvokePatternOnInvoked);
			if (hasAutomationListener)
			{
				var automationPeer = GetOrCreateAutomationPeer();
				if (automationPeer != null)
				{
					automationPeer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
				}
			}

			base.OnClick();
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			if (args.Property == DelayProperty)
			{
				OnDelayPropertyChanged(args.NewValue);
			}
			else if (args.Property == IntervalProperty)
			{
				OnIntervalPropertyChanged(args.NewValue);
			}
		}

		private void OnDelayPropertyChanged(object pNewDelay)
		{
			int newDelay = (int)pNewDelay;
			if (newDelay < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(Delay), "Delay cannot be less than 0.");
			}
		}

		private void OnIntervalPropertyChanged(object pNewInterval)
		{
			int newInterval = (int)pNewInterval;
			if (newInterval <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(Interval), "Interval cannot be less than 0.");
			}
		}

		/// <summary>
		/// Called when the IsEnabled property changes.
		/// </summary>
		/// <param name="e">Event args.</param>
		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);

			_keyboardCausingRepeat = false;
			_pointerCausingRepeat = false;

			UpdateRepeatState();
		}

		/// <summary>
		/// KeyDown event handler.
		/// </summary>
		/// <param name="args">Event args.</param>
		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			if (args.Key == VirtualKey.Space &&
				ClickMode != ClickMode.Hover)
			{
				_keyboardCausingRepeat = true;
				UpdateRepeatState();
			}

			base.OnKeyDown(args);
		}

		/// <summary>
		/// KeyUp event handler.
		/// </summary>
		/// <param name="args">Event args.</param>
		protected override void OnKeyUp(KeyRoutedEventArgs args)
		{
			base.OnKeyUp(args);

			if (args.Key == VirtualKey.Space &&
				ClickMode != ClickMode.Hover)
			{
				_keyboardCausingRepeat = false;
				UpdateRepeatState();
			}
		}

		/// <summary>
		/// LostFocus event handler.
		/// </summary>
		/// <param name="e">Event args.</param>
		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);

			if (ClickMode != ClickMode.Hover)
			{
				_keyboardCausingRepeat = false;
				_pointerCausingRepeat = false;
				UpdateRepeatState();
			}
		}

		private bool ShouldIgnoreInput(PointerRoutedEventArgs args)
		{
			bool ignoreInput = false;

			if (IgnoreTouchInput)
			{
				var pointerPoint = args.GetCurrentPoint(null);
				var pointerDeviceType = pointerPoint.PointerDevice?.PointerDeviceType ?? PointerDeviceType.Touch;
				if (pointerDeviceType == PointerDeviceType.Touch)
				{
					ignoreInput = true;
				}
			}

			return ignoreInput;
		}

		/// <summary>
		/// PointerEnter event handler.
		/// </summary>
		/// <param name="args">Event args.</param>
		protected override void OnPointerEntered(PointerRoutedEventArgs args)
		{
			if (ShouldIgnoreInput(args))
			{
				return;
			}

			base.OnPointerEntered(args);

			if (ClickMode == ClickMode.Hover)
			{
				_pointerCausingRepeat = true;
			}

			UpdateRepeatState();
			UpdateVisualState();
		}

		/// <summary>
		/// PointerMoved event handler.
		/// </summary>
		/// <param name="args">Event args.</param>
		protected override void OnPointerMoved(PointerRoutedEventArgs args)
		{
			// The reason this function does not call the base 
			// OnPointerMove is that the ButtonBase class sometimes 
			// sets IsPressed to false based on mouse position. This 
			// interferes with the RepeatButton functionality, which
			// relies on IsPressed being True in Hover mode incorrectly.						
		}

		/// <summary>
		/// PointerLeave event handler.
		/// </summary>
		/// <param name="args">Event args.</param>
		protected override void OnPointerExited(PointerRoutedEventArgs args)
		{
			if (ShouldIgnoreInput(args))
			{
				return;
			}

			base.OnPointerExited(args);

			if (ClickMode == ClickMode.Hover)
			{
				_pointerCausingRepeat = false;
				UpdateRepeatState();
			}

			UpdateVisualState();
		}

		/// <summary>
		/// PointerPressed event handler.
		/// </summary>
		/// <param name="args">Event args.</param>
		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			if (args.Handled)
			{
				return;
			}

			if (ShouldIgnoreInput(args))
			{
				return;
			}

			base.OnPointerPressed(args);

			var pointerPoint = args.GetCurrentPoint(this);
			var isLeftButtonPressed = pointerPoint.Properties.IsLeftButtonPressed;

			if (isLeftButtonPressed)
			{
				if (ClickMode != ClickMode.Hover)
				{
					_pointerCausingRepeat = true;
					UpdateRepeatState();
				}
			}
		}

		/// <summary>
		/// PointerReleased event handler.
		/// </summary>
		/// <param name="args">Event agrs.</param>
		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			if (args.Handled)
			{
				return;
			}

			if (ShouldIgnoreInput(args))
			{
				return;
			}

			base.OnPointerReleased(args);

			if (ClickMode != ClickMode.Hover)
			{
				_pointerCausingRepeat = false;
				UpdateRepeatState();
			}

			UpdateVisualState();
		}

		protected override AutomationPeer OnCreateAutomationPeer() => new RepeatButtonAutomationPeer(this);

		private void StartTimer()
		{
			if (_timer == null)
			{
				_timer = new DispatcherTimer();
				_timer.Tick += TimerCallback;
			}

			if (!_timer.IsEnabled)
			{
				_timer.Interval = TimeSpan.FromMilliseconds(Delay);
				_timer.Start();
			}
		}

		private void StopTimer() => _timer?.Stop();

		private void UpdateRepeatState()
		{
			if ((_pointerCausingRepeat && IsPointerOver) || _keyboardCausingRepeat)
			{
				StartTimer();
			}
			else
			{
				StopTimer();
			}
		}

		private void TimerCallback(object? sender, object state)
		{
			var interval = TimeSpan.FromMilliseconds(Interval);

			if (_timer!.Interval != interval)
			{
				_timer.Interval = interval;
			}

			var isPressed = IsPressed;

			if ((isPressed && IsPointerOver) || (isPressed && _keyboardCausingRepeat))
			{
				OnClick();
			}
			else
			{
				StopTimer();
			}
		}
	}
}
