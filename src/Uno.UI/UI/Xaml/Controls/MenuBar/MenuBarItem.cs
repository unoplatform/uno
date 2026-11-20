// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference MenuBarItem.cpp, MenuBarItem.h

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls;

[ContentProperty(Name = nameof(Items))]
public partial class MenuBarItem : Control
{
	// Variables
	private Button m_button;
	private MenuBarItemFlyout m_flyout;
	private WeakReference<DependencyObject> m_passThroughElement;
	private WeakReference<MenuBar> m_menuBar;
	private bool m_isFlyoutOpen;

	// Event tokens and handlers
	private readonly SerialDisposable m_presenterKeyDownRevoker = new();
	private readonly SerialDisposable m_flyoutClosedRevoker = new();
	private readonly SerialDisposable m_flyoutOpeningRevoker = new();
	private readonly SerialDisposable m_pointerEnteredRevoker = new();
	private readonly SerialDisposable m_accessKeyInvokedRevoker = new();

	private readonly SerialDisposable m_onMenuBarItemPointerPressedRevoker = new();
	private readonly SerialDisposable m_onMenuBarItemKeyDownRevoker = new();

	private readonly SerialDisposable m_pressedRevoker = new();
	private readonly SerialDisposable m_pointerOverRevoker = new();

	public MenuBarItem()
	{
		this.SetDefaultStyleKey();

		var items = new ObservableVector<MenuFlyoutItemBase>();
		items.VectorChanged += OnItemsVectorChanged;
		SetValue(ItemsProperty, items);

		Loaded += MenuBarItem_Loaded;
	}

	// Uno specific: re-resolves the parent MenuBar once loaded.
	private void MenuBarItem_Loaded(object sender, RoutedEventArgs e)
	{
		if (SharedHelpers.GetAncestorOfType<MenuBar>(VisualTreeHelper.GetParent(this)) is { } menuBar)
		{
			m_menuBar = new WeakReference<MenuBar>(menuBar);
		}
	}

	// Uno specific: WinUI propagates Enter/Leave to the Items through EnterSparseProperties. Items is not a
	// DependencyObject here, so the keyboard accelerators of the items are entered explicitly.
	internal override void EnterImpl(Uno.UI.Xaml.EnterParams @params, int depth)
	{
		base.EnterImpl(@params, depth);

		var items = Items;
		if (items is not null)
		{
			var visualTree = @params.VisualTree ?? Uno.UI.Xaml.Core.VisualTree.GetForElement(this, Uno.UI.Xaml.Core.VisualTree.LookupOptions.NoFallback);
			var kaParams = new Uno.UI.Xaml.EnterParams { IsForKeyboardAccelerator = true, IsLive = false, VisualTree = visualTree };
			foreach (var item in items)
			{
				item.Enter(kaParams, 0);
			}
		}
	}

	// Uno specific: see EnterImpl.
	internal override void LeaveImpl(Uno.UI.Xaml.LeaveParams @params)
	{
		base.LeaveImpl(@params);

		var items = Items;
		if (items is not null)
		{
			var visualTree = @params.VisualTree ?? Uno.UI.Xaml.Core.VisualTree.GetForElement(this, Uno.UI.Xaml.Core.VisualTree.LookupOptions.NoFallback);
			var kaParams = new Uno.UI.Xaml.LeaveParams { IsForKeyboardAccelerator = true, IsLive = false, VisualTree = visualTree };
			foreach (var item in items)
			{
				item.Leave(kaParams);
			}
		}
	}

	// IUIElement / IUIElementOverridesHelper
	protected override AutomationPeer OnCreateAutomationPeer() => new MenuBarItemAutomationPeer(this);

	// IFramework Override
	protected override void OnApplyTemplate()
	{
		m_button = GetTemplateChild("ContentButton") as Button;

		var menuBar = SharedHelpers.GetAncestorOfType<MenuBar>(VisualTreeHelper.GetParent(this));
		if (menuBar is not null)
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

		if (m_passThroughElement?.TryGetTarget(out var passThroughElement) == true)
		{
			flyout.OverlayInputPassThroughElement = passThroughElement;
		}
		m_flyout = flyout;

		if (m_button is { } button)
		{
			button.IsAccessKeyScope = true;
			button.ContextFlyout = flyout;
		}
	}

	private void AttachEventHandlers()
	{
		if (m_button is { } button)
		{
			m_pressedRevoker.Disposable = button.RegisterDisposablePropertyChangedCallback(ButtonBase.IsPressedProperty, OnVisualPropertyChanged);
			m_pointerOverRevoker.Disposable = button.RegisterDisposablePropertyChangedCallback(ButtonBase.IsPointerOverProperty, OnVisualPropertyChanged);
		}

		var pointerPressedHandler = new PointerEventHandler(OnMenuBarItemPointerPressed);
		AddHandler(PointerPressedEvent, pointerPressedHandler, true /*handledEventsToo*/);
		m_onMenuBarItemPointerPressedRevoker.Disposable = Disposable.Create(() => RemoveHandler(PointerPressedEvent, pointerPressedHandler));

		var keyDownHandler = new KeyEventHandler(OnMenuBarItemKeyDown);
		AddHandler(KeyDownEvent, keyDownHandler, true /*handledEventsToo*/);
		m_onMenuBarItemKeyDownRevoker.Disposable = Disposable.Create(() => RemoveHandler(KeyDownEvent, keyDownHandler));

		if (m_flyout is { } flyout)
		{
			flyout.Closed += OnFlyoutClosed;
			m_flyoutClosedRevoker.Disposable = Disposable.Create(() => flyout.Closed -= OnFlyoutClosed);
			flyout.Opening += OnFlyoutOpening;
			m_flyoutOpeningRevoker.Disposable = Disposable.Create(() => flyout.Opening -= OnFlyoutOpening);
		}

		PointerEntered += OnMenuBarItemPointerEntered;
		m_pointerEnteredRevoker.Disposable = Disposable.Create(() => PointerEntered -= OnMenuBarItemPointerEntered);

		AccessKeyInvoked += OnMenuBarItemAccessKeyInvoked;
		m_accessKeyInvokedRevoker.Disposable = Disposable.Create(() => AccessKeyInvoked -= OnMenuBarItemAccessKeyInvoked);
	}

