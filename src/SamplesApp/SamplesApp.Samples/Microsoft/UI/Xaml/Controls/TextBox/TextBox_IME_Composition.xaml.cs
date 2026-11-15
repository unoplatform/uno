using System;
using System.Text;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.TextBoxControl
{
	[Sample("TextBox", Name = "TextBox_IME_Composition", Description = "IME composition — shows composition event flow on all Skia platforms")]
	public sealed partial class TextBox_IME_Composition : UserControl
	{
		private readonly StringBuilder _eventLog = new();
		private int _eventCount;

		public TextBox_IME_Composition()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			DetectBackend();

			InputTextBox.GotFocus += (_, _) => AddLog("TextBox GotFocus");
			InputTextBox.LostFocus += (_, _) => AddLog("TextBox LostFocus");
			InputTextBox.TextChanged += OnTextChanged;

#if __SKIA__
			// Use the standard TextBox TextComposition events (available on all Skia platforms)
			InputTextBox.TextCompositionStarted += (_, args) =>
			{
				StateText.Text = "Composing";
				AddLog($"TextCompositionStarted: startIndex={args.StartIndex}, length={args.Length}");
			};

			InputTextBox.TextCompositionChanged += (_, args) =>
			{
				LastPreeditText.Text = GetCompositionSubstring(args.StartIndex, args.Length);
				AddLog($"TextCompositionChanged: startIndex={args.StartIndex}, length={args.Length}, text='{LastPreeditText.Text}'");
			};

			InputTextBox.TextCompositionEnded += (_, args) =>
			{
				var committedText = GetCompositionSubstring(args.StartIndex, args.Length);
				LastCommitText.Text = committedText.Length > 0 ? committedText : "(empty)";
				StateText.Text = "Idle";
				LastPreeditText.Text = "(cleared)";
				AddLog($"TextCompositionEnded: startIndex={args.StartIndex}, length={args.Length}, committed='{committedText}'");
			};

			AddLog("TextComposition event hooks registered");
#else
			BackendText.Text = "N/A (not Skia)";
#endif
		}

		private string GetCompositionSubstring(int startIndex, int length)
		{
			var text = InputTextBox.Text;
			if (startIndex >= 0 && length > 0 && startIndex + length <= text.Length)
			{
				return text.Substring(startIndex, length);
			}
			return "(out of range)";
		}

		private void DetectBackend()
		{
			if (OperatingSystem.IsLinux())
			{
				var gtkImModule = Environment.GetEnvironmentVariable("GTK_IM_MODULE");
				var unoImModule = Environment.GetEnvironmentVariable("UNO_IM_MODULE");
				var xmodifiers = Environment.GetEnvironmentVariable("XMODIFIERS");

				var detected = unoImModule ?? gtkImModule ?? "(from XMODIFIERS)";
				BackendText.Text = $"X11 — {detected}";
				AddLog($"Env: UNO_IM_MODULE={unoImModule}, GTK_IM_MODULE={gtkImModule}, XMODIFIERS={xmodifiers}");
			}
			else if (OperatingSystem.IsAndroid())
			{
				BackendText.Text = "Android BaseInputConnection";
				AddLog("Platform: Android Skia — IME via TextInputConnection");
			}
			else if (OperatingSystem.IsWindows())
			{
				BackendText.Text = "Win32 IMM32/TSF";
				AddLog("Platform: Win32 — IME via IMM32");
			}
			else if (OperatingSystem.IsMacOS())
			{
				BackendText.Text = "macOS IME extension";
				AddLog("Platform: macOS Skia — IME via MacOSImeTextBoxExtension");
			}
			else
			{
				BackendText.Text = "Unknown platform";
			}
		}

		private void OnTextChanged(object sender, TextChangedEventArgs e)
		{
			LastKeyText.Text = InputTextBox.Text.Length > 0
				? InputTextBox.Text[^1].ToString()
				: "(empty)";
		}

		private void AddLog(string message)
		{
			_eventCount++;
			_eventLog.Insert(0, $"[{_eventCount:D3}] {message}\n");

			// Keep log reasonable
			if (_eventLog.Length > 2000)
			{
				_eventLog.Length = 2000;
			}

			EventLogText.Text = _eventLog.ToString();
		}
	}
}
