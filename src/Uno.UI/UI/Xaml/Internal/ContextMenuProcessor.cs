// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ContextMenuProcessor.h, ContextMenuProcessor.cpp

#nullable enable

using System;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml.Internal;

/// <summary>
/// Handles raising ContextRequested and ContextCanceled events.
/// Ported from WinUI ContextMenuProcessor.cpp.
/// </summary>
internal partial class ContextMenuProcessor
{
	private const int ContextRequestOnHoldDelayMs = 500;

	private readonly ContentRoot _contentRoot;
	private bool _isContextMenuOnHolding;
	private Point _contextMenuOnHoldingTouchPoint;
	private DispatcherTimer? _contextMenuTimer;
	private WeakReference<UIElement>? _timerTargetElement;

	public ContextMenuProcessor(ContentRoot contentRoot)
	{
		_contentRoot = contentRoot ?? throw new ArgumentNullException(nameof(contentRoot));
	}

	/// <summary>
	/// Gets a value indicating whether a context menu is currently being shown via holding gesture.
	/// </summary>
	public bool IsContextMenuOnHolding => _isContextMenuOnHolding;

	/// <summary>
	/// Sets the context menu on holding state.
	/// </summary>
	public void SetIsContextMenuOnHolding(bool value) => _isContextMenuOnHolding = value;

	/// <summary>
	/// Sets the touch point for context menu on holding gesture.
	/// </summary>
	public void SetContextMenuOnHoldingTouchPoint(Point point) => _contextMenuOnHoldingTouchPoint = point;

	/// <summary>
	/// Process keyboard input for context menu triggers (Shift+F10, Application key, GamepadMenu).
	/// </summary>
	/// <param name="source">The source element.</param>
	/// <param name="virtualKey">The virtual key pressed.</param>
	/// <param name="modifierKeys">The modifier keys.</param>
	public void ProcessContextRequestOnKeyboardInput(
		DependencyObject source,
		VirtualKey virtualKey,
		VirtualKeyModifiers modifierKeys)
	{
		if ((virtualKey == VirtualKey.F10 && modifierKeys.HasFlag(VirtualKeyModifiers.Shift)) ||
			virtualKey == VirtualKey.Application ||
			virtualKey == VirtualKey.GamepadMenu)
		{
			// Default position is (-1, -1) indicating keyboard invocation
			var position = new Point(-1, -1);

			// For GamepadMenu, position at element center for better UX
			// This matches WinUI behavior where gamepad invocation shows
			// the context menu centered on the focused element
			if (virtualKey == VirtualKey.GamepadMenu && source is UIElement uiElement)
			{
				var size = uiElement.RenderSize;
				if (size.Width > 0 && size.Height > 0)
				{
					position = new Point(size.Width / 2, size.Height / 2);
				}
			}

			RaiseContextRequestedEvent(source, position, isTouchInput: false);
		}
	}

	/// <summary>
	/// Raise the ContextRequested event on the specified source element.
	/// </summary>
	/// <param name="source">The source element.</param>
	/// <param name="point">The global point, or (-1, -1) for keyboard invocation.</param>
	/// <param name="isTouchInput">Whether the input is from touch.</param>
	public void RaiseContextRequestedEvent(
		DependencyObject source,
		Point point,
		bool isTouchInput)
	{
		var args = new ContextRequestedEventArgs();
		args.SetGlobalPoint(point);

		if (source is UIElement uiElement)
		{
			args.OriginalSource = source;

			// This is a synchronous callout to application code that allows
			// the application to re-enter XAML. The application could
			// change state and release objects, so protect against
			// reentrancy by ensuring that objects are alive and state is
			// re-validated after return.
			uiElement.RaiseEvent(UIElement.ContextRequestedEvent, args);

			// If the event is not handled, show the ContextFlyout if available.
			// This matches WinUI's CUIElement::OnContextRequestedCore behavior.
			if (!args.Handled)
			{
				ShowContextFlyoutForElement(uiElement, args);
			}
		}

		if (args.Handled && isTouchInput)
		{
			_isContextMenuOnHolding = true;
		}
	}

	/// <summary>
	/// Shows the ContextFlyout for the specified element if one is set.
	/// If the element does not have a ContextFlyout, walks up the visual tree
	/// to find an ancestor with a ContextFlyout.
	/// Ported from WinUI CUIElement::OnContextRequestedCore.
	/// </summary>
	/// <param name="element">The element to show the flyout for.</param>
	/// <param name="args">The context requested event args.</param>
	private static void ShowContextFlyoutForElement(UIElement element, ContextRequestedEventArgs args)
	{
		// Find element with ContextFlyout (walk up tree if needed)
		// This matches WinUI behavior where a child element without ContextFlyout
		// can trigger its parent's ContextFlyout.
		UIElement contextFlyoutOwner = element;
		FlyoutBase? flyout = element.ContextFlyout;

		if (flyout == null)
		{
			// Walk up parent chain to find an ancestor with ContextFlyout
			DependencyObject? current = element;
			while (current != null && flyout == null)
			{
				current = (current as FrameworkElement)?.Parent
					?? Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);

				if (current is UIElement uiElement && uiElement.ContextFlyout != null)
				{
					flyout = uiElement.ContextFlyout;
					contextFlyoutOwner = uiElement;
				}
			}
		}

		if (flyout == null)
		{
			return;
		}

		var frameworkElement = contextFlyoutOwner as FrameworkElement;
		if (frameworkElement == null)
		{
			return;
		}

