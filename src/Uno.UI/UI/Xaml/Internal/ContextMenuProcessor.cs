// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: ContextMenuProcessor.h, ContextMenuProcessor.cpp

#nullable enable

using System;
using Windows.Foundation;
using Windows.System;
using Microsoft.UI.Xaml;
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
	private Point _contextMenuOnHoldingTouchPoint = new(-1, -1);

	public ContextMenuProcessor(ContentRoot contentRoot)
	{
		_contentRoot = contentRoot ?? throw new ArgumentNullException(nameof(contentRoot));
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
	/// Raise the ContextRequested event on the specified source element.
	/// </summary>
	/// <param name="source">The source element.</param>
	/// <param name="point">The global point, or (-1, -1) for keyboard invocation.</param>
	/// <param name="isTouchInput">Whether the input is from touch.</param>
	/// <remarks>
	/// Ported from WinUI ContextMenuProcessor.cpp:46-76.
	///
	/// In WinUI, the class handler (which shows ContextFlyout) is invoked at each element
	/// during event bubbling via the CControl::Delegates table mechanism. This is now
	/// handled by UIElement.InvokeClassHandler called from RaiseEvent, so we just need
	/// to raise the event here.
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

			// Raise the event - class handlers (OnContextRequestedImpl/OnContextRequestedCore)
			// will be invoked at each element during bubbling via UIElement.InvokeClassHandler.
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
			_contextMenuTimer = new DispatcherTimer();
			_contextMenuTimer.Interval = TimeSpan.FromMilliseconds(ContextRequestOnHoldDelayMs);
			_contextMenuTimer.Tick += OnContextRequestOnHoldingTimeout;
			_contextMenuTimer.TargetObject = element;
			_contextMenuTimer.Start();
		}
		else
		{
			RaiseContextRequestedEvent(element, _contextMenuOnHoldingTouchPoint, isTouchInput: true);
		}
	}

	/// <summary>
	/// Timer callback for delayed context menu on draggable elements.
	/// </summary>
	/// <remarks>
	/// Ported from WinUI ContextMenuProcessor.cpp:115-135
	/// </remarks>
	private static void OnContextRequestOnHoldingTimeout(object? sender, object e)
	{
		if (sender is DispatcherTimer timer)
		{
			timer.Stop();
			timer.Tick -= OnContextRequestOnHoldingTimeout;

			var source = timer.TargetObject;
			if (source != null)
			{
				var contextMenuProcessor = VisualTree.GetContentRootForElement(source)?.InputManager?.ContextMenuProcessor;
				contextMenuProcessor?.RaiseContextRequestedEvent(source, contextMenuProcessor._contextMenuOnHoldingTouchPoint, isTouchInput: true);
			}
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
}
