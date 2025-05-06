// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\CascadingMenuHelper.cpp, tag winui3/release/1.7.1, commit 5f27a786ac96c

using System;
using Uno.Disposables;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Core;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml.Controls;

internal partial class CascadingMenuHelper
{
	public CascadingMenuHelper()
	{
		m_isPointerOver = false;
		m_isPressed = false;
		m_subMenuShowDelay = -1;
	}

	~CascadingMenuHelper()
	{
		var delayOpenMenuTimer = m_delayOpenMenuTimer;
		if (delayOpenMenuTimer != null)
		{
			delayOpenMenuTimer.Stop();
		}

		var delayCloseMenuTimer = m_delayCloseMenuTimer;
		if (delayCloseMenuTimer != null)
		{
			delayCloseMenuTimer.Stop();
		}
	}

	internal void Initialize(FrameworkElement owner)
	{
		m_loadedHandler.Disposable = null;
		FrameworkElement ownerLocal = owner;
		m_wpOwner = WeakReferencePool.RentSelfWeakReference(ownerLocal);

		void OnLoadedClearFlags(object pSender, RoutedEventArgs args)
		{
			ClearStateFlags();
		}

		owner.Loaded += OnLoadedClearFlags;
		m_loadedHandler.Disposable = Disposable.Create(() => owner.Loaded -= OnLoadedClearFlags);

#if !HAS_UNO // Uno does not read from the registry
		// Try and read from Reg Key
		HKEY key = null;

		if (ERROR_SUCCESS == RegOpenKeyEx(HKEY_CURRENT_USER, "Control Panel\\Desktop\\", 0, KEY_QUERY_VALUE, &key))
		{
			char Buffer[32] = { 0 };
			DWORD BufferSize = sizeof(Buffer);
			DWORD dwOutType;

			if (RegQueryValueEx(key, "MenuShowDelay", NULL, &dwOutType/*REG_SZ*/, (LPBYTE)Buffer, &BufferSize) == ERROR_SUCCESS)
			{
				m_subMenuShowDelay = _wtoi(Buffer);
			}
			RegCloseKey(key);
		}
#endif

		// If the field wasn't successfully populated from the reg key
		// Or if the reg key contained a negative number
		// Then use a default value
		if (m_subMenuShowDelay < 0)
		{
			m_subMenuShowDelay = DefaultMenuShowDelay;
		}

		// Cascading menu owners should be access key scopes by default.
		ownerLocal.IsAccessKeyScope = true;
	}

	internal void OnApplyTemplate() => UpdateOwnerVisualState();

	// PointerEntered event handler that shows the sub menu
	// whenever the pointer is over the sub menu owner.
	// In case of touch, the sub menu item will be shown by
	// PointerReleased event.
	internal void OnPointerEntered(PointerRoutedEventArgs args)
	{
		bool handled = false;

		m_isPointerOver = true;

		handled = args.Handled;

		if (!handled)
		{
			var owner = m_wpOwner?.IsAlive == true ? m_wpOwner?.Target as ISubMenuOwner : null;

			if (owner is not null)
			{
				ISubMenuOwner parentOwner;
				parentOwner = owner.ParentOwner;

				if (parentOwner != null)
				{
					parentOwner.CancelCloseSubMenu();
				}
			}

			Pointer pointer = args.Pointer;
			var pointerDeviceType = pointer.PointerDeviceType;

			if (pointerDeviceType != PointerDeviceType.Touch)
			{
				CancelCloseSubMenu();

				EnsureDelayOpenMenuTimer();

				m_delayOpenMenuTimer.Start();

				UpdateOwnerVisualState();
			}

			args.Handled = true;
		}


	}

