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

	private static string GetElementName(object element)
	{
		if (element == null)
		{
			return "(null)";
		}

		if (element is FrameworkElement fe && !string.IsNullOrEmpty(fe.Name))
		{
			return fe.Name;
		}

		return element.GetType().Name;
	}

	// Test 1: Basic ContextRequested (no ContextFlyout)
	private void OnBasicContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var hasPosition = args.TryGetPosition(sender, out var position);
		LogEvent($"[1] ContextRequested on BasicButton" +
				 $"\n    HasPosition: {hasPosition}" +
				 (hasPosition ? $", Position: ({position.X:F1}, {position.Y:F1})" : " (keyboard)"));
	}

	// Test 2: ContextFlyout blocks handler
	private void OnWithFlyoutContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		// This handler should NOT be called when ContextFlyout is set
		LogEvent("[2] ContextRequested on WithFlyoutButton - THIS SHOULD NOT APPEAR!");
		FlyoutBlockingStatus.Text = "ERROR: Handler was called even with ContextFlyout set!";
	}

	private void OnWithoutFlyoutContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		LogEvent("[2] ContextRequested on WithoutFlyoutButton - Handler called (expected)");
		FlyoutBlockingStatus.Text = "Handler called on button WITHOUT ContextFlyout (correct)";
	}

	// Test 3: Event Bubbling
	private void OnBubblingInnerContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var originalSource = GetElementName(args.OriginalSource);
		LogEvent($"[3] ContextRequested on BubblingInner (OriginalSource: {originalSource}, Handled: {args.Handled})");
	}

	private void OnBubblingOuterContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var originalSource = GetElementName(args.OriginalSource);
		LogEvent($"[3] ContextRequested BUBBLED to BubblingOuter (OriginalSource: {originalSource}, Handled: {args.Handled})");
	}

	// Test 4: Handled property
	private void OnHandledInnerContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var shouldHandle = HandleEventCheckbox.IsChecked == true;
		LogEvent($"[4] ContextRequested on HandledInner - Setting Handled={shouldHandle}");

		if (shouldHandle)
		{
			args.Handled = true;
		}
	}

	private void OnHandledOuterContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		LogEvent($"[4] ContextRequested BUBBLED to HandledOuter (Handled: {args.Handled})");
	}

	// Test 5: Position information
	private void OnPositionContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var hasRelative = args.TryGetPosition(sender, out var relativePos);
		var hasGlobal = args.TryGetPosition(null, out var globalPos);

		if (hasRelative)
		{
			PositionText.Text = $"Relative: ({relativePos.X:F1}, {relativePos.Y:F1}) | Global: ({globalPos.X:F1}, {globalPos.Y:F1})";
			LogEvent($"[5] Position - Relative: ({relativePos.X:F1}, {relativePos.Y:F1}), Global: ({globalPos.X:F1}, {globalPos.Y:F1})");
		}
		else
		{
			PositionText.Text = "Position: N/A (keyboard invocation)";
			LogEvent("[5] Position - N/A (keyboard invocation)");
		}
	}

	// Test 6: Keyboard invocation
	private void OnKeyboardContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var hasPosition = args.TryGetPosition(sender, out var position);

		if (hasPosition)
		{
			KeyboardText.Text = $"POINTER invocation at ({position.X:F1}, {position.Y:F1})";
			LogEvent($"[6] POINTER invocation at ({position.X:F1}, {position.Y:F1})");
		}
		else
		{
			KeyboardText.Text = "KEYBOARD invocation (TryGetPosition returned false)";
			LogEvent("[6] KEYBOARD invocation (TryGetPosition returned false)");
		}
	}

	// Test 7: ContextCanceled (no ContextFlyout)
	private void OnCancelTestContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		CancelStatusText.Text = "ContextRequested fired!";
		args.Handled = true;
		LogEvent("[7] ContextRequested fired");
	}

	private void OnCancelTestContextCanceled(UIElement sender, RoutedEventArgs args)
	{
		CancelStatusText.Text = "ContextCanceled fired!";
		LogEvent("[7] ContextCanceled fired - touch/pen moved away during hold");
	}

	// Test 8: ContextCanceled with ContextFlyout
	private void OnCancelWithFlyoutContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		CancelWithFlyoutStatusText.Text = "ContextRequested fired (flyout should show)";
		LogEvent("[8] ContextRequested fired (with ContextFlyout)");
	}

	private void OnCancelWithFlyoutContextCanceled(UIElement sender, RoutedEventArgs args)
	{
		CancelWithFlyoutStatusText.Text = "ContextCanceled fired!";
		LogEvent("[8] ContextCanceled fired (with ContextFlyout) - touch/pen moved away");
	}

	// Test 9: OriginalSource tracking
	private void OnSourceContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var originalSource = GetElementName(args.OriginalSource);
		var senderName = GetElementName(sender);
		LogEvent($"[9] ContextRequested - Sender: {senderName}, OriginalSource: {originalSource}");
	}

	// Test 10: SelectorItem.OnContextRequestedImpl - ListView ContextFlyout fallback
	private void OnListViewContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var originalSource = GetElementName(args.OriginalSource);
		LogEvent($"[10] ContextRequested on ListView (OriginalSource: {originalSource})");
		LogEvent("[10] Note: This handler fires, but flyout is shown by OnContextRequestedImpl in SelectorItem");
	}

	private void OnListViewMixedContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		var originalSource = GetElementName(args.OriginalSource);
		LogEvent($"[10-mixed] ContextRequested on ListView (OriginalSource: {originalSource})");
	}

	private void OnItemWithOwnFlyoutContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		// This handler should NOT be called because the item has its own ContextFlyout
		LogEvent("[10-mixed] ContextRequested on Item 2 - THIS SHOULD NOT APPEAR (item has ContextFlyout)!");
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

		// Reset status texts
		FlyoutBlockingStatus.Text = "Compare: Right-click both buttons and check the log";
		PositionText.Text = "Position: (waiting...)";
		KeyboardText.Text = "Invocation type: (waiting...)";
		CancelStatusText.Text = "Status: (waiting...)";
		CancelWithFlyoutStatusText.Text = "Status: (waiting...)";
	}
}
