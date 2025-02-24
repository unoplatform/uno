using System;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Input.Keyboard
{
	[Sample("Keyboard")]
	public sealed partial class Keyboard_Events : Page
	{
		public Keyboard_Events()
		{
			this.InitializeComponent();

			SetupEvent(_root);
			SetupEvent(_btt1);
			SetupEvent(_btt2);
#if !HAS_UNO_WINUI
			SetupEvent(global::Windows.UI.Xaml.Window.Current.CoreWindow);
#endif
		}

		private void SetupEvent(FrameworkElement elt)
		{
			elt.KeyDown += (snd, e) =>
			{
				Console.WriteLine($"{elt.Name} - [KEYDOWN] {e.Key}");
				global::System.Diagnostics.Debug.WriteLine($"{elt.Name} - [KEYDOWN] {e.Key}");
				_output.Text += $"{elt.Name} - [KEYDOWN] {e.Key}\r\n";
			};
			elt.KeyUp += (snd, e) =>
			{
				Console.WriteLine($"{elt.Name} - [KEYUP] {e.Key}");
				global::System.Diagnostics.Debug.WriteLine($"{elt.Name} - [KEYUP] {e.Key}");
				_output.Text += $"{elt.Name} - [KEYUP] {e.Key}\r\n";
			};

			elt.PreviewKeyDown += (snd, e) =>
			{
				Console.WriteLine($"{elt.Name} - [PREVIEWKEYDOWN] {e.Key}");
				global::System.Diagnostics.Debug.WriteLine($"{elt.Name} - [PREVIEWKEYDOWN] {e.Key}");
				_output.Text += $"{elt.Name} - [PREVIEWKEYDOWN] {e.Key}\r\n";
			};
			elt.PreviewKeyUp += (snd, e) =>
			{
				Console.WriteLine($"{elt.Name} - [PREVIEWKEYUP] {e.Key}");
				global::System.Diagnostics.Debug.WriteLine($"{elt.Name} - [PREVIEWKEYUP] {e.Key}");
				_output.Text += $"{elt.Name} - [PREVIEWKEYUP] {e.Key}\r\n";
			};
		}

		private void SetupEvent(CoreWindow window)
		{
			window.KeyDown += (snd, e) =>
			{
				Console.WriteLine("CoreWindow - [KEYDOWN] {e.VirtualKey}");
				global::System.Diagnostics.Debug.WriteLine($"CoreWindow - [KEYDOWN] {e.VirtualKey}");
				_output.Text += $"CoreWindow - [KEYDOWN] {e.VirtualKey}\r\n";
			};
			window.KeyUp += (snd, e) =>
			{
				Console.WriteLine($"CoreWindow - [KEYUP] {e.VirtualKey}");
				global::System.Diagnostics.Debug.WriteLine($"CoreWindow - [KEYUP] {e.VirtualKey}");
				_output.Text += $"CoreWindow - [KEYUP] {e.VirtualKey}\r\n";
			};
		}
	}
}
