using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Input.ContextMenu;

/// <summary>
/// Sample for manually testing ContextRequested and ContextCanceled events.
/// </summary>
[SampleControlInfo("Input", "ContextRequested", description: "Manual testing sample for ContextRequested and ContextCanceled events")]
public sealed partial class ContextRequested_Sample : Page
{
	private int _eventCounter;

	public ContextRequested_Sample()
	{
		this.InitializeComponent();
	}

	private void LogEvent(string message)
	{
		_eventCounter++;
		var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
		var logEntry = $"[{_eventCounter}] {timestamp}: {message}\n";

		EventLogText.Text = logEntry + EventLogText.Text;

		// Limit log size
		if (EventLogText.Text.Length > 5000)
		{
			EventLogText.Text = EventLogText.Text.Substring(0, 4000);
		}
	}

	// Test 1 & General: Basic ContextRequested/ContextCanceled handlers
	private void OnContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var senderName = GetElementName(sender as FrameworkElement);
		var originalSourceName = GetElementName(args.OriginalSource as FrameworkElement);
		var hasPosition = args.TryGetPosition(sender, out var position);

		LogEvent($"ContextRequested on '{senderName}'" +
				 $"\n    OriginalSource: '{originalSourceName}'" +
				 $"\n    HasPosition: {hasPosition}" +
				 (hasPosition ? $", Position: ({position.X:F1}, {position.Y:F1})" : " (keyboard invocation)") +
				 $"\n    Handled: {args.Handled}");
	}

	private void OnContextCanceled(UIElement sender, RoutedEventArgs args)
	{
		var senderName = GetElementName(sender as FrameworkElement);
		LogEvent($"ContextCanceled on '{senderName}'");
	}

	// Test 2: Event Bubbling handlers
	private void OnInnerContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var originalSourceName = GetElementName(args.OriginalSource as FrameworkElement);
		LogEvent($"ContextRequested on INNER Border (OriginalSource: '{originalSourceName}', Handled: {args.Handled})");
	}

	private void OnOuterContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var originalSourceName = GetElementName(args.OriginalSource as FrameworkElement);
		LogEvent($"ContextRequested BUBBLED to OUTER Border (OriginalSource: '{originalSourceName}', Handled: {args.Handled})");
	}

	// Test 3: Handled property handlers
	private void OnHandledTestInnerContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var shouldHandle = HandleEventCheckbox.IsChecked == true;
		LogEvent($"ContextRequested on HandledTestInner - Setting Handled={shouldHandle}");

		if (shouldHandle)
		{
			args.Handled = true;
		}
	}

	private void OnHandledTestOuterContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		LogEvent($"ContextRequested BUBBLED to HandledTestOuter (Handled: {args.Handled})");
	}

	// Test 4: Position information handler
	private void OnPositionTestContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var hasPosition = args.TryGetPosition(sender, out var relativePosition);
		var hasGlobalPosition = args.TryGetPosition(null, out var globalPosition);

		if (hasPosition)
		{
			PositionText.Text = $"Position: ({relativePosition.X:F1}, {relativePosition.Y:F1}) relative to element";
			LogEvent($"ContextRequested - Relative: ({relativePosition.X:F1}, {relativePosition.Y:F1}), Global: ({globalPosition.X:F1}, {globalPosition.Y:F1})");
		}
		else
		{
			PositionText.Text = "Position: N/A (keyboard invocation)";
			LogEvent("ContextRequested - No position (keyboard invocation)");
		}
	}

	// Test 5: Keyboard invocation handler
	private void OnKeyboardTestContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var hasPosition = args.TryGetPosition(sender, out var position);

		if (hasPosition)
		{
			KeyboardInvocationText.Text = $"Invocation type: POINTER at ({position.X:F1}, {position.Y:F1})";
			LogEvent($"Keyboard test: POINTER invocation at ({position.X:F1}, {position.Y:F1})");
		}
		else
		{
			KeyboardInvocationText.Text = "Invocation type: KEYBOARD (Shift+F10 or Application key)";
			LogEvent("Keyboard test: KEYBOARD invocation (no position)");
		}
	}

	// Test 8: Prevent flyout handler
	private void OnPreventFlyoutContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var shouldPrevent = PreventFlyoutCheckbox.IsChecked == true;
		LogEvent($"ContextRequested on PreventFlyoutButton - Preventing flyout: {shouldPrevent}");

		if (shouldPrevent)
		{
			args.Handled = true;
			LogEvent("Flyout prevented by setting Handled=true");
		}
	}

	// Menu item click handler
	private void OnMenuItemClick(object sender, RoutedEventArgs e)
	{
		if (sender is MenuFlyoutItem item)
		{
			LogEvent($"Menu item clicked: '{item.Text}'");
		}
	}

	// Clear log button handler
	private void OnClearLogClick(object sender, RoutedEventArgs e)
	{
		_eventCounter = 0;
		EventLogText.Text = "Events will be logged here...";
	}

	private static string GetElementName(FrameworkElement element)
	{
		if (element == null)
		{
			return "(null)";
		}

		if (!string.IsNullOrEmpty(element.Name))
		{
			return element.Name;
		}

		return element.GetType().Name;
	}
}