		if (args.TryGetPosition(element, out var point))
		{
			// Transform point from source element to contextFlyoutOwner coordinates if different
			if (element != contextFlyoutOwner)
			{
				var transform = element.TransformToVisual(contextFlyoutOwner);
				point = transform.TransformPoint(point);
			}

			// Show at specific position (pointer/touch invocation)
			var showOptions = new FlyoutShowOptions
			{
				Position = point,
				ShowMode = FlyoutShowMode.Standard
			};
			flyout.ShowAt(frameworkElement, showOptions);
		}
		else
		{
			// Keyboard invocation - no explicit position provided
#if __SKIA__
			// For TextBox on Skia, position the flyout at the text selection/caret
			if (element is TextBox textBox)
			{
				var textPosition = textBox.GetContextMenuShowPosition();
				if (textPosition.HasValue)
				{
					// Transform position to flyout owner coordinates if needed
					var position = textPosition.Value;
					if (element != contextFlyoutOwner)
					{
						var transform = element.TransformToVisual(contextFlyoutOwner);
						position = transform.TransformPoint(position);
					}

					var showOptions = new FlyoutShowOptions
					{
						Position = position,
						ShowMode = FlyoutShowMode.Standard
					};
					flyout.ShowAt(frameworkElement, showOptions);
					args.Handled = true;
					return;
				}
			}
#endif

			// Default keyboard invocation - show without specific position
			flyout.ShowAt(frameworkElement);
		}

		args.Handled = true;
	}

	/// <summary>
	/// Raises the ContextCanceled event on the specified element.
	/// Called when user drags after showing context menu via touch hold.
	/// </summary>
	/// <param name="element">The element to raise ContextCanceled on.</param>
	public void RaiseContextCanceledEvent(UIElement element)
	{
		if (element == null)
		{
			return;
		}

		var args = new RoutedEventArgs { OriginalSource = element };
		element.RaiseEvent(UIElement.ContextCanceledEvent, args);
		_isContextMenuOnHolding = false;
	}

	/// <summary>
	/// Process cancellation of context menu on touch drag after hold.
	/// When user drags after touch-and-hold that showed a context menu:
	/// - If in flyout layer (popup): close the topmost light-dismiss popup
	/// - If not in flyout layer: raise ContextCanceled event
	/// </summary>
	/// <param name="element">The element that received the drag.</param>
	public void ProcessContextCancelOnDrag(UIElement element)
	{
		if (_isContextMenuOnHolding && element != null)
		{
			// Stop any pending context menu timer
			StopContextMenuTimer();

			if (IsInFlyoutLayer(element))
			{
				// Element is in a popup/flyout - close the topmost light-dismiss popup
				// This matches WinUI behavior: dragging within a shown flyout dismisses it
				var popupRoot = VisualTree.GetPopupRootForElement(element);
				popupRoot?.CloseTopmostPopup(FocusState.Programmatic, PopupRoot.PopupFilter.LightDismissOnly);
			}
			else
			{
				// Element is NOT in a popup - raise ContextCanceled event
				// This matches WinUI behavior: dragging when app code handled ContextRequested
				// (without a flyout) triggers ContextCanceled
				RaiseContextCanceledEvent(element);
			}

			_isContextMenuOnHolding = false;
		}
	}

	/// <summary>
	/// Determines if the element is within a flyout/popup layer.
	/// Used to decide between closing a flyout vs raising ContextCanceled on drag.
	/// </summary>
	/// <param name="element">The element to check.</param>
	/// <returns>True if element is inside a popup; false otherwise.</returns>
	private static bool IsInFlyoutLayer(UIElement element)
	{
		if (element == null)
		{
			return false;
		}

		// GetRootOfPopupSubTree returns non-null if element is inside a popup
		return element.GetRootOfPopupSubTree() != null;
	}

	/// <summary>
	/// Handle holding gesture for touch input. If element is draggable/pannable,
	/// delay context menu by 500ms to allow pan gestures.
	/// </summary>
	/// <param name="element">The element that received the holding gesture.</param>
	public void ProcessContextRequestOnHoldingGesture(UIElement element)
	{
		bool isDraggableOrPannable = UIElement.IsDraggableOrPannable(element);

		if (isDraggableOrPannable)
		{
			// Create and start the contextmenu timer, and attach the timeout handler to fire ShowContextMenu
			StopContextMenuTimer();

			_contextMenuTimer = new DispatcherTimer();
			_contextMenuTimer.Interval = TimeSpan.FromMilliseconds(ContextRequestOnHoldDelayMs);
			_timerTargetElement = new WeakReference<UIElement>(element);
			_contextMenuTimer.Tick += OnContextRequestOnHoldingTimeout;
			_contextMenuTimer.Start();
		}
		else
		{
			RaiseContextRequestedEvent(element, _contextMenuOnHoldingTouchPoint, isTouchInput: true);
		}
	}

	/// <summary>
	/// Stops the context menu timer if running.
	/// </summary>
	public void StopContextMenuTimer()
	{
		if (_contextMenuTimer != null)
		{
			_contextMenuTimer.Stop();
			_contextMenuTimer.Tick -= OnContextRequestOnHoldingTimeout;
			_contextMenuTimer = null;
		}
		_timerTargetElement = null;
	}

	/// <summary>
	/// Timer callback for delayed context menu on draggable elements.
	/// </summary>
	private void OnContextRequestOnHoldingTimeout(object? sender, object e)
	{
		StopContextMenuTimer();

		if (_timerTargetElement?.TryGetTarget(out var element) == true)
		{
			RaiseContextRequestedEvent(element, _contextMenuOnHoldingTouchPoint, isTouchInput: true);
		}
	}
}