	// PointerExited event handler that ensures we close the sub menu
	// whenever the pointer leaves the current sub menu or
	// the main presenter. If the exited point is on the sub menu owner
	// or the sub menu, we want to keep the sub menu open.
	internal void OnPointerExited(
		PointerRoutedEventArgs args,
		bool parentIsSubMenu)
	{
		bool handled = false;

		m_isPointerOver = false;
		m_isPressed = false;

		handled = args.Handled;

		if (m_delayOpenMenuTimer != null)
		{
			m_delayOpenMenuTimer.Stop();
		}

		var ownerAsUIE = m_wpOwner?.IsAlive == true ? m_wpOwner?.Target as FrameworkElement : null;

		if (!handled && ownerAsUIE is not null && ownerAsUIE.IsInLiveTree)
		{
			var pointer = args.Pointer;
			var pointerDeviceType = pointer.PointerDeviceType;

			if (PointerDeviceType.Mouse == pointerDeviceType && !parentIsSubMenu)
			{
				UIElement subMenuPresenterAsUIE = m_wpSubMenuPresenter?.IsAlive == true ? m_wpSubMenuPresenter?.Target as UIElement : null;

				if (subMenuPresenterAsUIE is not null && subMenuPresenterAsUIE.IsInLiveTree)
				{
					bool isOwnerOrSubMenuHit = false;
					var pointerPoint = args.GetCurrentPoint(null);
					var point = pointerPoint.Position;

					var elements = VisualTreeHelper.FindElementsInHostCoordinates(point, ownerAsUIE, true /* includeAllElements */);

					foreach (var element in elements)
					{
						if (ownerAsUIE == element || subMenuPresenterAsUIE == element)
						{
							isOwnerOrSubMenuHit = true;
							break;
						}
					}

					if (!isOwnerOrSubMenuHit)
					{
						elements = VisualTreeHelper.FindElementsInHostCoordinates(point, subMenuPresenterAsUIE, true /* includeAllElements */);

						foreach (var element in elements)
						{
							if (ownerAsUIE == element || subMenuPresenterAsUIE == element)
							{
								isOwnerOrSubMenuHit = true;
								break;
							}
						}
					}

					// To close the sub menu, the pointer must be outside of the opened chain of sub-menus.
					if (!isOwnerOrSubMenuHit)
					{
						DelayCloseSubMenu();
						args.Handled = true;
					}
				}
			}

			UpdateOwnerVisualState();
		}
	}

	// PointerPressed event handler ensures that we're in the pressed state.
	internal void OnPointerPressed(PointerRoutedEventArgs args)
	{
		m_isPressed = true;

		args.Handled = true;
	}

	// PointerReleased event handler shows the sub menu in the
	// case of touch input.
	internal void OnPointerReleased(PointerRoutedEventArgs args)
	{
		Pointer pointer;
		PointerDeviceType pointerDeviceType;

		m_isPressed = false;

		pointer = args.Pointer;
		pointerDeviceType = pointer.PointerDeviceType;

		// Show the sub menu in the case of touch and pen input.
		// In case of the mouse device, the sub menu will be shown whenever the pointer is over the sub menu owner.
		// Note that sub menu is also shown OnPointerOver with pen device.
		if (PointerDeviceType.Touch == pointerDeviceType || PointerDeviceType.Pen == pointerDeviceType || PointerDeviceType.Mouse == pointerDeviceType)
		{
			OpenSubMenu();
		}

		args.Handled = true;
	}

	internal void OnGotFocus(RoutedEventArgs args)
	{
		UpdateOwnerVisualState();
	}

	internal void OnLostFocus(RoutedEventArgs args)
	{
		m_isPressed = false;
		UpdateOwnerVisualState();
	}

	// KeyDown event handler that handles the keyboard navigation between
	// the menu items and shows the sub menu in the case where we hit
	// the enter, space, or right arrow keys.
	internal void OnKeyDown(KeyRoutedEventArgs args)
	{
		bool handled = false;

		handled = args.Handled;

		if (!handled)
		{
			var key = args.Key;
			var keyStatus = args.KeyStatus;

			if (!keyStatus.IsMenuKeyDown)
			{
				// Show the sub menu with the enter, space, or right arrow keys
				if (key == VirtualKey.Enter ||
					key == VirtualKey.Right ||
					key == VirtualKey.Space)
				{
					OpenSubMenu();
					args.Handled = true;
				}
			}
		}
	}

