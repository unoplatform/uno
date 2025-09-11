// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference SplitButton.cpp, tag winui3/release/1.4.2

using Microsoft.UI.Private.Controls;
using Uno.UI.Helpers.WinUI;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno;
using Uno.Disposables;
using Uno.UI.Core;
using Windows.Foundation;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

using SplitButtonAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.SplitButtonAutomationPeer;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class SplitButton : ContentControl
	{
		private Button m_primaryButton = null;
		private Button m_secondaryButton = null;

		private bool m_isFlyoutOpen = false;
		private PointerDeviceType m_lastPointerDeviceType = PointerDeviceType.Mouse;
		private bool m_isKeyDown = false;

		// Uno Doc: no need for revokers when we're subscribing to events on the element itself in the constructor once
		// winrt::UIElement::KeyDown_revoker m_keyDownRevoker{};
		// winrt::UIElement::KeyUp_revoker m_keyUpRevoker{};

		private SerialDisposable m_clickPrimaryRevoker = new();
		private SerialDisposable m_pressedPrimaryRevoker = new();
		private SerialDisposable m_pointerOverPrimaryRevoker = new();

		private SerialDisposable m_pointerEnteredPrimaryRevoker = new();
		private SerialDisposable m_pointerExitedPrimaryRevoker = new();
		private SerialDisposable m_pointerPressedPrimaryRevoker = new();
		private SerialDisposable m_pointerReleasedPrimaryRevoker = new();
		private SerialDisposable m_pointerCanceledPrimaryRevoker = new();
		private SerialDisposable m_pointerCaptureLostPrimaryRevoker = new();

		private SerialDisposable m_clickSecondaryRevoker = new();
		private SerialDisposable m_pressedSecondaryRevoker = new();
		private SerialDisposable m_pointerOverSecondaryRevoker = new();

		private SerialDisposable m_pointerEnteredSecondaryRevoker = new();
		private SerialDisposable m_pointerExitedSecondaryRevoker = new();
		private SerialDisposable m_pointerPressedSecondaryRevoker = new();
		private SerialDisposable m_pointerReleasedSecondaryRevoker = new();
		private SerialDisposable m_pointerCanceledSecondaryRevoker = new();
		private SerialDisposable m_pointerCaptureLostSecondaryRevoker = new();

		private SerialDisposable m_flyoutOpenedRevoker = new();
		private SerialDisposable m_flyoutClosedRevoker = new();
		private SerialDisposable m_flyoutPlacementChangedRevoker = new();

		protected bool m_hasLoaded = false;

		public SplitButton()
		{
			this.SetDefaultStyleKey();

			KeyDown += OnSplitButtonKeyDown;
			KeyUp += OnSplitButtonKeyUp;

			// Uno Specific: prevent leaks
			Loaded += (_, _) => OnApplyTemplate();
			Unloaded += (_, _) => UnregisterEvents();
		}

		public
#if __ANDROID__
			new
#endif
			event TypedEventHandler<SplitButton, SplitButtonClickEventArgs> Click;

		protected override void OnApplyTemplate()
		{
			UnregisterEvents();

			m_primaryButton = GetTemplateChild("PrimaryButton") as Button;
			m_secondaryButton = GetTemplateChild("SecondaryButton") as Button;

			if (m_primaryButton is { } primaryButton)
			{
				m_clickPrimaryRevoker.Disposable = new DisposableAction(() => primaryButton.Click -= OnClickPrimary);
				primaryButton.Click += OnClickPrimary;

				var pressedPrimaryToken = primaryButton.RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, OnVisualPropertyChanged);
				m_pressedPrimaryRevoker.Disposable = new DisposableAction(() => primaryButton.UnregisterPropertyChangedCallback(ButtonBase.IsPressedProperty, pressedPrimaryToken));
				var pointerOverPrimaryToken = primaryButton.RegisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, OnVisualPropertyChanged);
				m_pointerOverPrimaryRevoker.Disposable = new DisposableAction(() => primaryButton.UnregisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, pointerOverPrimaryToken));

				// Register for pointer events so we can keep track of the last used pointer type
				m_pointerEnteredPrimaryRevoker.Disposable = new DisposableAction(() => primaryButton.PointerEntered -= OnPointerEvent);
				primaryButton.PointerEntered += OnPointerEvent;
				m_pointerExitedPrimaryRevoker.Disposable = new DisposableAction(() => primaryButton.PointerExited -= OnPointerEvent);
				primaryButton.PointerExited += OnPointerEvent;
				m_pointerPressedPrimaryRevoker.Disposable = new DisposableAction(() => primaryButton.PointerPressed -= OnPointerEvent);
				primaryButton.PointerPressed += OnPointerEvent;
				m_pointerReleasedPrimaryRevoker.Disposable = new DisposableAction(() => primaryButton.PointerReleased -= OnPointerEvent);
				primaryButton.PointerReleased += OnPointerEvent;
				m_pointerCanceledPrimaryRevoker.Disposable = new DisposableAction(() => primaryButton.PointerCanceled -= OnPointerEvent);
				primaryButton.PointerCanceled += OnPointerEvent;
				m_pointerCaptureLostPrimaryRevoker.Disposable = new DisposableAction(() => primaryButton.PointerCaptureLost -= OnPointerEvent);
				primaryButton.PointerCaptureLost += OnPointerEvent;
			}

			if (m_secondaryButton is { } secondaryButton)
			{
				// Do localization for the secondary button
				var secondaryName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_SplitButtonSecondaryButtonName);
				AutomationProperties.SetName(secondaryButton, secondaryName);

				m_clickSecondaryRevoker.Disposable = new DisposableAction(() => secondaryButton.Click -= OnClickSecondary);
				secondaryButton.Click += OnClickSecondary;

				var pressedSecondaryToken = secondaryButton.RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, OnVisualPropertyChanged);
				m_pressedSecondaryRevoker.Disposable = new DisposableAction(() => secondaryButton.UnregisterPropertyChangedCallback(ButtonBase.IsPressedProperty, pressedSecondaryToken));
				var pointerOverSecondaryToken = secondaryButton.RegisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, OnVisualPropertyChanged);
				m_pointerOverSecondaryRevoker.Disposable = new DisposableAction(() => secondaryButton.UnregisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, pointerOverSecondaryToken));

				// Register for pointer events so we can keep track of the last used pointer type
				m_pointerEnteredSecondaryRevoker.Disposable = new DisposableAction(() => secondaryButton.PointerEntered -= OnPointerEvent);
				secondaryButton.PointerEntered += OnPointerEvent;
				m_pointerExitedSecondaryRevoker.Disposable = new DisposableAction(() => secondaryButton.PointerExited -= OnPointerEvent);
				secondaryButton.PointerExited += OnPointerEvent;
				m_pointerPressedSecondaryRevoker.Disposable = new DisposableAction(() => secondaryButton.PointerPressed -= OnPointerEvent);
				secondaryButton.PointerPressed += OnPointerEvent;
				m_pointerReleasedSecondaryRevoker.Disposable = new DisposableAction(() => secondaryButton.PointerReleased -= OnPointerEvent);
				secondaryButton.PointerReleased += OnPointerEvent;
				m_pointerCanceledSecondaryRevoker.Disposable = new DisposableAction(() => secondaryButton.PointerCanceled -= OnPointerEvent);
				secondaryButton.PointerCanceled += OnPointerEvent;
				m_pointerCaptureLostSecondaryRevoker.Disposable = new DisposableAction(() => secondaryButton.PointerCaptureLost -= OnPointerEvent);
				secondaryButton.PointerCaptureLost += OnPointerEvent;
			}

			// Register events on flyout
			RegisterFlyoutEvents();

			UpdateVisualStates();

			m_hasLoaded = true;
		}


		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			DependencyProperty property = args.Property;

			if (property == FlyoutProperty)
			{
				OnFlyoutChanged();
			}
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new SplitButtonAutomationPeer(this);
		}

		private void OnFlyoutChanged()
		{
			RegisterFlyoutEvents();

			UpdateVisualStates();
		}

		private void RegisterFlyoutEvents()
		{
			m_flyoutOpenedRevoker.Dispose();
			m_flyoutClosedRevoker.Dispose();
			m_flyoutPlacementChangedRevoker.Dispose();

			// Uno Specific: use a local variable instead of the property to capture the flyout reference even if Flyout changes
			if (Flyout is { } flyout)
			{
				m_flyoutOpenedRevoker.Disposable = new DisposableAction(() => flyout.Opened -= OnFlyoutOpened);
				flyout.Opened += OnFlyoutOpened;
				m_flyoutClosedRevoker.Disposable = new DisposableAction(() => flyout.Closed -= OnFlyoutClosed);
				flyout.Closed += OnFlyoutClosed;
				var flyoutPlacementChangedToken = flyout.RegisterPropertyChangedCallback(FlyoutBase.PlacementProperty, OnFlyoutPlacementChanged);
				m_flyoutPlacementChangedRevoker.Disposable = new DisposableAction(() => flyout.UnregisterPropertyChangedCallback(FlyoutBase.PlacementProperty, flyoutPlacementChangedToken));
			}
		}

		private void OnVisualPropertyChanged(DependencyObject sender, DependencyProperty args)
		{
			UpdateVisualStates();
		}

		internal virtual bool InternalIsChecked() => false;

		internal void UpdateVisualStates(bool useTransitions = true)
		{
			// place the secondary button
			if (m_isKeyDown)
			{
				VisualStateManager.GoToState(this, "SecondaryButtonSpan", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(this, "SecondaryButtonRight", useTransitions);
			}

			// change visual state
			var primaryButton = m_primaryButton;
			var secondaryButton = m_secondaryButton;
			if (!IsEnabled)
			{
				VisualStateManager.GoToState(this, "Disabled", useTransitions);
			}
			else if (primaryButton != null && secondaryButton != null)
			{
				if (m_isFlyoutOpen)
				{
					if (InternalIsChecked())
					{
						VisualStateManager.GoToState(this, "CheckedFlyoutOpen", useTransitions);
					}
					else
					{
						VisualStateManager.GoToState(this, "FlyoutOpen", useTransitions);
					}
				}
				// SplitButton and ToggleSplitButton share a template -- this section is driving the checked states for Toggle
				else if (InternalIsChecked())
				{
					if (m_lastPointerDeviceType == PointerDeviceType.Touch || m_isKeyDown)
					{
						if (primaryButton.IsPressed || secondaryButton.IsPressed || m_isKeyDown)
						{
							VisualStateManager.GoToState(this, "CheckedTouchPressed", useTransitions);
						}
						else
						{
							VisualStateManager.GoToState(this, "Checked", useTransitions);
						}
					}
					else if (primaryButton.IsPressed)
					{
						VisualStateManager.GoToState(this, "CheckedPrimaryPressed", useTransitions);
					}
					else if (primaryButton.IsPointerOver)
					{
						VisualStateManager.GoToState(this, "CheckedPrimaryPointerOver", useTransitions);
					}
					else if (secondaryButton.IsPressed)
					{
						VisualStateManager.GoToState(this, "CheckedSecondaryPressed", useTransitions);
					}
					else if (secondaryButton.IsPointerOver)
					{
						VisualStateManager.GoToState(this, "CheckedSecondaryPointerOver", useTransitions);
					}
					else
					{
						VisualStateManager.GoToState(this, "Checked", useTransitions);
					}
				}
				else
				{
					if (m_lastPointerDeviceType == PointerDeviceType.Touch || m_isKeyDown)
					{
						if (primaryButton.IsPressed || secondaryButton.IsPressed || m_isKeyDown)
						{
							VisualStateManager.GoToState(this, "TouchPressed", useTransitions);
						}
						else
						{
							VisualStateManager.GoToState(this, "Normal", useTransitions);
						}
					}
					else if (primaryButton.IsPressed)
					{
						VisualStateManager.GoToState(this, "PrimaryPressed", useTransitions);
					}
					else if (primaryButton.IsPointerOver)
					{
						VisualStateManager.GoToState(this, "PrimaryPointerOver", useTransitions);
					}
					else if (secondaryButton.IsPressed)
					{
						VisualStateManager.GoToState(this, "SecondaryPressed", useTransitions);
					}
					else if (secondaryButton.IsPointerOver)
					{
						VisualStateManager.GoToState(this, "SecondaryPointerOver", useTransitions);
					}
					else
					{
						VisualStateManager.GoToState(this, "Normal", useTransitions);
					}
				}
			}
		}

		internal void OpenFlyout()
		{
			if (Flyout is { } flyout)
			{
				FlyoutShowOptions options = new FlyoutShowOptions();
				options.Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
				flyout.ShowAt(this, options);
			}
		}

		internal void CloseFlyout()
		{
			if (Flyout is { } flyout)
			{
				flyout.Hide();
			}
		}

		private void ExecuteCommand()
		{
			if (Command is { } command)
			{
				var commandParameter = CommandParameter;

				if (command.CanExecute(commandParameter))
				{
					command.Execute(commandParameter);
				}
			}
		}

		private void OnFlyoutOpened(object sender, object args)
		{
			m_isFlyoutOpen = true;
			UpdateVisualStates();
			SharedHelpers.RaiseAutomationPropertyChangedEvent(this, ExpandCollapseState.Collapsed, ExpandCollapseState.Expanded);
		}

		private void OnFlyoutClosed(object sender, object args)
		{
			m_isFlyoutOpen = false;
			UpdateVisualStates();
			SharedHelpers.RaiseAutomationPropertyChangedEvent(this, ExpandCollapseState.Expanded, ExpandCollapseState.Collapsed);
		}

		private void OnFlyoutPlacementChanged(DependencyObject sender, DependencyProperty dp)
		{
			UpdateVisualStates();
		}

		protected virtual void OnClickPrimary(object sender, RoutedEventArgs args)
		{
			var eventArgs = new SplitButtonClickEventArgs();
			Click?.Invoke(this, eventArgs);

			AutomationPeer peer = FrameworkElementAutomationPeer.FromElement(this);
			if (peer != null)
			{
				peer.RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
			}
		}

		private void OnClickSecondary(object sender, RoutedEventArgs args)
		{
			OpenFlyout();
		}

		internal void Invoke()
		{
			bool invoked = false;

			if (FrameworkElementAutomationPeer.FromElement(m_primaryButton) is AutomationPeer peer)
			{
				if (peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invokeProvider)
				{
					invokeProvider.Invoke();
					invoked = true;
				}
			}

			// If we don't have a primary button that provides an invoke provider, we'll fall back to calling OnClickPrimary manually.
			if (!invoked)
			{
				OnClickPrimary(null, null);
			}
		}

		private void OnPointerEvent(object sender, PointerRoutedEventArgs args)
		{
			PointerDeviceType pointerDeviceType = args.Pointer.PointerDeviceType;

			// Allows the test app to simulate how the control responds to touch input.
			if (SplitButtonTestHelper.SimulateTouch)
			{
				pointerDeviceType = PointerDeviceType.Touch;
			}

			if (m_lastPointerDeviceType != pointerDeviceType)
			{
				m_lastPointerDeviceType = pointerDeviceType;
				UpdateVisualStates();
			}
		}

		private void OnSplitButtonKeyDown(object sender, KeyRoutedEventArgs args)
		{
			VirtualKey key = args.Key;
			if (key == VirtualKey.Space || key == VirtualKey.Enter || key == VirtualKey.GamepadA)
			{
				m_isKeyDown = true;
				UpdateVisualStates();
			}
		}

		private void OnSplitButtonKeyUp(object sender, KeyRoutedEventArgs args)
		{
			VirtualKey key = args.Key;
			if (key == VirtualKey.Space || key == VirtualKey.Enter || key == VirtualKey.GamepadA)
			{
				m_isKeyDown = false;
				UpdateVisualStates();

				// Consider this a click on the primary button
				if (IsEnabled)
				{
					OnClickPrimary(null, null);
					ExecuteCommand();
					args.Handled = true;
				}
			}
			else if (key == VirtualKey.Down)
			{
				CoreVirtualKeyStates menuState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
				bool menuKeyDown = (menuState & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

				if (IsEnabled && menuKeyDown)
				{
					// Open the menu on alt-down
					OpenFlyout();
					args.Handled = true;
				}
			}
			else if (key == VirtualKey.F4 && IsEnabled)
			{
				// Open the menu on F4
				OpenFlyout();
				args.Handled = true;
			}
		}

		private void UnregisterEvents()
		{
			// This explicitly unregisters all events related to the two buttons in OnApplyTemplate
			// in case the new template doesn't have all the expected elements.

			m_clickPrimaryRevoker.Dispose();
			m_pressedPrimaryRevoker.Dispose();
			m_pointerOverPrimaryRevoker.Dispose();

			m_pointerEnteredPrimaryRevoker.Dispose();
			m_pointerExitedPrimaryRevoker.Dispose();
			m_pointerPressedPrimaryRevoker.Dispose();
			m_pointerReleasedPrimaryRevoker.Dispose();
			m_pointerCanceledPrimaryRevoker.Dispose();
			m_pointerCaptureLostPrimaryRevoker.Dispose();

			m_clickSecondaryRevoker.Dispose();
			m_pressedSecondaryRevoker.Dispose();
			m_pointerOverSecondaryRevoker.Dispose();

			m_pointerEnteredSecondaryRevoker.Dispose();
			m_pointerExitedSecondaryRevoker.Dispose();
			m_pointerPressedSecondaryRevoker.Dispose();
			m_pointerReleasedSecondaryRevoker.Dispose();
			m_pointerCanceledSecondaryRevoker.Dispose();
			m_pointerCaptureLostSecondaryRevoker.Dispose();
		}

		internal bool IsFlyoutOpen => m_isFlyoutOpen;
	}
}
