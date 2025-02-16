// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference MenuBarItem.cpp, tag winui3/release/1.4.2

using System;
using Microsoft/* UWP don't rename */.UI.Xaml.Automation.Peers;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Microsoft.UI.Input;
using Uno;
using AutomationPeer = Windows.UI.Xaml.Automation.Peers.AutomationPeer;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Items))]
	public partial class MenuBarItem : Control
	{
		private Button m_button;
		private MenuBarItemFlyout m_flyout;
		private WeakReference<DependencyObject> m_passThroughElement = new WeakReference<DependencyObject>(null);
		private WeakReference<MenuBar> m_menuBar;
		private bool m_isFlyoutOpen;

		private SerialDisposable m_presenterKeyDownRevoker = new SerialDisposable();
		private SerialDisposable m_flyoutClosedRevoker = new SerialDisposable();
		private SerialDisposable m_flyoutOpeningRevoker = new SerialDisposable();
		private SerialDisposable m_pointerEnteredRevoker = new SerialDisposable();
		private SerialDisposable m_accessKeyInvokedRevoker = new SerialDisposable();

		private IDisposable m_onMenuBarItemPointerPressedRevoker;
		private IDisposable m_onMenuBarItemKeyDownRevoker;

		private IDisposable m_pressedRevoker;
		private IDisposable m_pointerOverRevoker;


		public MenuBarItem()
		{
			DefaultStyleKey = typeof(MenuBarItem);

			var items = new ObservableVector<MenuFlyoutItemBase>();
			var observableVector = items as IObservableVector<MenuFlyoutItemBase>;
			observableVector.VectorChanged += OnItemsVectorChanged;
			SetValue(ItemsProperty, observableVector);

			// Uno Specific: make sure to only subscribe to events while loaded
			Loaded += (_, _) => OnApplyTemplate();
			Unloaded += (_, _) => DetachEventHandlers();
		}

		// IUIElement / IUIElementOverridesHelper
		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new MenuBarItemAutomationPeer(this);
		}

		// IFramework Override
		protected override void OnApplyTemplate()
		{
			m_button = GetTemplateChild("ContentButton") as Button;

			var menuBar = SharedHelpers.GetAncestorOfType<MenuBar>(VisualTreeHelper.GetParent(this));
			if (menuBar is { })
			{
				m_menuBar = new WeakReference<MenuBar>(menuBar);
				// Ask parent MenuBar for its root to enable pass through
				menuBar.RequestPassThroughElement(this);
			}

			PopulateContent();
			DetachEventHandlers();
			AttachEventHandlers();
		}

		private void PopulateContent()
		{
			// Create flyout
			var flyout = new MenuBarItemFlyout();

			foreach (var flyoutItem in Items)
			{
				flyout.Items.Add(flyoutItem);
			}

			flyout.Placement = FlyoutPlacementMode.Bottom;

			if (m_passThroughElement?.TryGetTarget(out var passThroughElement) ?? false)
			{
				flyout.OverlayInputPassThroughElement = passThroughElement;
			}
			m_flyout = flyout;

			if (m_button is { } button)
			{
				button.IsAccessKeyScope = true;
				button.ContextFlyout = flyout;
			}

			// Uno specific
			SetFlyoutDataContext();
		}

		// Uno specific
		internal protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);

			SetFlyoutDataContext();
		}

		// Uno-specific
		private void SetFlyoutDataContext()
		{
			// This is present to force the dataContext to be passed to the popup of the flyout since it is not directly a child in the visual tree of the flyout.
			m_flyout?.SetValue(
				MenuFlyout.DataContextProperty,
				this.DataContext,
				precedence: DependencyPropertyValuePrecedences.Inheritance
			);
		}

		private void AttachEventHandlers()
		{
			if (m_button != null)
			{
				m_pressedRevoker = m_button.RegisterDisposablePropertyChangedCallback(ButtonBase.IsPressedProperty, OnVisualPropertyChanged);
				m_pointerOverRevoker = m_button.RegisterDisposablePropertyChangedCallback(ButtonBase.IsPointerOverProperty, OnVisualPropertyChanged);
			}

			m_onMenuBarItemPointerPressedRevoker = new DisposableAction(() => RemoveHandler(PointerPressedEvent, (PointerEventHandler)OnMenuBarItemPointerPressed));
			AddHandler(
				PointerPressedEvent,
				(PointerEventHandler)OnMenuBarItemPointerPressed,
				true /*handledEventsToo*/
			);

			m_onMenuBarItemKeyDownRevoker = new DisposableAction(() => RemoveHandler(KeyDownEvent, (KeyEventHandler)OnMenuBarItemKeyDown));
			AddHandler(
				KeyDownEvent,
				(KeyEventHandler)OnMenuBarItemKeyDown,
				true /*handledEventsToo*/
			);

			if (m_flyout != null)
			{
				m_flyoutClosedRevoker.Disposable = new DisposableAction(() => m_flyout.Closed -= OnFlyoutClosed);
				m_flyout.Closed += OnFlyoutClosed;
				m_flyoutOpeningRevoker.Disposable = new DisposableAction(() => m_flyout.Opening -= OnFlyoutOpening);
				m_flyout.Opening += OnFlyoutOpening;
			}

			m_pointerEnteredRevoker.Disposable = new DisposableAction(() => PointerEntered -= OnMenuBarItemPointerEntered);
			PointerEntered += OnMenuBarItemPointerEntered;

			m_accessKeyInvokedRevoker.Disposable = new DisposableAction(() => AccessKeyInvoked -= OnMenuBarItemAccessKeyInvoked);
			AccessKeyInvoked += OnMenuBarItemAccessKeyInvoked;
		}

		private void DetachEventHandlers()
		{
			m_pressedRevoker?.Dispose();
			m_pointerOverRevoker?.Dispose();

			m_flyoutClosedRevoker.Dispose();
			m_flyoutOpeningRevoker.Dispose();

			m_onMenuBarItemPointerPressedRevoker?.Dispose();
			m_onMenuBarItemKeyDownRevoker?.Dispose();
		}

		// Event Handlers
		private void OnMenuBarItemPointerEntered(object sender, PointerRoutedEventArgs args)
		{
			if (m_menuBar.TryGetTarget(out var menuBar))
			{
				var flyoutOpen = menuBar.IsFlyoutOpen;
				if (flyoutOpen)
				{
					ShowMenuFlyout();
				}
			}
		}

		private void OnMenuBarItemPointerPressed(object sender, PointerRoutedEventArgs args)
		{
			if (m_menuBar.TryGetTarget(out var menuBar))
			{
				var flyoutOpen = menuBar.IsFlyoutOpen;
				if (!flyoutOpen)
				{
					ShowMenuFlyout();
				}
			}
		}

		private void OnMenuBarItemKeyDown(object sender, KeyRoutedEventArgs args)
		{
			var isAltDown = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;

			var key = args.Key;
			if (key == VirtualKey.Down
				|| key == VirtualKey.Enter
				|| key == VirtualKey.Space)
			{
				ShowMenuFlyout();
			}
			else if (key == VirtualKey.Right)
			{
				if (FlowDirection == FlowDirection.RightToLeft)
				{
					MoveFocusTo(FlyoutLocation.Left);
				}
				else
				{
					MoveFocusTo(FlyoutLocation.Right);
				}
				args.Handled = true;
			}
			else if (key == VirtualKey.Left)
			{
				if (FlowDirection == FlowDirection.RightToLeft)
				{
					MoveFocusTo(FlyoutLocation.Right);
				}
				else
				{
					MoveFocusTo(FlyoutLocation.Left);
				}
				args.Handled = true;
			}
		}

		private void OnPresenterKeyDown(object sender, KeyRoutedEventArgs args)
		{
			// If the event came from a MenuFlyoutSubItem it means right/left arrow will open it, so we should not handle them to not override default behaviour
			if (args.OriginalSource is MenuFlyoutSubItem subitem)
			{
				if (subitem.Items[0] is { })
				{
					return;
				}
			}

			var key = args.Key;
			if (key == VirtualKey.Right)
			{
				if (FlowDirection == FlowDirection.RightToLeft)
				{
					OpenFlyoutFrom(FlyoutLocation.Left);
				}
				else
				{
					OpenFlyoutFrom(FlyoutLocation.Right);
				}
			}
			else if (key == VirtualKey.Left)
			{
				if (FlowDirection == FlowDirection.RightToLeft)
				{
					OpenFlyoutFrom(FlyoutLocation.Right);
				}
				else
				{
					OpenFlyoutFrom(FlyoutLocation.Left);
				}
			}
		}

		private void OnItemsVectorChanged(IObservableVector<MenuFlyoutItemBase> sender, IVectorChangedEventArgs e)
		{
			if (m_flyout is { } flyout)
			{
				var index = e.Index;
				switch (e.CollectionChange)
				{
					case CollectionChange.ItemInserted:
						flyout.Items.Insert((int)index, Items[(int)index]);
						break;
					case CollectionChange.ItemRemoved:
						flyout.Items.RemoveAt((int)index);
						break;
					default:
						break;
				}
			}
		}

		private void OnMenuBarItemAccessKeyInvoked(DependencyObject sender, AccessKeyInvokedEventArgs args)
		{
			ShowMenuFlyout();
			args.Handled = true;
		}

		// Menu Flyout actions
		internal void ShowMenuFlyout()
		{
			if (Items.Count != 0)
			{
				if (m_button != null)
				{
					var width = m_button.ActualWidth;
					var height = m_button.ActualHeight;

					// Sets an exclusion rect over the button that generates the flyout so that even if the menu opens upwards
					// (which is the default in touch mode) it doesn't cover the menu bar button.
					FlyoutShowOptions options = new FlyoutShowOptions();
					options.Position = new Point(0, height);
					options.Placement = FlyoutPlacementMode.Bottom;
					options.ExclusionRect = new Rect(0, 0, width, height);
					m_flyout.ShowAt(m_button, options);

					// Attach keyboard event handler
					m_presenterKeyDownRevoker.Disposable = new DisposableAction(() => m_flyout.m_presenter.KeyDown -= OnPresenterKeyDown);
					m_flyout.m_presenter.KeyDown += OnPresenterKeyDown;
				}
			}
		}

		internal void CloseMenuFlyout()
		{
			m_flyout.Hide();
		}

		private void OpenFlyoutFrom(FlyoutLocation location)
		{
			if (m_menuBar.TryGetTarget(out var menuBar))
			{
				var index = menuBar.Items.IndexOf(this);
				CloseMenuFlyout();
				if (location == FlyoutLocation.Left)
				{
					menuBar.Items[((index - 1) + menuBar.Items.Count) % menuBar.Items.Count].ShowMenuFlyout();
				}
				else
				{
					menuBar.Items[(index + 1) % menuBar.Items.Count].ShowMenuFlyout();
				}
			}
		}

		private void MoveFocusTo(FlyoutLocation location)
		{
			if (m_menuBar.TryGetTarget(out var menuBar))
			{
				var index = menuBar.Items.IndexOf(this);
				if (location == FlyoutLocation.Left)
				{
					menuBar.Items[((index - 1) + menuBar.Items.Count) % menuBar.Items.Count].Focus(FocusState.Programmatic);
				}
				else
				{
					menuBar.Items[(index + 1) % menuBar.Items.Count].Focus(FocusState.Programmatic);
				}
			}
		}

		internal void AddPassThroughElement(DependencyObject element)
		{
			m_passThroughElement = new WeakReference<DependencyObject>(element);
		}

		public bool IsFlyoutOpen()
		{
			return m_isFlyoutOpen;
		}

		public void Invoke()
		{
			if (IsFlyoutOpen())
			{
				CloseMenuFlyout();
			}
			else
			{
				ShowMenuFlyout();
			}
		}

		// Menu Flyout Events
		private void OnFlyoutClosed(object sender, object args)
		{
			m_isFlyoutOpen = false;

			if (m_menuBar.TryGetTarget(out var menuBar))
			{
				menuBar.IsFlyoutOpen = false;
			}

			UpdateVisualStates();
		}

		private void OnFlyoutOpening(object sender, object args)
		{
			Focus(FocusState.Pointer);

			m_isFlyoutOpen = true;

			if (m_menuBar.TryGetTarget(out var menuBar))
			{
				menuBar.IsFlyoutOpen = true;
			}

			UpdateVisualStates();
		}

		private void OnVisualPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			UpdateVisualStates();
		}

		private void UpdateVisualStates()
		{
			if (m_button is { } button)
			{
				if (button.IsPressed)
				{
					VisualStateManager.GoToState(this, "Pressed", false);
				}
				else if (button.IsPointerOver)
				{
					VisualStateManager.GoToState(this, "PointerOver", false);
				}
				else
				{
					if (m_isFlyoutOpen)
					{
						VisualStateManager.GoToState(this, "Selected", false);
					}
					else
					{
						VisualStateManager.GoToState(this, "Normal", false);
					}
				}
			}
		}
	}
}