	internal void OnKeyUp(KeyRoutedEventArgs args)
	{
		UpdateOwnerVisualState();
		args.Handled = true;
	}

	// Creates a DispatcherTimer for delaying showing the sub menu flyout
	private void EnsureDelayOpenMenuTimer()
	{
		if (m_delayOpenMenuTimer is null)
		{
			m_delayOpenMenuTimer = new DispatcherTimer();
			m_delayOpenMenuTimer.Tick += (s, e) => DelayOpenMenuTimerTickHandler();

			TimeSpan delayTimeSpan = TimeSpan.FromMilliseconds(m_subMenuShowDelay);
			m_delayOpenMenuTimer.Interval = delayTimeSpan;
		}
	}

	// Handler for the Tick event on the delay open menu timer.
	private void DelayOpenMenuTimerTickHandler()
	{
		EnsureCloseExistingSubItems();

		// Open the current sub menu
		OpenSubMenu();

		if (m_delayOpenMenuTimer is not null)
		{
			m_delayOpenMenuTimer.Stop();
		}
	}

	// Creates a DispatcherTimer for delaying hiding the sub menu flyout
	private void EnsureDelayCloseMenuTimer()
	{
		if (m_delayCloseMenuTimer is null)
		{
			m_delayCloseMenuTimer = new DispatcherTimer();
			m_delayCloseMenuTimer.Tick += (s, e) => DelayCloseMenuTimerTickHandler();

			TimeSpan delayTimeSpan = TimeSpan.FromMilliseconds(m_subMenuShowDelay);
			m_delayCloseMenuTimer.Interval = delayTimeSpan;
		}
	}

	// Handler for the Tick event on the delay close menu timer.
	private void DelayCloseMenuTimerTickHandler()
	{
		CloseSubMenu();

		if (m_delayCloseMenuTimer != null)
		{
			m_delayCloseMenuTimer.Stop();
			UpdateOwnerVisualState();
		}
	}

	// Ensure that any currently open sub menus are closed
	private void EnsureCloseExistingSubItems()
	{
		ISubMenuOwner ownerAsSubMenuOwner = m_wpOwner?.Target as ISubMenuOwner;

		if (ownerAsSubMenuOwner != null)
		{
			ownerAsSubMenuOwner.ClosePeerSubMenus();
		}
	}

	internal void SetSubMenuPresenter(FrameworkElement subMenuPresenter)
	{
		m_wpSubMenuPresenter = WeakReferencePool.RentSelfWeakReference(subMenuPresenter);

		var ownerAsSubMenuOwner = m_wpOwner?.IsAlive == true ? m_wpOwner.Target as ISubMenuOwner : null;

		if (ownerAsSubMenuOwner != null)
		{
			var menuPresenter = subMenuPresenter as IMenuPresenter;

			if (menuPresenter != null)
			{
				menuPresenter.Owner = ownerAsSubMenuOwner;
			}
		}
	}

