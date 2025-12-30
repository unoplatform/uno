// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ButtonBase_Partial.h, ButtonBase_Partial.cpp, tag winui3/release/1.4.2

#nullable enable

using System;
using System.Windows.Input;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class ButtonBase
	{
		// Last known position of the pointer with respect to this button.
		private Point _pointerPosition = Point.Zero;

		// True if the SPACE or ENTER key is currently pressed, false otherwise.
		private bool _isSpaceOrEnterKeyDown;

		// True if the NAVIGATION_ACCEPT or GAMEPAD_A vkey is currently pressed, false otherwise.
		private bool _isNavigationAcceptOrGamepadAKeyDown;

		// True if the pointer's left button is currently down, false otherwise.
		private bool _isPointerLeftButtonDown;

		// True if ENTER key is equivalent to SPACE
		private bool _keyboardNavigationAcceptsReturn;

		// On pointer released we perform some actions depending on control. We decide to whether to perform them
		// depending on some parameters including but not limited to whether released is followed by a pressed, which
		// mouse button is pressed, what type of pointer is it etc. This boolean keeps our decision.
		private bool _shouldPerformActions;

		// Whether the button should handle keyboard ENTER and SPACE key input.
		// HubSection's header button sets this to false for non-interactive headers so that they
		// are still focusable tab stobs but so that they do not invoke.
		private bool _handlesKeyboardInput = true;

		/// <summary>
		/// Event registration token corresponding to subscription for ICommand.CanExecuteChanged event.
		/// </summary>
		private readonly SerialDisposable _canExecuteChangedHandler = new SerialDisposable();

		// Previously we released the capture from the pointer that we extracted from pointer released args. However, righttapped args
		// does not carry pointer information. So if we defer the action that we previously did until we get onrighttappedunhandled, we need
		// to store the pointer to release its capture.
		private Pointer? _pointerForPendingRightTapped;

		// True if the pointer is captured.
		private bool _isPointerCaptured;

		private protected virtual void Initialize()
		{
			// Allow the button to respond to the ENTER key and be focused
			SetAcceptsReturn(true);

			Loaded += OnLoaded;
#if !UNO_HAS_ENHANCED_LIFECYCLE
			//TODO Uno specific: Call LeaveImpl to simulate leaving visual tree
			Unloaded += (s, e) => LeaveImpl();
#endif
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			if (args.Property == ClickModeProperty)
			{
				OnClickModeChanged((ClickMode)args.NewValue);
			}
			else if (args.Property == IsPressedProperty)
			{
				OnIsPressedChanged();
			}
			else if (args.Property == CommandProperty)
			{
				OnCommandChanged(args.OldValue, args.NewValue);
			}
			else if (args.Property == CommandParameterProperty)
			{
				UpdateCanExecuteSafe();
			}
			else if (args.Property == VisibilityProperty)
			{
				OnVisibilityChanged();
			}
		}

		/// <summary>
		/// Validate the ClickMode property when its value is changed.
		/// </summary>
		/// <param name="newClickMode">New click mode.</param>
		private void OnClickModeChanged(ClickMode newClickMode)
		{
			if (!Enum.IsDefined((ClickMode)newClickMode))
			{
				throw new ArgumentException("Invalid ClickMode set.", nameof(newClickMode));
			}
		}

		/// <summary>
		/// Update the visual states when the IsPressed property is changed.
		/// </summary>
		private void OnIsPressedChanged() => UpdateVisualState();

		/// <summary>
		/// Update the visual states when the Visibility property is changed.
		/// </summary>
		private protected override void OnVisibilityChanged()
		{
			if (Visibility != Visibility.Visible)
			{
				ClearStateFlags();
			}

			UpdateVisualState();
		}

		/// <summary>
		/// Called when the IsEnabled property changes.
		/// </summary>
		/// <param name="e">Enabled changed event args.</param>
		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);

			if (!IsEnabled)
			{
				ClearStateFlags();
			}

			UpdateVisualState();

			// Uno specific
			OnIsEnabledChangedPartial(e);
		}

		partial void OnIsEnabledChangedPartial(IsEnabledChangedEventArgs e);

		/// <summary>
		/// Called when the element enters the tree. Attaches event handler to Command.CanExecuteChanged.
		/// </summary>
#if UNO_HAS_ENHANCED_LIFECYCLE
		private protected override void EnterImpl(bool live)
#else
		private void EnterImpl()
