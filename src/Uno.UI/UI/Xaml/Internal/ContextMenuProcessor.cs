// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: ContextMenuProcessor.h, ContextMenuProcessor.cpp

#nullable enable

using System;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml.Internal;

/// <summary>
/// Handles raising ContextRequested and ContextCanceled events.
/// </summary>
/// <remarks>
/// Ported from WinUI ContextMenuProcessor.cpp.
/// </remarks>
internal partial class ContextMenuProcessor
{
	private const int ContextRequestOnHoldDelayMs = 500;

	// Fields (WinUI order from ContextMenuProcessor.h)
	private readonly ContentRoot _contentRoot;
	private bool _isContextMenuOnHolding;
	private DispatcherTimer? _contextMenuTimer;
	private WeakReference<UIElement>? _timerTargetElement;
	private Point _contextMenuOnHoldingTouchPoint = new(-1, -1);

	public ContextMenuProcessor(ContentRoot contentRoot)
	{
		_contentRoot = contentRoot ?? throw new ArgumentNullException(nameof(contentRoot));
	}

	/// <summary>
	/// Raise the ContextRequested event on the specified source element.
	/// </summary>
	/// <param name="source">The source element.</param>
	/// <param name="point">The global point, or (-1, -1) for keyboard invocation.</param>
	/// <param name="isTouchInput">Whether the input is from touch.</param>
	/// <remarks>
	/// Ported from WinUI ContextMenuProcessor.cpp:46-76
	/// </remarks>
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

			// WinUI behavior: Class handler runs FIRST as part of the event mechanism.
			// This shows the ContextFlyout before user handlers can set Handled=true.
			// See WinUI eventmgr.cpp:RaiseUIElementEvents which calls the Delegates array
			// handlers during event raising.
			if (uiElement is Control control)
			{
				// For Controls, invoke OnContextRequestedImpl which can be overridden
				control.InvokeOnContextRequestedImpl(args);
			}
			else
			{
				// For non-Controls, just try to show flyout on the element
				UIElement.OnContextRequestedCore(uiElement, uiElement, args);
			}

			// Then raise the user event for notification purposes.
			// This is a synchronous callout to application code that allows
			// the application to re-enter XAML. The application could
			// change state and release objects, so protect against
			// reentrancy by ensuring that objects are alive and state is
			// re-validated after return.
			uiElement.RaiseEvent(UIElement.ContextRequestedEvent, args);
		}

		if (args.Handled && isTouchInput)
		{
			_isContextMenuOnHolding = true;
		}
	}

	/// <summary>
	/// Process keyboard input for context menu triggers (Shift+F10, Application key, GamepadMenu).
	/// </summary>
	/// <param name="source">The source element.</param>
	/// <param name="virtualKey">The virtual key pressed.</param>
	/// <param name="modifierKeys">The modifier keys.</param>
	/// <remarks>
	/// Ported from WinUI ContextMenuProcessor.cpp:28-44
	/// </remarks>
	public void ProcessContextRequestOnKeyboardInput(
		DependencyObject source,
		VirtualKey virtualKey,
		VirtualKeyModifiers modifierKeys)
	{
		if ((virtualKey == VirtualKey.F10 && modifierKeys.HasFlag(VirtualKeyModifiers.Shift)) ||
			virtualKey == VirtualKey.Application ||
			virtualKey == VirtualKey.GamepadMenu)
		{
			RaiseContextRequestedEvent(source, new Point(-1, -1), isTouchInput: false);
		}
	}

	/// <summary>
	/// Handle holding gesture for touch input. If element is draggable/pannable,
	/// delay context menu by 500ms to allow pan gestures.
	/// </summary>
	/// <param name="element">The element that received the holding gesture.</param>
	/// <remarks>
	/// Ported from WinUI ContextMenuProcessor.cpp:78-113
	/// </remarks>
	public void ProcessContextRequestOnHoldingGesture(UIElement element)
	{
		bool isDraggableOrPannable = UIElement.IsDraggableOrPannable(element);

		if (isDraggableOrPannable)
		{
			// Create and start the contextmenu timer, and attach the timeout handler to fire ShowContextMenu
			StopContextMenuTimer();

			_contextMenuTimer = new DispatcherTimer();
			_contextMenuTimer.Interval = TimeSpan.FromMilliseconds(ContextRequestOnHoldDelayMs);
			_contextMenuTimer.Tick += OnContextRequestOnHoldingTimeout;
			// Store element reference for timer callback
			_timerTargetElement = new WeakReference<UIElement>(element);
			_contextMenuTimer.Start();
		}
		else
		{
			RaiseContextRequestedEvent(element, _contextMenuOnHoldingTouchPoint, isTouchInput: true);
		}
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
	/// Gets the context menu timer.
	/// </summary>
	public DispatcherTimer? GetContextMenuTimer() => _contextMenuTimer;

	/// <summary>
	/// Sets the touch point for context menu on holding gesture.
	/// </summary>
	public void SetContextMenuOnHoldingTouchPoint(Point point) => _contextMenuOnHoldingTouchPoint = point;

	/// <summary>
	/// Timer callback for delayed context menu on draggable elements.
	/// </summary>
	/// <remarks>
	/// Ported from WinUI ContextMenuProcessor.cpp:115-135
	/// </remarks>
	private void OnContextRequestOnHoldingTimeout(object? sender, object e)
	{
		StopContextMenuTimer();

		if (_timerTargetElement?.TryGetTarget(out var element) == true)
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
}