	// Shows the sub menu at the appropriate position.
	// The sub menu will be adjusted if the sub presenter size changes.
	internal void OpenSubMenu()
	{
		var ownerAsSubMenuOwner = m_wpOwner?.IsAlive == true ? m_wpOwner?.Target as ISubMenuOwner : null;

		if (ownerAsSubMenuOwner is not null)
		{
			ownerAsSubMenuOwner.PrepareSubMenu();

			bool isSubMenuOpen = false;
			isSubMenuOpen = ownerAsSubMenuOwner.IsSubMenuOpen;

			if (!isSubMenuOpen)
			{
				var ownerAsControl = m_wpOwner?.IsAlive == true ? m_wpOwner?.Target as Control : null;

				if (ownerAsControl is not null)
				{
					EnsureCloseExistingSubItems();

					double subItemWidth = 0;
					subItemWidth = ownerAsControl.ActualWidth;

					FlowDirection flowDirection = FlowDirection.LeftToRight;
					flowDirection = ownerAsControl.FlowDirection;

					Point subMenuPosition = new Point(0, 0);

					bool isPositionedAbsolutely = false;
					isPositionedAbsolutely = ownerAsSubMenuOwner.IsSubMenuPositionedAbsolutely;

					if (isPositionedAbsolutely)
					{
						GeneralTransform transformToRoot;
						transformToRoot = ownerAsControl.TransformToVisual(null);
						subMenuPosition = transformToRoot.TransformPoint(subMenuPosition);
					}

					var ownerAsMenuFlyoutSubItem = ownerAsControl as MenuFlyoutSubItem;
					Popup popup = null;
					Control menuFlyoutPresenter = null;
					if (ownerAsMenuFlyoutSubItem is not null)
					{
						popup = ownerAsMenuFlyoutSubItem.GetPopup();
						menuFlyoutPresenter = ownerAsMenuFlyoutSubItem.GetMenuFlyoutPresenter();

						VisualTree visualTree = VisualTree.GetForElement(ownerAsMenuFlyoutSubItem);
						if (visualTree is not null)
						{
							// Put the popup on the same VisualTree as this flyout sub item to make sure it shows up in the right place
							popup.SetAssociatedVisualTree(visualTree);
						}
					}

					if (flowDirection == FlowDirection.RightToLeft && isPositionedAbsolutely)
					{
						subMenuPosition.X += (float)(m_subMenuOverlapPixels - subItemWidth);
					}
					else
					{
						subMenuPosition.X += (float)(subItemWidth - m_subMenuOverlapPixels);
					}

					if (popup is not null && menuFlyoutPresenter is not null)
					{
						double menuFlyoutPresenterWidth = menuFlyoutPresenter.ActualWidth;
						double menuFlyoutPresenterHeight = menuFlyoutPresenter.ActualHeight;

						// GetPositionAndDirection is called to identify the submenu's direction alone.
						GetPositionAndDirection(
							menuFlyoutPresenterWidth,
							menuFlyoutPresenterHeight,
							popup,
							out var subMenuPosition2,
							out var isSubMenuDirectionUp,
							out var positionAndDirectionSet);

						global::System.Diagnostics.Debug.Assert(subMenuPosition2.X == double.NegativeInfinity || subMenuPosition2.X == subMenuPosition.X);
						global::System.Diagnostics.Debug.Assert(subMenuPosition2.Y == double.NegativeInfinity || subMenuPosition2.Y == subMenuPosition.Y);

						if (positionAndDirectionSet)
						{
							ownerAsSubMenuOwner.SetSubMenuDirection(isSubMenuDirectionUp);
						}
					}

					ownerAsSubMenuOwner.OpenSubMenu(subMenuPosition);
#if HAS_UNO
					if (_lastTargetPoint is { } lastTargetPoint)
					{
						// Uno-specific workaround: reapply the location calculated in OnPresenterSizeChanged(), since that one properly
						// adjusts to keep submenu within screen bounds. (WinUI seemingly relies upon presenter.SizeChanged being raised
						// every time submenu opens? On Uno it isn't.)
						ownerAsSubMenuOwner.PositionSubMenu(lastTargetPoint);
					}
#endif
					ownerAsSubMenuOwner.RaiseAutomationPeerExpandCollapse(true /* isOpen */);
					ElementSoundPlayer.RequestInteractionSoundForElement(ElementSoundKind.Invoke, ownerAsControl);
				}
			}
		}
	}

	internal void CloseSubMenu()
	{
		CloseChildSubMenus();

		ISubMenuOwner owner = m_wpOwner?.IsAlive == true ? m_wpOwner.Target as ISubMenuOwner : null;

		if (owner is not null)
		{
			owner.CloseSubMenu();
			owner.RaiseAutomationPeerExpandCollapse(false /* isOpen */);

			var ownerAsDO = owner as DependencyObject;

			if (ownerAsDO != null)
			{
				ElementSoundPlayer.RequestInteractionSoundForElement(ElementSoundKind.Hide, ownerAsDO as DependencyObject);
			}
		}
	}