#endif
		{
#if UNO_HAS_ENHANCED_LIFECYCLE
			base.EnterImpl(live);
#endif
			if (_canExecuteChangedHandler.Disposable == null)
			{
				var command = Command;

				if (command != null)
				{
					void CanExecuteChangedHandler(object? sender, object args)
					{
						UpdateCanExecute();
					}

					command.CanExecuteChanged += CanExecuteChangedHandler;
					_canExecuteChangedHandler.Disposable = Disposable.Create(() =>
					{
						command.CanExecuteChanged -= CanExecuteChangedHandler;
					});
				}

				// In case we missed an update to CanExecute while the CanExecuteChanged handler was unhooked,
				// we need to update our value now.
				UpdateCanExecute();
			}
		}

		/// <summary>
		/// Called when the element leaves the tree. Detaches event handler from Command.CanExecuteChanged.
		/// </summary>
#if UNO_HAS_ENHANCED_LIFECYCLE
		private protected override void LeaveImpl(bool live)
#else
		private void LeaveImpl()
#endif
		{
#if UNO_HAS_ENHANCED_LIFECYCLE
			base.LeaveImpl(live);
#endif
			if (_canExecuteChangedHandler.Disposable != null)
			{
				_canExecuteChangedHandler.Disposable = null;
			}
		}

		/// <summary>
		/// Clear flags relating to the visual state.  Called when IsEnabled is set to false
		/// or when Visibility is set to Hidden or Collapsed.
		/// </summary>
		private void ClearStateFlags()
		{
			using var suspender = new StateChangeSuspender(this);

			IsPressed = false;
			IsPointerOver = false;
			_isPointerCaptured = false;
			_isSpaceOrEnterKeyDown = false;
			_isPointerLeftButtonDown = false;
			_isNavigationAcceptOrGamepadAKeyDown = false;
		}

		/// <summary>
		/// Called when ButtonBase.Command property changes.
		/// </summary>
		/// <param name="oldValue">Old value.</param>
		/// <param name="newValue">New value.</param>
		private protected virtual void OnCommandChanged(object oldValue, object newValue)
		{
			// Remove handler for CanExecuteChanged from the old value
			if (_canExecuteChangedHandler.Disposable != null)
			{
				_canExecuteChangedHandler.Disposable = null;
			}

			if (oldValue != null)
			{
				var oldCommandAsUICommand = oldValue as XamlUICommand;

				if (oldCommandAsUICommand != null)
				{
					CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, ContentControl.ContentProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, UIElement.KeyboardAcceleratorsProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, UIElement.AccessKeyProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, AutomationProperties.HelpTextProperty);
					CommandingHelpers.ClearBindingIfSet(oldCommandAsUICommand, this, ToolTipService.ToolTipProperty);
				}
			}

			// Subscribe to the CanExecuteChanged event on the new value
			if (newValue != null)
			{
				var newCommand = newValue as ICommand;

				void CanExecuteChangedHandler(object? sender, object args)
				{
					UpdateCanExecute();
				}

				if (newCommand != null)
				{
					newCommand.CanExecuteChanged += CanExecuteChangedHandler;
					_canExecuteChangedHandler.Disposable = Disposable.Create(() =>
					{
						newCommand.CanExecuteChanged -= CanExecuteChangedHandler;
					});
				}

				var newCommandAsUICommand = newCommand as XamlUICommand;

				if (newCommandAsUICommand != null)
				{
					CommandingHelpers.BindToLabelPropertyIfUnset(newCommandAsUICommand, this, ContentControl.ContentProperty);
					CommandingHelpers.BindToKeyboardAcceleratorsIfUnset(newCommandAsUICommand, this);
					CommandingHelpers.BindToAccessKeyIfUnset(newCommandAsUICommand, this);
					CommandingHelpers.BindToDescriptionPropertiesIfUnset(newCommandAsUICommand, this);
				}
			}

			// Coerce the button enabled state with the CanExecute state of the command.
			UpdateCanExecuteSafe();
		}

		/// <summary>
		/// Coerces button enabled state with CanExecute state of the command.
		/// </summary>
		private void UpdateCanExecute()
		{
			var canExecute = true;

			var command = Command;
			if (command != null)
			{
				var commandParameter = CommandParameter;

				// SYNC_CALL_TO_APP DIRECT - This next line may directly call out to app code.
				canExecute = command.CanExecute(commandParameter);
			}

			// If command is present and cannot be executed, disable the button.
			var suppress = !canExecute;
			SuppressIsEnabled(suppress);
		}

		private void UpdateCanExecuteSafe()
		{
			// uno specific workaround:
			// If Button::Command binding produces an ICommand value that throws Exception in its CanExecute,
			// this value will be canceled and replaced by the Binding::FallbackValue.
			try
			{
				UpdateCanExecute();
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Failed to update CanExecute", e);
				}
			}
		}

		/// <summary>
		/// Executes ButtonBase.Command.
		/// </summary>
		private void ExecuteCommand()
		{
			var command = Command;
			if (command != null)
			{
				var commandParameter = CommandParameter;

				// SYNC_CALL_TO_APP DIRECT - This next line may directly call out to app code.
				var canExecute = command.CanExecute(CommandParameter);
				if (canExecute)
				{
					// SYNC_CALL_TO_APP DIRECT - This next line may directly call out to app code.
					command.Execute(commandParameter);
				}
			}
		}

		/// <summary>
		/// Loaded event handler.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="args">Event args.</param>
		private void OnLoaded(object sender, RoutedEventArgs args)
		{
			UpdateVisualState(false);
#if !UNO_HAS_ENHANCED_LIFECYCLE
			// TODO Uno specific: Call EnterImpl to simulate entering visual tree
			EnterImpl();
#endif
		}

		/// <summary>
		/// GotFocus event handler.
		/// </summary>
		/// <param name="e">Event args.</param>
		protected override void OnGotFocus(RoutedEventArgs e)
		{
			UpdateVisualState();

			base.OnGotFocus(e);
		}

		/// <summary>
		/// Release pointer capture if we already had it.
		/// </summary>
		/// <param name="pointer">Pointer or null.</param>
		private void ReleasePointerCaptureInternal(Pointer? pointer)
		{
			if (pointer == null)
			{
				// No pointer available so clear all captures.
				ReleasePointerCaptures();
			}
			else
			{
				ReleasePointerCapture(pointer);
			}

			_isPointerCaptured = false;
		}

		/// <summary>
		/// LostFocus event handler.
		/// </summary>
		/// <param name="e">Event args.</param>
		protected override void OnLostFocus(RoutedEventArgs e)
		{
			if (FocusState == FocusState.Unfocused)
			{
				using var suspender = new StateChangeSuspender(this);

				if (ClickMode != ClickMode.Hover)
				{
					IsPressed = false;
					ReleasePointerCaptureInternal(null);
					_isSpaceOrEnterKeyDown = false;
					_isNavigationAcceptOrGamepadAKeyDown = false;
				}
			}

			base.OnLostFocus(e);
		}

		/// <summary>
		/// KeyDown event handler.
		/// </summary>
		/// <param name="args">Event args.</param>
		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			var handled = args.Handled;
			if (handled || !_handlesKeyboardInput)
			{
				Cleanup();
				return;
			}

			var key = args.Key;
			handled = OnKeyDownInternal(key);
			if (handled)
			{
				args.Handled = true;
			}

			Cleanup();

			void Cleanup()
			{
				base.OnKeyDown(args);
			}
		}

		/// <summary>
		/// Handles the KeyDown event for ButtonBase.
		/// Note: ENTER previously did not use ClickMode and was processed on first
		/// keydown. However keydown events are sent synchronously, so if the button was
		/// clicked using the ENTER key, the click handler could not execute code that
		/// caused reentrancy, like displaying a dialog. Such handlers are common. To
		/// solve this, ENTER was changed to use ClickMode. The default ClickMode is
		/// ClickMode.Release, and keyup events are sent asynchronously, so in the
		/// default case, click handlers can display dialogs or execute other code that
		/// causes reentrancy. If the ClickMode is changed by the app to ClickMode.Press,
		/// the click handler will not be able to execute code that causes reentrancy.
		/// </summary>
		private protected virtual bool OnKeyDownInternal(VirtualKey key)
		{
			KeyProcess.KeyDown(key, out var handled, _keyboardNavigationAcceptsReturn, this);
			return handled;
		}

		/// <summary>
		/// KeyUp event handler.
		/// </summary>
		/// <param name="args">Event args.</param>
		protected override void OnKeyUp(KeyRoutedEventArgs args)
		{
			var handled = args.Handled;
			if (handled || !_handlesKeyboardInput)
			{
				Cleanup();
				return;
			}

			var key = args.Key;
			handled = OnKeyUpInternal(key);
			if (handled)
			{
				args.Handled = true;
			}

			Cleanup();

			void Cleanup()
			{
				base.OnKeyUp(args);
			}
		}

		/// <summary>
		/// Handle key up events.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <returns>Handled?</returns>
		private bool OnKeyUpInternal(VirtualKey key)
		{
			KeyProcess.KeyUp(key, out var handled, _keyboardNavigationAcceptsReturn, this);
			return handled;
		}

		protected override void OnPointerEntered(PointerRoutedEventArgs args)
		{
			IsPointerOver = true;

			using (var suspender = new StateChangeSuspender(this))
			{

				if (ClickMode == ClickMode.Hover && IsEnabled)
				{
					IsPressed = true;
					OnClick();
				}
			}

			base.OnPointerEntered(args);
		}

		protected override void OnPointerExited(PointerRoutedEventArgs e)
		{
			IsPointerOver = false;

			using (var suspender = new StateChangeSuspender(this))
			{

				if (ClickMode == ClickMode.Hover && IsEnabled)
				{
					IsPressed = false;
				}
			}

			base.OnPointerExited(e);
		}

		/// <summary>
		/// Determine if the pointer is above the button based on its last known position.
		/// </summary>
		/// <returns>Is valid?</returns>
		private bool IsValidPointerPosition()
		{
			// This method is used to check mouse position after a mouse down and a drag. If the mouse moves outside the bounds
			// of the button, then we treat it as not pressed anymore. The cached _pointerPosition comes from Windows.UI.Input's
			// PointerPoint.Position, which uses a HimetricPoint that has subpixel precision, beyond even the plateau scale.
			// All other hit tests in Xaml use pixels or DIPs that have been rounded before Xaml receives the point. The HimetricPoint
			// can have precision issues after being converted to DIPs, so we allow a tolerance when doing the bounds comparison here.
			const double tolerance = 0.05;

			return
				-tolerance <= _pointerPosition.X && _pointerPosition.X <= ActualWidth + tolerance &&
				-tolerance <= _pointerPosition.Y && _pointerPosition.Y <= ActualHeight + tolerance;
		}

		/// <summary>
		/// PointerMoved event handler.
		/// </summary>
		/// <param name="args">Event args.</param>
		protected override void OnPointerMoved(PointerRoutedEventArgs args)
		{
			// Cache the last pointer position
			var pointerPoint = args.GetCurrentPoint(this);
			_pointerPosition = pointerPoint.Position;

			// Determine if the button is still pressed near the pointer's position			
			if (_isPointerLeftButtonDown &&
				IsEnabled &&
				ClickMode != ClickMode.Hover &&
				_isPointerCaptured &&
				!_isSpaceOrEnterKeyDown && !_isNavigationAcceptOrGamepadAKeyDown)
			{
				var isValid = IsValidPointerPosition();
				IsPressed = isValid;
			}

			base.OnPointerMoved(args);
		}

		/// <summary>
		/// Capture the pointer.
		/// </summary>
		/// <param name="pointer">Pointer.</param>
		private void CapturePointerInternal(Pointer pointer)
		{
			if (!_isPointerCaptured)
			{
				_isPointerCaptured = CapturePointer(pointer);
			}
		}

		/// <summary>
		/// PointerPressed event handler.
		/// </summary>
		/// <param name="args">Event args.</param>
		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			if (args.Handled)
			{
				Cleanup();
				return;
			}

			var pointerPoint = args.GetCurrentPoint(this);
			var pointerProperties = pointerPoint.Properties;
			var isLeftButtonPressed = pointerProperties.IsLeftButtonPressed;

			if (isLeftButtonPressed)
			{
				_isPointerLeftButtonDown = true;

				// Note: even if ClickMode is Press, we capture the pointer and handle the Release args, but we do nothing if Hover

				// Capturing the Pointer ensures that we will be the first element to receive the pointer released event
				// It will also ensure that if we scroll while pressing the button, as the capture will be lost, we won't raise Click.

				if (!IsEnabled || ClickMode == ClickMode.Hover)
				{
					Cleanup();
					return;
				}

				args.Handled = true;

				using (var suspender = new StateChangeSuspender(this))
				{
					Focus(FocusState.Pointer);

					CapturePointerInternal(args.Pointer);
					if (_isPointerCaptured)
					{
						IsPressed = true;
					}
				}

				if (ClickMode == ClickMode.Press)
				{
					OnClick();
				}
			}

			Cleanup();
			void Cleanup()
			{
				base.OnPointerPressed(args);
			}
		}

		/// <inheritdoc />
		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			if (args.Handled)
			{
				Cleanup();
				return;
			}

			// This should be changed to Pointer down and only occur when pointer is down or mouse left button is down.
			_isPointerLeftButtonDown = false;

			if (!IsEnabled || ClickMode == ClickMode.Hover)
			{
				Cleanup();
				return;
			}

			// We can not put all our logic for this event inside an if block conditioning to this BOOLEAN, as we need to
			// release our pointer capture and set is pressed to false depending solely on m_bIsSpaceOrEnterKeyDown
			_shouldPerformActions = (IsPressed && !_isSpaceOrEnterKeyDown && !_isNavigationAcceptOrGamepadAKeyDown);

			_pointerForPendingRightTapped = null;
			if (!_isSpaceOrEnterKeyDown && !_isNavigationAcceptOrGamepadAKeyDown)
			{
				IsPressed = false;
				_pointerForPendingRightTapped = args.Pointer;
			}
			//TODO Uno: GestureFollowing is not implemented yet.
			var gestureFollowing = args.GestureFollowing;
			if (gestureFollowing == GestureModes.RightTapped)
			{
				// This will be released OnRightTappedUnhandled or destructor.
				Cleanup();
				return;
			}

			// No right tap is pending. Note that we are intentionally NOT handling the args
			// if we do not fall through here because basically we are no_opting in that case.
			args.Handled = true;
			PerformPointerUpAction();
			if (!_isSpaceOrEnterKeyDown && !_isNavigationAcceptOrGamepadAKeyDown)
			{
				var pointer = args.Pointer;
				ReleasePointerCaptureInternal(pointer);

#if HAS_UNO
				//TODO Uno: Releasing pointer capture resets the handled status to false
				//while this does not seem to happen in UWP for this scenario.
				args.Handled = true;
#endif
			}

			Cleanup();

			void Cleanup()
			{
				base.OnPointerReleased(args);
			}
		}

		private void PerformPointerUpAction()
		{
			if (ClickMode == ClickMode.Release && _shouldPerformActions)
			{
				OnClick();
			}

			_shouldPerformActions = false;
		}

		/// <summary>
		/// PointerCaptureLost event handler.
		/// </summary>
		/// <param name="args">Event args.</param>
		protected override void OnPointerCaptureLost(PointerRoutedEventArgs args)
		{
			base.OnPointerCaptureLost(args);

			var pointer = args.Pointer;
			ReleasePointerCaptureInternal(pointer);

			// For touch, we can clear PointerOver when receiving PointerCaptureLost, which we get when the finger is lifted
			// or from cancellation, e.g. pinch-zoom gesture in ScrollViewer.
			// For mouse, we need to wait for PointerExited because the mouse may still be above the ButtonBase when
			// PointerCaptureLost is received from clicking.
			var pointerPoint = args.GetCurrentPoint(null);
			var pointerDeviceType = (PointerDeviceType)pointerPoint.PointerDeviceType;
			if (pointerDeviceType == PointerDeviceType.Touch)
			{
				// Uno TODO
				//IsPointerOver = false;
			}

			using var suspender = new StateChangeSuspender(this);
			IsPressed = false;
		}

		private protected override void OnRightTappedUnhandled(RightTappedRoutedEventArgs args)
		{
			base.OnRightTappedUnhandled(args);

			if (args.Handled)
			{
				Cleanup();
				return;
			}

			PerformPointerUpAction();
			ReleasePointerCaptureInternal(_pointerForPendingRightTapped);

			Cleanup();

			void Cleanup()
			{
				_pointerForPendingRightTapped = null;
			}
		}

		/// <summary>
		/// Apply a template to the ButtonBase.
		/// </summary>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Sync the logical and visual states of the control.
			UpdateVisualState(false);
		}

		/// <summary>
		/// Raises the Click routed event.
		/// </summary>
		private protected virtual void OnClick()
		{
			// Request a play invoke sound for Click event. This call needs to be made before the click event is raised.
			// This is because RequestInteractionSoundForElement expects the element to be in the tree.
			// And some click handlers remove the button from the tree e.g. an accept/cancel button on a Popup or Flyout.
			ElementSoundPlayerService.RequestInteractionSoundForElementStatic(ElementSoundKind.Invoke, this);

			// Create the args
			var args = new RoutedEventArgs();
			args.OriginalSource = this;

			// Raise the event
			Click?.Invoke(this, args);

			// Execute command associated with the button
			ExecuteCommand();
		}

		internal void ProgrammaticClick() => OnClick();

		private protected void SetAcceptsReturn(bool value) => _keyboardNavigationAcceptsReturn = value;
	}
}
