using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Disposables;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Windows.UI.Xaml.Controls
{
	internal class CascadingMenuHelper
	{

		// The overlapped menu pixels between the main main menu presenter and the sub presenter
		const int m_subMenuOverlapPixels = 4;

		int m_subMenuShowDelay;

		// Owner of the cascading menu
		WeakReference m_wpOwner;

		// Presenter of the sub-menu
		WeakReference m_wpSubMenuPresenter;

		// Event pointer for the Loaded event
		IDisposable m_loadedHandler;

		// Dispatcher timer to delay showing the sub menu flyout
		DispatcherTimer m_delayOpenMenuTimer;

		// Dispatcher timer to delay hiding the sub menu flyout
		DispatcherTimer m_delayCloseMenuTimer;

		// Indicate the pointer is over the cascading menu owner
		bool m_isPointerOver = true;

		// Indicate the pointer is pressed on the cascading menu owner
		bool m_isPressed = true;

		internal bool IsPressed => m_isPressed;

		internal bool IsPointerOver => m_isPointerOver;


		// This fallback is used if we fail to retrieve a value from the MenuShowDelay RegKey
		const int DefaultMenuShowDelay = 400; // in milliseconds

		// Uno-specific workaround (see comment below)
		private Point? _lastTargetPoint;

		public CascadingMenuHelper()
		{
			m_isPointerOver = false;
			m_isPressed = false;
			m_subMenuShowDelay = -1;

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: ", this));
#endif // CMH_DEBUG
		}

		~CascadingMenuHelper()
		{
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: ~", this));
#endif // CMH_DEBUG

			var delayOpenMenuTimer = m_delayOpenMenuTimer;
			if (delayOpenMenuTimer != null)
			{
#if CMH_DEBUG
				(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: ~CascadingMenuHelper - Stopping m_delayOpenMenuTimer.", this));
#endif // CMH_DEBUG

				delayOpenMenuTimer.Stop();
			}

			var delayCloseMenuTimer = m_delayCloseMenuTimer;
			if (delayCloseMenuTimer != null)
			{
#if CMH_DEBUG
				(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: ~CascadingMenuHelper - Stopping m_delayCloseMenuTimer.", this));
#endif // CMH_DEBUG
				delayCloseMenuTimer.Stop();
			}
		}

		internal void Initialize(FrameworkElement owner)
		{
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: Initialize.", this));
#endif // CMH_DEBUG

			FrameworkElement ownerLocal = owner;
			m_wpOwner = new WeakReference(ownerLocal);

			void OnLoadedClearFlags(object pSender, RoutedEventArgs args)
			{
				ClearStateFlags();
			}

			owner.Loaded += OnLoadedClearFlags;
			m_loadedHandler = Disposable.Create(() => owner.Loaded -= OnLoadedClearFlags);

#if false
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

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: Initialize - m_subMenuShowDelay=%d.", this, m_subMenuShowDelay));
#endif // CMH_DEBUG


			// Cascading menu owners should be access key scopes by default.
			ownerLocal.IsAccessKeyScope = true;
		}

		internal void OnApplyTemplate()
		{
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnApplyTemplate.", this));
#endif // CMH_DEBUG

			UpdateOwnerVisualState();

		}

		// PointerEntered event handler that shows the sub menu
		// whenever the pointer is over the sub menu owner.
		// In case of touch, the sub menu item will be shown by
		// PointerReleased event.
		internal void OnPointerEntered(PointerRoutedEventArgs args)
		{
			bool handled = false;

			m_isPointerOver = true;

			handled = args.Handled;

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnPointerEntered - handled=%d.", this, handled));
#endif // CMH_DEBUG

			if (!handled)
			{
				var owner = m_wpOwner?.Target as ISubMenuOwner;

#if CMH_DEBUG
				(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnPointerEntered - owner=0x%p.", this, owner));
#endif // CMH_DEBUG

				if (owner != null)
				{
					ISubMenuOwner parentOwner;
					parentOwner = owner.ParentOwner;

#if CMH_DEBUG
					(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnPointerEntered - parentOwner=0x%p.", this, parentOwner));
#endif // CMH_DEBUG

					if (parentOwner != null)
					{
						parentOwner.CancelCloseSubMenu();
					}
				}

				Pointer pointer = args.Pointer;
				var pointerDeviceType = pointer.PointerDeviceType;

#if CMH_DEBUG
				(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnPointerEntered - pointerDeviceType=%d.", this, pointerDeviceType));
#endif // CMH_DEBUG

				if (pointerDeviceType != PointerDeviceType.Touch)
				{
					CancelCloseSubMenu();

					EnsureDelayOpenMenuTimer();

#if CMH_DEBUG
					(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnPointerEntered - Starting m_delayOpenMenuTimer.", this));
#endif // CMH_DEBUG

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

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnPointerExited - handled=%d, parentIsSubMenu=%d.", this, handled, parentIsSubMenu));
#endif // CMH_DEBUG

			if (m_delayOpenMenuTimer != null)
			{
#if CMH_DEBUG
				(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnPointerExited - Stopping m_delayOpenMenuTimer.", this));
#endif // CMH_DEBUG

				m_delayOpenMenuTimer.Stop();
			}

			var ownerAsUIE = m_wpOwner?.Target as FrameworkElement;

			if (!handled && ownerAsUIE != null && ownerAsUIE.IsLoaded)
			{
				Pointer pointer;

				pointer = args.Pointer;
				var pointerDeviceType = pointer.PointerDeviceType;

#if CMH_DEBUG
				(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnPointerExited - pointerDeviceType=%d.", this, pointerDeviceType));
#endif // CMH_DEBUG

				if (PointerDeviceType.Mouse == pointerDeviceType && !parentIsSubMenu)
				{
					UIElement subMenuPresenterAsUIE = m_wpSubMenuPresenter?.Target as UIElement;

					if (subMenuPresenterAsUIE != null)
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
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnPointerPressed.", this));
#endif // CMH_DEBUG

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

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnPointerReleased - pointerDeviceType=%d.", this, pointerDeviceType));
#endif // CMH_DEBUG

			// Show the sub menu in the case of touch input.
			// In case of the mouse device or pen, the sub menu will be shown whenever the pointer is over
			// the sub menu owner.
			if (PointerDeviceType.Touch == pointerDeviceType)
			{
				OpenSubMenu();
			}

			args.Handled = true;
		}

		internal void OnGotFocus(RoutedEventArgs args)
		{
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnGotFocus.", this));
#endif // CMH_DEBUG

			UpdateOwnerVisualState();
		}

		internal void OnLostFocus(RoutedEventArgs args)
		{
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnLostFocus.", this));
#endif // CMH_DEBUG

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

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnKeyDown - handled=%d.", this, handled));
#endif // CMH_DEBUG

			if (!handled)
			{
				var key = args.Key;

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

		internal void OnKeyUp(KeyRoutedEventArgs args)
		{
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnKeyUp.", this));
#endif // CMH_DEBUG

			UpdateOwnerVisualState();
			args.Handled = true;
		}

		// Creates a DispatcherTimer for delaying showing the sub menu flyout
		void EnsureDelayOpenMenuTimer()
		{
			if (m_delayOpenMenuTimer == null)
			{
				m_delayOpenMenuTimer = new DispatcherTimer();
				m_delayOpenMenuTimer.Tick += (s, e) => DelayOpenMenuTimerTickHandler();

				TimeSpan delayTimeSpan = TimeSpan.FromMilliseconds(m_subMenuShowDelay);
				m_delayOpenMenuTimer.Interval = delayTimeSpan;
			}
		}

		// Handler for the Tick event on the delay open menu timer.
		void DelayOpenMenuTimerTickHandler()
		{
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: DelayOpenMenuTimerTickHandler.", this));
#endif // CMH_DEBUG

			EnsureCloseExistingSubItems();

			// Open the current sub menu
			OpenSubMenu();

			if (m_delayOpenMenuTimer != null)
			{
#if CMH_DEBUG
				(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: DelayOpenMenuTimerTickHandler - Stopping m_delayOpenMenuTimer.", this));
#endif // CMH_DEBUG

				m_delayOpenMenuTimer.Stop();
			}
		}

		// Creates a DispatcherTimer for delaying hiding the sub menu flyout
		void EnsureDelayCloseMenuTimer()
		{
			if (m_delayCloseMenuTimer == null)
			{
				m_delayCloseMenuTimer = new DispatcherTimer();
				m_delayCloseMenuTimer.Tick += (s, e) => DelayCloseMenuTimerTickHandler();

				TimeSpan delayTimeSpan = TimeSpan.FromMilliseconds(m_subMenuShowDelay);
				m_delayCloseMenuTimer.Interval = delayTimeSpan;
			}
		}

		// Handler for the Tick event on the delay close menu timer.
		void DelayCloseMenuTimerTickHandler()
		{
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: DelayCloseMenuTimerTickHandler.", this));
#endif // CMH_DEBUG

			CloseSubMenu();

			if (m_delayCloseMenuTimer != null)
			{
#if CMH_DEBUG
				(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: DelayCloseMenuTimerTickHandler - Stopping m_delayCloseMenuTimer.", this));
#endif // CMH_DEBUG
				m_delayCloseMenuTimer.Stop();
			}
		}

		// Ensure that any currently open sub menus are closed
		void EnsureCloseExistingSubItems()
		{
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: EnsureCloseExistingSubItems.", this));
#endif // CMH_DEBUG

			ISubMenuOwner ownerAsSubMenuOwner = m_wpOwner?.Target as ISubMenuOwner;

			if (ownerAsSubMenuOwner != null)
			{
				ownerAsSubMenuOwner.ClosePeerSubMenus();
			}
		}

		internal void SetSubMenuPresenter(FrameworkElement subMenuPresenter)
		{
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: SetSubMenuPresenter.", this));
#endif // CMH_DEBUG

			m_wpSubMenuPresenter = new WeakReference(subMenuPresenter);

			ISubMenuOwner ownerAsSubMenuOwner = m_wpOwner?.Target as ISubMenuOwner;

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: SetSubMenuPresenter - ownerAsSubMenuOwner=0x%p.", this, ownerAsSubMenuOwner));
#endif // CMH_DEBUG

			if (ownerAsSubMenuOwner != null)
			{
				IMenuPresenter menuPresenter = subMenuPresenter as IMenuPresenter;

#if CMH_DEBUG
				(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: SetSubMenuPresenter - menuPresenter=0x%p.", this, menuPresenter));
#endif // CMH_DEBUG

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
			ISubMenuOwner ownerAsSubMenuOwner = m_wpOwner?.Target as ISubMenuOwner;

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OpenSubMenu - ownerAsSubMenuOwner=0x%p.", this, ownerAsSubMenuOwner));
#endif // CMH_DEBUG

			if (ownerAsSubMenuOwner != null)
			{
				ownerAsSubMenuOwner.PrepareSubMenu();

				bool isSubMenuOpen = false;
				isSubMenuOpen = ownerAsSubMenuOwner.IsSubMenuOpen;

				if (!isSubMenuOpen)
				{
					Control ownerAsControl = m_wpOwner?.Target as Control;

					if (ownerAsControl != null)
					{
						EnsureCloseExistingSubItems();

						double subItemWidth = 0;
						subItemWidth = ownerAsControl.ActualWidth;

						FlowDirection flowDirection = FlowDirection.LeftToRight;
						flowDirection = ownerAsControl.FlowDirection;

						Point targetPoint = new Point(0, 0);

						bool isPositionedAbsolutely = false;
						isPositionedAbsolutely = ownerAsSubMenuOwner.IsSubMenuPositionedAbsolutely;

						if (isPositionedAbsolutely)
						{
							GeneralTransform transformToRoot;
							transformToRoot = ownerAsControl.TransformToVisual(null);
							targetPoint = transformToRoot.TransformPoint(targetPoint);
						}

						if (flowDirection == FlowDirection.RightToLeft)
						{
							targetPoint.X += (float)(m_subMenuOverlapPixels - subItemWidth);
						}
						else
						{
							targetPoint.X += (float)(subItemWidth - m_subMenuOverlapPixels);
						}

						ownerAsSubMenuOwner.OpenSubMenu(targetPoint);
						if (_lastTargetPoint is { } lastTargetPoint)
						{
							// Uno-specific workaround: reapply the location calculated in OnPresenterSizeChanged(), since that one properly
							// adjusts to keep submenu within screen bounds. (WinUI seemingly relies upon presenter.SizeChanged being raised
							// every time submenu opens? On Uno it isn't.)
							ownerAsSubMenuOwner.PositionSubMenu(lastTargetPoint);
						}
						ownerAsSubMenuOwner.RaiseAutomationPeerExpandCollapse(true /* isOpen */);
						ElementSoundPlayer.RequestInteractionSoundForElement(ElementSoundKind.Invoke, ownerAsControl);
					}
				}
			}
		}

		internal void CloseSubMenu()
		{
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: CloseSubMenu.", this));
#endif // CMH_DEBUG

			CloseChildSubMenus();

			ISubMenuOwner owner = m_wpOwner?.Target as ISubMenuOwner;

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: CloseSubMenu - owner=0x%p.", this, owner));
#endif // CMH_DEBUG

			if (owner != null)
			{
				owner.CloseSubMenu();
				owner.RaiseAutomationPeerExpandCollapse(false /* isOpen */);

				DependencyObject ownerAsDO = owner as DependencyObject;

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
			FrameworkElement subMenuPresenterAsFE = m_wpSubMenuPresenter?.Target as Frame;

			IMenuPresenter subMenuPresenter = null;

			if (subMenuPresenterAsFE != null)
			{
				subMenuPresenter = subMenuPresenterAsFE as IMenuPresenter;
			}

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: CloseChildSubMenus - subMenuPresenter=0x%p.", this, subMenuPresenter));
#endif // CMH_DEBUG

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
#if CMH_DEBUG
				(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: DelayCloseSubMenu - Starting m_delayCloseMenuTimer.", this));
#endif // CMH_DEBUG

				m_delayCloseMenuTimer.Start();
			}
		}

		internal void CancelCloseSubMenu()
		{
			if (m_delayCloseMenuTimer != null)
			{
#if CMH_DEBUG
				(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: CancelCloseSubMenu - Stopping m_delayCloseMenuTimer.", this));
#endif // CMH_DEBUG

				m_delayCloseMenuTimer.Stop();
			}
		}

		internal void ClearStateFlags()
		{
#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: ClearStateFlags.", this));
#endif // CMH_DEBUG

			m_isPressed = false;
			m_isPointerOver = false;
			UpdateOwnerVisualState();
		}

		internal void OnIsEnabledChanged()
		{
			Control ownerAsControl = m_wpOwner?.Target as Control;

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnIsEnabledChanged - ownerAsControl=0x%p.", this, ownerAsControl));
#endif // CMH_DEBUG

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
			UIElement ownerAsUIE = m_wpOwner?.Target as UIElement;

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnVisibilityChanged - ownerAsUIE=0x%p.", this, ownerAsUIE));
#endif // CMH_DEBUG

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
			Control ownerAsControl = m_wpOwner?.Target as Control;

			ISubMenuOwner ownerAsSubMenuOwner = m_wpOwner?.Target as ISubMenuOwner;

			Control presenterAsControl = m_wpSubMenuPresenter?.Target as Control;

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: OnPresenterSizeChanged - ownerAsControl=0x%p, ownerAsSubMenuOwner=0x%p.", this, ownerAsControl, ownerAsSubMenuOwner));
#endif // CMH_DEBUG

			if (ownerAsControl != null && ownerAsSubMenuOwner != null && presenterAsControl != null)
			{
				Size newPresenterSize = args.NewSize;

				FlowDirection flowDirection = ownerAsControl.FlowDirection;

				// We sometimes will only want to change one of the two XY-positions of the menu,
				// but some menus (e.g. AppBarButton.Flyout) don't allow you to individually change
				// one axis of the position - you need to close and reopen the menu in a different location.
				// This necessitates a single function call that takes a Point parameter rather than
				// two function calls that individually change the X and then the Y position of the menu,
				// since otherwise we'd be closing and reopening the menu twice if we needed to change
				// both positions, which would be visually disruptive.
				// As such, we need a way to tell PositionSubMenu to leave one of the positions as it was.
				// We'll use negative infinity as a sentinel value that means "don't change this coordinate value".
				Point targetPoint = new Point(float.NegativeInfinity, float.NegativeInfinity);

				bool isPositionedAbsolutely = false;
				isPositionedAbsolutely = ownerAsSubMenuOwner.IsSubMenuPositionedAbsolutely;

				Point positionPoint = new Point(0, 0);

				if (isPositionedAbsolutely)
				{
					// Get the current sub menu item target position as the client point
					GeneralTransform transformToRoot;
					transformToRoot = ownerAsControl.TransformToVisual(null);
					positionPoint = transformToRoot.TransformPoint(positionPoint);
				}

				// Get the current sub menu item width and height
				double width = 0;
				double height = 0;
				width = ownerAsControl.ActualWidth;
				height = ownerAsControl.ActualHeight;

				// Get the current presenter max width/height
				var maxWidth = presenterAsControl.MaxWidth;
				var maxHeight = presenterAsControl.MaxHeight;

#if false // UNO TODO Windowed menus are not supported
				// If the current menu is a windowed popup, the position setting will be within the current nearest
				// monitor boundary. Otherwise, the position will ensure within the Xaml window boundary.
				if ((CPopup*)(popup.GetHandle()).IsWindowed())
				{
					Rect targetBounds = {
						positionPoint.X,
						positionPoint.Y,
						(FLOAT)(width),
						(FLOAT)(height)};

					// Get the available monitor bounds from the current nearest monitor.
					// IHM bounds will be excluded from the monitor bounds.
					Rect availableMonitorRect = default;
					(DXamlCore.GetCurrent().CalculateAvailableMonitorRect(popup, positionPoint, &availableMonitorRect));

					// Set the max width and height with the available monitor bounds
					(presenterAsControl.put_MaxWidth(
						DoubleUtil.IsNaN(maxWidth) ? availableMonitorRect.Width : DoubleUtil.Min(maxWidth, availableMonitorRect.Width)));
					(presenterAsControl.put_MaxHeight(
						DoubleUtil.IsNaN(maxHeight) ? availableMonitorRect.Height : DoubleUtil.Min(maxHeight, availableMonitorRect.Height)));

					// Get the available bottom space from the current nearest monitor bounds
					double bottomSpace = (availableMonitorRect.Y + availableMonitorRect.Height) - (targetBounds.Y);

					if (flowDirection == FlowDirection_LeftToRight)
					{
						// Get the available right space from the current nearest monitor bounds
						double rightSpace = (availableMonitorRect.X + availableMonitorRect.Width) - (targetBounds.X + targetBounds.Width);
						// If the current sub presenter width isn't enough in the default right space,
						// the MenuFlyoutSubItem will be positioned on the left side if the current presenter
						// width is less than the sub item left(X) position. Otherwise, it will be aligned to
						// the right side of the available monitor rect.
						if (newPresenterSize.Width > rightSpace)
						{
							if (newPresenterSize.Width < availableMonitorRect.Width - rightSpace - targetBounds.Width)
							{
								targetPoint.X = (float)(positionPoint.X - newPresenterSize.Width + m_subMenuOverlapPixels);
							}
							else
							{
								targetPoint.X = (float)(positionPoint.X + width + rightSpace - newPresenterSize.Width);
							}
						}
					}
					else
					{
						// Get the available left space from the current nearest monitor bounds
						double leftSpace = targetBounds.X - availableMonitorRect.X - targetBounds.Width;
						// If the current sub presenter width isn't enough in the default left space,
						// the MenuFlyoutSubItem will be positioned on the right side if the current presenter
						// width is less than the sub item right(X) position. Otherwise, it will be aligned to
						// the left side of the available monitor rect.
						if (newPresenterSize.Width > leftSpace)
						{
							if (newPresenterSize.Width < (availableMonitorRect.Width - leftSpace - targetBounds.Width))
							{
								targetPoint.X = (float)(positionPoint.X + newPresenterSize.Width - m_subMenuOverlapPixels);
							}
							else
							{
								targetPoint.X = (float)(positionPoint.X - width - leftSpace + newPresenterSize.Width);
							}
						}
						else
						{
							targetPoint.X = (float)(positionPoint.X - targetBounds.Width + m_subMenuOverlapPixels);
						}
					}

					// If the current sub presenter doesn't have space to fit in the default bottom position,
					// then the MenuFlyoutSubItem will be aligned with the bottom of the target bounds.
					// If the MenuFlyoutSubItem is too tall to fit when bottom aligned with the target bounds
					// then it will be bottom aligned with the edge of the monitor.
					if (newPresenterSize.Height > bottomSpace)
					{
						if ((targetBounds.Y + targetBounds.Height) > newPresenterSize.Height)
						{
							targetPoint.Y = (float)(positionPoint.Y - newPresenterSize.Height + targetBounds.Height);
						}
						else
						{
							targetPoint.Y = (float)(positionPoint.Y - DoubleUtil.Min(newPresenterSize.Height - bottomSpace, availableMonitorRect.Height - bottomSpace));
						}
					}
				}
				else
#endif // UNO TODO Windowed menus are not supported
				{
					// Get the available window rect
					Rect availableWindowRect = FlyoutBase.CalculateAvailableWindowRect(
						true /* isMenuFlyout */,
						popup,
						null /* placementTarget */,
						true /* hasTargetPosition */,
						positionPoint,
						false /* isFull */);

					// Set the max width and height with the available windows bounds
					presenterAsControl.MaxWidth =
						double.IsNaN(maxWidth) ? availableWindowRect.Width : Math.Min(maxWidth, availableWindowRect.Width);
					presenterAsControl.MaxHeight =
						double.IsNaN(maxHeight) ? availableWindowRect.Height : Math.Min(maxHeight, availableWindowRect.Height);

					// Get the available bottom space to set the MenuFlyoutSubItem
					double bottomSpace = availableWindowRect.Height - positionPoint.Y;

					if (flowDirection == FlowDirection.LeftToRight)
					{
						// Get the available right space to set the MenuFlyoutSubItem
						double rightSpace = availableWindowRect.Width - (positionPoint.X + width);
						// If the current sub presenter width isn't enough in the default right space,
						// the MenuFlyoutSubItem will be positioned on the left side if the current presenter
						// width is less than the sub item left(X) position. Otherwise, it will be aligned
						// right side of the available window rect.
						if (newPresenterSize.Width > rightSpace)
						{
							if (newPresenterSize.Width < positionPoint.X)
							{
								targetPoint.X = (float)(positionPoint.X - newPresenterSize.Width + m_subMenuOverlapPixels);
							}
							else
							{
								targetPoint.X = (float)(availableWindowRect.Width - newPresenterSize.Width);
							}
						}
					}
					else
					{
						// Get the available left space to set the MenuFlyoutSubItem
						double leftSpace = positionPoint.X - availableWindowRect.X - width;
						// If the current sub presenter width isn't enough in the default left space,
						// the MenuFlyoutSubItem will be positioned on the right side if the current presenter
						// width is less than the sub item right(X) position. Otherwise, it will be aligned
						// left side of the available window rect.
						if (newPresenterSize.Width > leftSpace)
						{
							if (newPresenterSize.Width < (availableWindowRect.Width + availableWindowRect.X - positionPoint.X))
							{
								targetPoint.X = (float)(positionPoint.X + width - m_subMenuOverlapPixels);
							}
							else
							{
								targetPoint.X = (float)width;
							}
						}
						else
						{
							targetPoint.X = (float)(positionPoint.X - width + m_subMenuOverlapPixels);
						}
					}

					// If the current sub presenter doesn't have space to fit in the default bottom position,
					// then the MenuFlyoutSubItem will be aligned with the bottom of the target bounds.
					// If the MenuFlyoutSubItem is too tall to fit when bottom aligned with the target bounds
					// then it will be bottom aligned with the edge of the monitor.
					if (newPresenterSize.Height > bottomSpace)
					{
						if ((positionPoint.Y + height) > newPresenterSize.Height)
						{
							targetPoint.Y = (float)(positionPoint.Y + height - newPresenterSize.Height);
						}
						else
						{
							targetPoint.Y = (float)(positionPoint.Y - Math.Min(newPresenterSize.Height - bottomSpace, availableWindowRect.Height - bottomSpace));
						}
					}
				}

				_lastTargetPoint = targetPoint;
				ownerAsSubMenuOwner.PositionSubMenu(targetPoint);
			}
		}

		void UpdateOwnerVisualState()
		{
			Control ownerAsControl = m_wpOwner?.Target as Control;

#if CMH_DEBUG
			(DebugTrace(XCP_TRACE_OUTPUT_MSG /*traceType*/, "CMH[0x%p]: UpdateOwnerVisualState - ownerAsControl=0x%p.", this, ownerAsControl));
#endif // CMH_DEBUG

			if (ownerAsControl != null)
			{
				ownerAsControl.UpdateVisualState(true /* useTransitions */);
			}
		}
	}
}