	internal void CloseChildSubMenus()
	{
		// WeakRefPtr fails an assert if we attempt to AsOrNull() to a type
		// that we aren't sure if the contents of the weak reference
		// implement that type.  To avoid that assert, we first As() the
		// weak reference contents to a known type that it's guaranteed
		// to be (if it isn't null), and we then AsOrNull() the ComPtr,
		// once it's safe to ask about a type that we're not sure of.
		var subMenuPresenterAsFE = m_wpSubMenuPresenter?.IsAlive == true ? m_wpSubMenuPresenter?.Target as FrameworkElement : null;

		IMenuPresenter subMenuPresenter = null;

		if (subMenuPresenterAsFE != null)
		{
			subMenuPresenter = subMenuPresenterAsFE as IMenuPresenter;
		}

		if (subMenuPresenter != null)
		{
			subMenuPresenter.CloseSubMenu();
		}
	}

	internal void DelayCloseSubMenu()
	{
		EnsureDelayCloseMenuTimer();
		if (m_delayCloseMenuTimer != null)
		{
			m_delayCloseMenuTimer.Start();
			UpdateOwnerVisualState();
		}
	}

	internal void CancelCloseSubMenu()
	{
		if (m_delayCloseMenuTimer != null)
		{
			m_delayCloseMenuTimer.Stop();
			UpdateOwnerVisualState();
		}
	}

	internal void ClearStateFlags()
	{
		m_isPressed = false;
		m_isPointerOver = false;
		UpdateOwnerVisualState();
	}

	internal void OnIsEnabledChanged(IsEnabledChangedEventArgs args)
	{
		var ownerAsControl = m_wpOwner?.IsAlive == true ? m_wpOwner.Target as Control : null;

		if (ownerAsControl != null)
		{
			bool bIsEnabled = false;
			bIsEnabled = ownerAsControl.IsEnabled;

			if (!bIsEnabled)
			{
				ClearStateFlags();
			}
			else
			{
				ownerAsControl.UpdateVisualState(true /* useTransitions */);
			}
		}
	}

	public void OnVisibilityChanged()
	{
		UIElement ownerAsUIE = m_wpOwner?.IsAlive == true ? m_wpOwner.Target as UIElement : null;

		if (ownerAsUIE != null)
		{
			Visibility visibility = ownerAsUIE.Visibility;

			if (Visibility.Visible != visibility)
			{
				ClearStateFlags();
			}
		}
	}

	internal void OnPresenterSizeChanged(
		object pSender,
		SizeChangedEventArgs args,
		Popup popup)
	{
		ISubMenuOwner ownerAsSubMenuOwner = m_wpOwner?.IsAlive == true ? m_wpOwner?.Target as ISubMenuOwner : null;

		if (ownerAsSubMenuOwner is not null)
		{
			Size newPresenterSize = args.NewSize;

			GetPositionAndDirection(
				newPresenterSize.Width,
				newPresenterSize.Height,
				popup,
				out var subMenuPosition,
				out var isSubMenuDirectionUp,
				out var positionAndDirectionSet);

			if (positionAndDirectionSet)
			{
				_lastTargetPoint = subMenuPosition;

				ownerAsSubMenuOwner.PositionSubMenu(subMenuPosition);
				ownerAsSubMenuOwner.SetSubMenuDirection(isSubMenuDirectionUp);
			}
		}
	}

