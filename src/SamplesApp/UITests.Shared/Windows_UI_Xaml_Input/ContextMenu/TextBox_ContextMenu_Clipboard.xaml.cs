using System;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace UITests.Shared.Windows_UI_Xaml_Input.ContextMenu;

/// <summary>
/// Repro sample for clipboard access failures and keyboard shortcut issues (kahua-private#436).
/// Issues covered:
/// 1. Clipboard.GetContent() returns empty
/// 2. Clipboard round-trip failures
/// 3. Ctrl+V paste hooks not working (WASM)
/// 4. Shift+End not selecting text (WASM)
/// 5. Additional keyboard shortcut issues
/// </summary>
[Sample("Input", Name = "TextBox ContextMenu Clipboard", Description = "Clipboard and keyboard shortcut bugs repro (kahua-private#436)", IsManualTest = true)]
public sealed partial class TextBox_ContextMenu_Clipboard : Page
{
	private int _eventCounter;

	public TextBox_ContextMenu_Clipboard()
	{
		this.InitializeComponent();
	}

	private void LogEvent(string message)
	{
		_eventCounter++;
		var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
		var logEntry = $"[{_eventCounter}] {timestamp}: {message}\n";

		EventLogText.Text = logEntry + EventLogText.Text;

		if (EventLogText.Text.Length > 5000)
		{
			EventLogText.Text = EventLogText.Text.Substring(0, 4000);
		}
	}

