using System;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Input.ContextMenu;

/// <summary>
/// Repro sample for TextCommandBarFlyout visual and behavioral bugs on Skia (kahua-private#436).
/// Issues covered:
/// 1. DropShadow incorrect size
/// 2. Scrollbar appearing in context menu
/// 3. DropShadow remains after submenu command execution
/// 4. Mouse-over submenu unintended behavior
/// 5. Clipboard commands intermittently missing (PrimaryCommands = 0)
/// 6. TextBox states affecting context menu commands
/// </summary>
[Sample("Input", Name = "TextBox ContextMenu Flyout", Description = "TextCommandBarFlyout visual/behavioral bugs repro (kahua-private#436)", IsManualTest = true)]
public sealed partial class TextBox_ContextMenu_Flyout : Page
{
	private int _eventCounter;

	public TextBox_ContextMenu_Flyout()
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

	private void OnPreSelectedTextBoxLoaded(object sender, RoutedEventArgs e)
	{
		PreSelectedTextBox.SelectAll();
	}

	private void OnInspectFlyoutClick(object sender, RoutedEventArgs e)
	{
		InspectTextBoxFlyout(MissingCommandsTextBox);
	}

	private void OnSelectAllAndInspectClick(object sender, RoutedEventArgs e)
	{
		MissingCommandsTextBox.Focus(FocusState.Programmatic);
		MissingCommandsTextBox.SelectAll();
		InspectTextBoxFlyout(MissingCommandsTextBox);
	}

	private void InspectTextBoxFlyout(TextBox textBox)
	{
		var flyout = textBox.ContextFlyout;
		if (flyout == null)
		{
			var result = "ContextFlyout: NULL (no flyout assigned)";
			FlyoutInspectResult.Text = result;
			LogEvent(result);
			return;
		}

		var sb = new StringBuilder();
		sb.AppendLine($"ContextFlyout Type: {flyout.GetType().FullName}");

		if (flyout is TextCommandBarFlyout tcbf)
		{
			sb.AppendLine($"PrimaryCommands: {tcbf.PrimaryCommands.Count}");
			sb.AppendLine($"SecondaryCommands: {tcbf.SecondaryCommands.Count}");

			if (tcbf.PrimaryCommands.Count == 0 && tcbf.SecondaryCommands.Count == 0)
			{
				sb.AppendLine("*** BUG: Both PrimaryCommands and SecondaryCommands are 0! ***");
			}
			else if (tcbf.PrimaryCommands.Count == 0)
			{
				sb.AppendLine("*** BUG: PrimaryCommands is 0 (Cut/Copy/Paste missing)! ***");
			}

			foreach (var cmd in tcbf.PrimaryCommands)
			{
				if (cmd is AppBarButton abb)
				{
					sb.AppendLine($"  Primary: '{abb.Label}' IsEnabled={abb.IsEnabled}");
				}
			}

			foreach (var cmd in tcbf.SecondaryCommands)
			{
				if (cmd is AppBarButton abb)
				{
					sb.AppendLine($"  Secondary: '{abb.Label}' IsEnabled={abb.IsEnabled}");
				}
				else if (cmd is AppBarSeparator)
				{
					sb.AppendLine("  Secondary: ---separator---");
				}
			}
		}
		else if (flyout is MenuFlyout mf)
		{
			sb.AppendLine($"MenuFlyout (WASM HTML path) Items: {mf.Items.Count}");
			foreach (var item in mf.Items)
			{
				if (item is MenuFlyoutItem mfi)
				{
					sb.AppendLine($"  Item: '{mfi.Text}'");
				}
				else if (item is MenuFlyoutSubItem mfsi)
				{
					sb.AppendLine($"  SubItem: '{mfsi.Text}' ({mfsi.Items.Count} children)");
				}
			}
		}
		else
		{
			sb.AppendLine($"Unknown flyout type");
		}

		var text = sb.ToString().TrimEnd();
		FlyoutInspectResult.Text = text;
		LogEvent(text.Replace("\n", " | ").Replace("\r", ""));
	}

	private void OnClearLogClick(object sender, RoutedEventArgs e)
	{
		_eventCounter = 0;
		EventLogText.Text = "Events will be logged here...";
		FlyoutInspectResult.Text = "Click 'Inspect Flyout' to see command counts...";
	}
}