	private void GetPositionAndDirection(
		double presenterWidth,
		double presenterHeight,
		Popup popup,
		out Point subMenuPosition,
		out bool isSubMenuDirectionUp,
		out bool positionAndDirectionSet)
	{
		// We sometimes will only want to change one of the two XY-positions of the menu,
		// but some menus (e.g. AppBarButton.Flyout) don't allow you to individually change
		// one axis of the position - you need to close and reopen the menu in a different location.
		// This necessitates a single function call that takes a Point parameter rather than
		// two function calls that individually change the X and then the Y position of the menu,
		// since otherwise we'd be closing and reopening the menu twice if we needed to change
		// both positions, which would be visually disruptive.
		// As such, we need a way to tell PositionSubMenu to leave one of the positions as it was.
		// We'll use negative infinity as a sentinel value that means "don't change this coordinate value".
		subMenuPosition = new Point(float.NegativeInfinity, float.NegativeInfinity);

		isSubMenuDirectionUp = false;
		positionAndDirectionSet = false;

		Control ownerAsControl = m_wpOwner?.IsAlive == true ? m_wpOwner?.Target as Control : null;

		ISubMenuOwner ownerAsSubMenuOwner = m_wpOwner?.IsAlive == true ? m_wpOwner?.Target as ISubMenuOwner : null;

		Control presenterAsControl = m_wpSubMenuPresenter?.IsAlive == true ? m_wpSubMenuPresenter?.Target as Control : null;

		bool isPositionedAbsolutely = false;
		isPositionedAbsolutely = ownerAsSubMenuOwner.IsSubMenuPositionedAbsolutely;

		Point ownerPosition = new Point(0, 0);

		if (isPositionedAbsolutely)
		{
			// Get the current sub menu item target position as the client point
			GeneralTransform transformToRoot = ownerAsControl.TransformToVisual(null);
			ownerPosition = transformToRoot.TransformPoint(ownerPosition);
		}

#if HAS_UNO
		// TODO: We are missing part of the logic here connected to windowed menus.
#endif

		// Get the available window rect
		Rect availableWindowRect = FlyoutBase.CalculateAvailableWindowRect(
			true /* isMenuFlyout */,
			popup,
			null /* placementTarget */,
			true /* hasTargetPosition */,
			ownerPosition,
			false /* isFull */);

		GetPositionAndDirection(
			presenterWidth,
			presenterHeight,
			availableWindowRect,
			ownerPosition,
			out subMenuPosition,
			out isSubMenuDirectionUp,
			out positionAndDirectionSet);
	}

