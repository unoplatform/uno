using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using Windows.System;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl;

[Sample("TextBox", Description = "Focus a TextBox and press Enter to test focus navigation", IsManualTest = true)]
public sealed partial class TextBox_FocusNavigation_Enter : UserControl
{
	public TextBox_FocusNavigation_Enter()
	{
		this.InitializeComponent();

		TextBox1.KeyDown += OnTextBoxKeyDown;
		TextBox2.KeyDown += OnTextBoxKeyDown;
		TextBox3.KeyDown += OnTextBoxKeyDown;
		TextBox4.KeyDown += OnTextBoxKeyDown;
		TextBox5.KeyDown += OnTextBoxKeyDown;

		TextBox1.GotFocus += OnTextBoxGotFocus;
		TextBox2.GotFocus += OnTextBoxGotFocus;
		TextBox3.GotFocus += OnTextBoxGotFocus;
		TextBox4.GotFocus += OnTextBoxGotFocus;
		TextBox5.GotFocus += OnTextBoxGotFocus;
	}

	private void OnTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key == VirtualKey.Enter)
		{
			var textBox = (TextBox)sender;
			Log($"KeyDown Enter received on {textBox.Name} (AcceptsReturn={textBox.AcceptsReturn})");

			if (!textBox.AcceptsReturn)
			{
				var options = new FindNextElementOptions()
				{
					SearchRoot = XamlRoot?.Content
				};

				Log($"Attempting to find next element (SearchRoot: {options.SearchRoot?.GetType().Name ?? "null"})");

				try
				{
					var nextElement = FocusManager.FindNextElement(FocusNavigationDirection.Next, options);
					if (nextElement is Control control)
					{
						Log($"Moving focus to {(nextElement as FrameworkElement)?.Name ?? nextElement.GetType().Name}");
						control.Focus(FocusState.Programmatic);
						e.Handled = true;
					}
					else
					{
						Log($"FindNextElement returned: {nextElement?.GetType().Name ?? "null"}");
					}
				}
				catch (Exception ex)
				{
					Log($"ERROR: {ex.Message}");
				}
			}
			else
			{
				Log("Enter should insert newline (AcceptsReturn=true)");
			}
		}
	}

	private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
	{
		var textBox = (TextBox)sender;
		StatusText.Text = $"Currently focused: {textBox.Name}";
		Log($"GotFocus: {textBox.Name}");
	}

	private void Log(string message)
	{
		LogText.Text = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n{LogText.Text}";
	}
}
