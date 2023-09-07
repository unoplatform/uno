// MUX commit reference 36f8f8f6d5f11f414fdaa0462d0c4cb845cf4254

using Microsoft.UI.Private.Controls;
using Uno.UI.Helpers.WinUI;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

using SplitButtonAutomationPeer = Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers.SplitButtonAutomationPeer;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	public partial class SplitButton : ContentControl
	{
		private bool m_isKeyDown = false;
		private bool m_isFlyoutOpen = false;
		private PointerDeviceType m_lastPointerDeviceType = PointerDeviceType.Mouse;
		private Button m_primaryButton = null;
		private Button m_secondaryButton = null;

		private long m_flyoutPlacementChangedRevoker;
		private long m_pressedPrimaryRevoker;
		private long m_pointerOverPrimaryRevoker;
		private long m_pressedSecondaryRevoker;
		private long m_pointerOverSecondaryRevoker;

		internal bool m_hasLoaded = false;

		public SplitButton()
		{
			DefaultStyleKey = typeof(SplitButton);

			KeyDown += OnSplitButtonKeyDown;
			KeyUp += OnSplitButtonKeyUp;
		}

		public
#if __ANDROID__
			new
#endif
			event Windows.Foundation.TypedEventHandler<SplitButton, SplitButtonClickEventArgs> Click;

		protected override void OnApplyTemplate()
		{
			UnregisterEvents();

			m_primaryButton = this.GetTemplateChild("PrimaryButton") as Button;
			m_secondaryButton = this.GetTemplateChild("SecondaryButton") as Button;

			var primaryButton = m_primaryButton;
			if (primaryButton != null)
			{
				primaryButton.Click += OnClickPrimary;

				m_pressedPrimaryRevoker = primaryButton.RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, OnVisualPropertyChanged);
				m_pointerOverPrimaryRevoker = primaryButton.RegisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, OnVisualPropertyChanged);

				// Register for pointer events so we can keep track of the last used pointer type
				primaryButton.PointerEntered += OnPointerEvent;
				primaryButton.PointerExited += OnPointerEvent;
				primaryButton.PointerPressed += OnPointerEvent;
				primaryButton.PointerReleased += OnPointerEvent;
				primaryButton.PointerCanceled += OnPointerEvent;
				primaryButton.PointerCaptureLost += OnPointerEvent;
			}

			var secondaryButton = m_secondaryButton;
			if (secondaryButton != null)
			{
				// Do localization for the secondary button
				var secondaryName = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_SplitButtonSecondaryButtonName);
				AutomationProperties.SetName(secondaryButton, secondaryName);

				secondaryButton.Click += OnClickSecondary;

				m_pressedSecondaryRevoker = secondaryButton.RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, OnVisualPropertyChanged);
				m_pointerOverSecondaryRevoker = secondaryButton.RegisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, OnVisualPropertyChanged);

				// Register for pointer events so we can keep track of the last used pointer type
				secondaryButton.PointerEntered += OnPointerEvent;
				secondaryButton.PointerExited += OnPointerEvent;
				secondaryButton.PointerPressed += OnPointerEvent;
				secondaryButton.PointerReleased += OnPointerEvent;
				secondaryButton.PointerCanceled += OnPointerEvent;
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
				if (args.OldValue is Flyout oldFlyout)
				{
					oldFlyout.Opened -= OnFlyoutOpened;
					oldFlyout.Closed -= OnFlyoutClosed;
					oldFlyout.UnregisterPropertyChangedCallback(FlyoutBase.PlacementProperty, m_flyoutPlacementChangedRevoker);
				}
				OnFlyoutChanged();
			}
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new SplitButtonAutomationPeer(this);
		}

		void OnFlyoutChanged()
		{
			RegisterFlyoutEvents();

			UpdateVisualStates();
		}

		void RegisterFlyoutEvents()
		{
			if (Flyout != null)
			{
				Flyout.Opened += OnFlyoutOpened;
				Flyout.Closed += OnFlyoutClosed;
				m_flyoutPlacementChangedRevoker = Flyout.RegisterPropertyChangedCallback(FlyoutBase.PlacementProperty, OnFlyoutPlacementChanged);
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
			if (m_lastPointerDeviceType == PointerDeviceType.Touch || m_isKeyDown)
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
			if (primaryButton != null && secondaryButton != null)
			{
				if (m_isFlyoutOpen)
				{
					VisualStateManager.GoToState(this, "FlyoutOpen", useTransitions);
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
			var flyout = Flyout;
			if (flyout != null)
			{
				if (SharedHelpers.IsFlyoutShowOptionsAvailable())
				{
					FlyoutShowOptions options = new FlyoutShowOptions();
					options.Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
					flyout.ShowAt(this, options);
				}
				else
				{
					flyout.ShowAt(this);
				}
			}
		}

		internal void CloseFlyout()
		{
			var flyout = Flyout;
			if (flyout != null)
			{
				flyout.Hide();
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

		internal virtual void OnClickPrimary(object sender, RoutedEventArgs args)
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
					args.Handled = true;
				}
			}
			else if (key == VirtualKey.Down)
			{
				CoreVirtualKeyStates menuState = KeyboardStateTracker.GetKeyState(VirtualKey.Menu);
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

			if (m_primaryButton != null)
			{
				m_primaryButton.Click -= OnClickPrimary;

				m_primaryButton.UnregisterPropertyChangedCallback(ButtonBase.IsPressedProperty, m_pressedPrimaryRevoker);
				m_primaryButton.UnregisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, m_pointerOverPrimaryRevoker);

				m_primaryButton.PointerEntered -= OnPointerEvent;
				m_primaryButton.PointerExited -= OnPointerEvent;
				m_primaryButton.PointerPressed -= OnPointerEvent;
				m_primaryButton.PointerReleased -= OnPointerEvent;
				m_primaryButton.PointerCanceled -= OnPointerEvent;
				m_primaryButton.PointerCaptureLost -= OnPointerEvent;
			}

			if (m_secondaryButton != null)
			{
				m_secondaryButton.Click -= OnClickSecondary;
				m_secondaryButton.UnregisterPropertyChangedCallback(ButtonBase.IsPressedProperty, m_pressedSecondaryRevoker);
				m_secondaryButton.UnregisterPropertyChangedCallback(ButtonBase.IsPointerOverProperty, m_pointerOverSecondaryRevoker);

				m_secondaryButton.PointerEntered -= OnPointerEvent;
				m_secondaryButton.PointerExited -= OnPointerEvent;
				m_secondaryButton.PointerPressed -= OnPointerEvent;
				m_secondaryButton.PointerReleased -= OnPointerEvent;
				m_secondaryButton.PointerCanceled -= OnPointerEvent;
				m_secondaryButton.PointerCaptureLost -= OnPointerEvent;
			}
		}

		internal bool IsFlyoutOpen => m_isFlyoutOpen;
	}
}