	private void GetPositionAndDirection(
		double presenterWidth,
		double presenterHeight,
		Rect availableBounds,
		Point ownerPosition,
		out Point subMenuPosition,
		out bool isSubMenuDirectionUp,
		out bool positionAndDirectionSet)
	{
		subMenuPosition = new Point(float.NegativeInfinity, float.NegativeInfinity);
		isSubMenuDirectionUp = false;
		positionAndDirectionSet = false;

		Control ownerAsControl = m_wpOwner?.IsAlive == true ? m_wpOwner?.Target as Control : null;

		ISubMenuOwner ownerAsSubMenuOwner = m_wpOwner?.IsAlive == true ? m_wpOwner?.Target as ISubMenuOwner : null;

		Control presenterAsControl = m_wpSubMenuPresenter?.IsAlive == true ? m_wpSubMenuPresenter?.Target as Control : null;

		if (ownerAsControl is null || presenterAsControl is null)
		{
			return;
		}

		FlowDirection flowDirection = ownerAsControl.FlowDirection;

		// Get the current sub menu item width and height
		var ownerWidth = ownerAsControl.ActualWidth;
		var ownerHeight = ownerAsControl.ActualHeight;

		// Get the current presenter max width/height
		var maxWidth = presenterAsControl.MaxWidth;
		var maxHeight = presenterAsControl.MaxHeight;

		// Set the max width and height with the available windows bounds
		presenterAsControl.MaxWidth =
			double.IsNaN(maxWidth) ? availableBounds.Width : Math.Min(maxWidth, availableBounds.Width);
		presenterAsControl.MaxHeight =
			double.IsNaN(maxHeight) ? availableBounds.Height : Math.Min(maxHeight, availableBounds.Height);

		// Get the available bottom space to set the MenuFlyoutSubItem
		double bottomSpace = availableBounds.Y + availableBounds.Height - ownerPosition.Y;

		if (flowDirection == FlowDirection.LeftToRight)
		{
			// Get the available right space to set the MenuFlyoutSubItem
			double rightSpace = availableBounds.X + availableBounds.Width - ownerPosition.X - ownerWidth;
			// If the current sub presenter width isn't enough in the default right space,
			// the MenuFlyoutSubItem will be positioned on the left side if the current presenter
			// width is less than the sub item left(X) position. Otherwise, it will be aligned
			// right side of the available window rect.
			if (presenterWidth > rightSpace)
			{
				if (presenterWidth < availableBounds.Width - rightSpace - ownerWidth)
				{
					subMenuPosition.X = ownerPosition.X - presenterWidth + m_subMenuOverlapPixels;
				}
				else
				{
					subMenuPosition.X = ownerPosition.X + ownerWidth + rightSpace - presenterWidth;
				}
			}
		}
		else
		{
			// Get the available left space to set the MenuFlyoutSubItem
			double leftSpace = ownerPosition.X - availableBounds.X - ownerWidth;
			// If the current sub presenter width isn't enough in the default left space,
			// the MenuFlyoutSubItem will be positioned on the right side if the current presenter
			// width is less than the sub item right(X) position. Otherwise, it will be aligned
			// left side of the available window rect.
			if (presenterWidth > leftSpace)
			{
				if (presenterWidth < (availableBounds.Width - leftSpace - ownerWidth))
				{
					subMenuPosition.X = ownerPosition.X + presenterWidth - m_subMenuOverlapPixels;
				}
				else
				{
					subMenuPosition.X = ownerPosition.X - ownerWidth - leftSpace + presenterWidth;
				}
			}
			else
			{
				subMenuPosition.X = ownerPosition.X - ownerWidth + m_subMenuOverlapPixels;
			}
		}

		// If the current sub presenter doesn't have space to fit in the default bottom position,
		// then the MenuFlyoutSubItem will be aligned with the bottom of the target bounds.
		// If the MenuFlyoutSubItem is too tall to fit when bottom aligned with the target bounds
		// then it will be bottom aligned with the edge of the monitor.
		if (presenterHeight > bottomSpace)
		{
			// There is not enough bottom space to align top of owner with top of presenter.
			double topSpace = availableBounds.Height + ownerHeight - bottomSpace;

			if (topSpace >= presenterHeight)
			{
				// There is enough top space to align bottom of owner with bottom of presenter.
				subMenuPosition.Y = ownerPosition.Y + ownerHeight - presenterHeight;
				isSubMenuDirectionUp = true;
			}
			else
			{
				// presenterHeight > bottomSpace and presenterHeight > topSpace
				if (bottomSpace < topSpace)
				{
					// Aligning top of presenter with top of available bounds.
					subMenuPosition.Y = availableBounds.Y;
					isSubMenuDirectionUp = true;
				}
				else
				{
					// Aligning top of presenter with top of available bounds.
					subMenuPosition.Y = availableBounds.Y;
					isSubMenuDirectionUp = true;
				}
			}
		}
		else
		{
			// There is enough bottom space to align top of owner with top of presenter.
			subMenuPosition.Y = ownerPosition.Y;
		}

		positionAndDirectionSet = true;
	}

	private void UpdateOwnerVisualState()
	{
		Control ownerAsControl = m_wpOwner?.IsAlive == true ? m_wpOwner.Target as Control : null;

#if CMH_DEBUG
		(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: UpdateOwnerVisualState - ownerAsControl=0x%p.", this, ownerAsControl));
#endif // CMH_DEBUG

		if (ownerAsControl is not null)
		{
			ownerAsControl.UpdateVisualState(true /* useTransitions */);
		}
	}

	internal bool IsDelayCloseTimerRunning()
	{
		if (m_delayCloseMenuTimer is not null)
		{
			return m_delayCloseMenuTimer.IsEnabled;
		}

		return false;
	}

	// Uno-specific workaround (see comment below)
	private Point? _lastTargetPoint;
}