	private void DetachEventHandlers()
	{
		m_pressedRevoker.Disposable = null;
		m_pointerOverRevoker.Disposable = null;

		m_flyoutClosedRevoker.Disposable = null;
		m_flyoutOpeningRevoker.Disposable = null;

		m_onMenuBarItemPointerPressedRevoker.Disposable = null;
		m_onMenuBarItemKeyDownRevoker.Disposable = null;
	}

	// Event Handlers
	private void OnMenuBarItemPointerEntered(object sender, PointerRoutedEventArgs args)
	{
		if (m_menuBar?.TryGetTarget(out var menuBar) == true)
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
		if (m_menuBar?.TryGetTarget(out var menuBar) == true)
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

		if (!isAltDown)
		{
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
	}

	private void OnPresenterKeyDown(object sender, KeyRoutedEventArgs args)
	{
		// If the event came from a MenuFlyoutSubItem it means right/left arrow will open it, so we should not handle them to not override default behaviour
		if (args.OriginalSource is MenuFlyoutSubItem subitem)
		{
			// Uno specific: WinUI's GetAt(0) on an empty sub-item fails the handler; the C# indexer would throw instead.
			if (subitem.Items.Count > 0 && subitem.Items[0] is not null)
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
			var index = (int)e.Index;
			switch (e.CollectionChange)
			{
				case CollectionChange.ItemInserted:
					flyout.Items.Insert(index, Items[index]);
					break;
				case CollectionChange.ItemRemoved:
					flyout.Items.RemoveAt(index);
					break;
				default:
					break;
			}
		}
	}

	private void OnMenuBarItemAccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
	{
		ShowMenuFlyout();
		args.Handled = true;
	}

	// Menu Flyout actions
	internal void ShowMenuFlyout()
	{
		if (Items.Count != 0)
		{
			if (m_button is { } button)
			{
				var width = (float)button.ActualWidth;
				var height = (float)button.ActualHeight;

				// Sets an exclusion rect over the button that generates the flyout so that even if the menu opens upwards
				// (which is the default in touch mode) it doesn't cover the menu bar button.
				FlyoutShowOptions options = new();
				options.Position = new Point(0, height);
				options.Placement = FlyoutPlacementMode.Bottom;
				options.ExclusionRect = new Rect(0, 0, width, height);
				m_flyout.ShowAt(button, options);

				// Attach keyboard event handler
				var presenter = m_flyout.m_presenter;
				presenter.KeyDown += OnPresenterKeyDown;
				m_presenterKeyDownRevoker.Disposable = Disposable.Create(() => presenter.KeyDown -= OnPresenterKeyDown);
			}
		}
	}

	internal void CloseMenuFlyout() => m_flyout.Hide();

	private void OpenFlyoutFrom(FlyoutLocation location)
	{
		if (m_menuBar?.TryGetTarget(out var menuBar) == true)
		{
			var index = menuBar.Items.IndexOf(this);
			CloseMenuFlyout();
			if (location == FlyoutLocation.Left)
			{
				FocusAndReturnNextFocusableItem(index, -1)?.ShowMenuFlyout();
			}
			else
			{
				FocusAndReturnNextFocusableItem(index, +1)?.ShowMenuFlyout();
			}
		}
	}

	private MenuBarItem FocusAndReturnNextFocusableItem(int index, int direction)
	{
		if (m_menuBar?.TryGetTarget(out var menuBar) == true)
		{
			int itemsCount = menuBar.Items.Count;
			// index + direction might be negative so account for that by adding itemsCount.
			int itemIndex = (index + direction + itemsCount) % itemsCount;
			while (!menuBar.Items[itemIndex].Focus(FocusState.Programmatic))
			{
				// index + direction might be negative so account for that by adding itemsCount.
				itemIndex = (itemIndex + direction + itemsCount) % itemsCount;
				if (itemIndex == index)
				{
					// We are where we started, return no new item.
					return null;
				}
			}
			return menuBar.Items[itemIndex];
		}
		return null;
	}

	private void MoveFocusTo(FlyoutLocation location)
	{
		if (m_menuBar?.TryGetTarget(out var menuBar) == true)
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

	internal void AddPassThroughElement(DependencyObject element) => m_passThroughElement = new WeakReference<DependencyObject>(element);

	// TODO Uno specific: public in Uno, implementation-only in WinUI.
	public bool IsFlyoutOpen() => m_isFlyoutOpen;

	// TODO Uno specific: public in Uno, implementation-only in WinUI.
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

		if (m_menuBar?.TryGetTarget(out var menuBar) == true)
		{
			menuBar.IsFlyoutOpen = false;
		}

		UpdateVisualStates();
	}

	private void OnFlyoutOpening(object sender, object args)
	{
		Focus(FocusState.Programmatic);

		m_isFlyoutOpen = true;

		if (m_menuBar?.TryGetTarget(out var menuBar) == true)
		{
			menuBar.IsFlyoutOpen = true;
		}

		UpdateVisualStates();
	}

	private void OnVisualPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) => UpdateVisualStates();

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