	// Issue 1: Clipboard inspection
	private async void OnInspectClipboardClick(object sender, RoutedEventArgs e)
	{
		try
		{
			var dataPackageView = Clipboard.GetContent();
			var formats = dataPackageView.AvailableFormats;

			var sb = new StringBuilder();
			var formatList = string.Join(", ", formats);
			sb.AppendLine($"Available Formats: [{formatList}]");

			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				var text = await dataPackageView.GetTextAsync();
				var preview = text.Length > 100 ? text.Substring(0, 100) + "..." : text;
				sb.AppendLine($"Text: '{preview}'");
			}
			else
			{
				sb.AppendLine("Text: NOT PRESENT");
			}

			if (dataPackageView.Contains(StandardDataFormats.Html))
			{
				try
				{
					var html = await dataPackageView.GetHtmlFormatAsync();
					var preview = html.Length > 100 ? html.Substring(0, 100) + "..." : html;
					sb.AppendLine($"HTML: '{preview}'");
				}
				catch (Exception ex)
				{
					sb.AppendLine($"HTML: FAILED ({ex.Message})");
				}
			}

			var result = sb.ToString().TrimEnd();
			ClipboardInspectResult.Text = result;
			LogEvent("Clipboard: " + result.Replace("\n", " | ").Replace("\r", ""));
		}
		catch (Exception ex)
		{
			var error = $"Clipboard inspection FAILED: {ex.Message}";
			ClipboardInspectResult.Text = error;
			LogEvent(error);
		}
	}

	private void OnSetClipboardClick(object sender, RoutedEventArgs e)
	{
		var dataPackage = new DataPackage();
		dataPackage.RequestedOperation = DataPackageOperation.Copy;
		dataPackage.SetText("Test clipboard text set at " + DateTime.Now.ToString("HH:mm:ss"));
		Clipboard.SetContent(dataPackage);
		LogEvent("Clipboard text set programmatically");
	}

	private void OnClearClipboardClick(object sender, RoutedEventArgs e)
	{
		Clipboard.Clear();
		LogEvent("Clipboard cleared");
	}

	// Issue 2: Clipboard round-trip
	private void OnCopySourceClick(object sender, RoutedEventArgs e)
	{
		ClipboardSourceTextBox.Focus(FocusState.Programmatic);
		ClipboardSourceTextBox.SelectAll();

		var dataPackage = new DataPackage();
		dataPackage.RequestedOperation = DataPackageOperation.Copy;
		dataPackage.SetText(ClipboardSourceTextBox.Text);
		Clipboard.SetContent(dataPackage);
		LogEvent($"Copied source text: '{ClipboardSourceTextBox.Text}'");
	}

	private async void OnPasteToTargetClick(object sender, RoutedEventArgs e)
	{
		try
		{
			var dataPackageView = Clipboard.GetContent();
			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				var text = await dataPackageView.GetTextAsync();
				ClipboardTargetTextBox.Text = text;
				LogEvent($"Pasted to target: '{text}'");
			}
			else
			{
				LogEvent("Paste failed: no text on clipboard");
			}
		}
		catch (Exception ex)
		{
			LogEvent($"Paste failed: {ex.Message}");
		}
	}

	private void OnVerifyRoundTripClick(object sender, RoutedEventArgs e)
	{
		var source = ClipboardSourceTextBox.Text;
		var target = ClipboardTargetTextBox.Text;

		if (string.Equals(source, target, StringComparison.Ordinal))
		{
			RoundTripResult.Text = $"PASS: Source and target match ('{source}')";
			LogEvent("Round-trip: PASS");
		}
		else
		{
			RoundTripResult.Text = $"FAIL: Source='{source}' Target='{target}'";
			LogEvent($"Round-trip: FAIL — source='{source}' target='{target}'");
		}
	}

	// Issue 3: Ctrl+V paste hooks (WASM)
	private void OnPasteHookTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
	{
		var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
			.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

		if (ctrl)
		{
			string action = e.Key switch
			{
				Windows.System.VirtualKey.V => "Ctrl+V (Paste)",
				Windows.System.VirtualKey.C => "Ctrl+C (Copy)",
				Windows.System.VirtualKey.X => "Ctrl+X (Cut)",
				Windows.System.VirtualKey.A => "Ctrl+A (Select All)",
				Windows.System.VirtualKey.Z => "Ctrl+Z (Undo)",
				Windows.System.VirtualKey.Y => "Ctrl+Y (Redo)",
				_ => null
			};

			if (action != null)
			{
				PasteHookStatus.Text = $"KeyDown: {action} (Handled={e.Handled})";
				LogEvent($"KeyDown: {action} Handled={e.Handled}");
			}
		}
	}

	// Issue 4: Shift+End selection
	private void OnShiftEndSelectionChanged(object sender, RoutedEventArgs e)
	{
		var tb = (TextBox)sender;
		var selectedText = tb.SelectedText;
		var info = $"Selection: Start={tb.SelectionStart}, Length={tb.SelectionLength}";
		if (!string.IsNullOrEmpty(selectedText))
		{
			var preview = selectedText.Length > 50 ? selectedText.Substring(0, 50) + "..." : selectedText;
			info += $", Text='{preview}'";
		}
		ShiftEndStatus.Text = info;
	}

	private void OnShiftEndMultiLineSelectionChanged(object sender, RoutedEventArgs e)
	{
		var tb = (TextBox)sender;
		var selectedText = tb.SelectedText;
		var info = $"Selection: Start={tb.SelectionStart}, Length={tb.SelectionLength}";
		if (!string.IsNullOrEmpty(selectedText))
		{
			var preview = selectedText.Length > 50 ? selectedText.Substring(0, 50) + "..." : selectedText;
			info += $", Text='{preview}'";
		}
		ShiftEndMultiLineStatus.Text = info;
	}

	// Issue 5: Keyboard shortcuts
	private void OnKeyboardShortcutKeyDown(object sender, KeyRoutedEventArgs e)
	{
		var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
			.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
		var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
			.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

		string keyCombo = "";
		if (ctrl)
		{
			keyCombo += "Ctrl+";
		}
		if (shift)
		{
			keyCombo += "Shift+";
		}
		keyCombo += e.Key.ToString();

		// Only log interesting key combinations
		if (ctrl || shift || e.Key == Windows.System.VirtualKey.Home || e.Key == Windows.System.VirtualKey.End)
		{
			KeyboardShortcutStatus.Text = $"KeyDown: {keyCombo} (Handled={e.Handled})";
			LogEvent($"Keyboard: {keyCombo} Handled={e.Handled}");
		}
	}

	private void OnClearLogClick(object sender, RoutedEventArgs e)
	{
		_eventCounter = 0;
		EventLogText.Text = "Events will be logged here...";
		ClipboardInspectResult.Text = "Click 'Inspect Clipboard' to see clipboard state...";
		RoundTripResult.Text = "Copy, paste, then verify...";
		PasteHookStatus.Text = "Keyboard and paste events will be logged here...";
		ShiftEndStatus.Text = "Selection: (none)";
		ShiftEndMultiLineStatus.Text = "Selection: (none)";
		KeyboardShortcutStatus.Text = "Key events will be logged here...";
	}
}
