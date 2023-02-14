using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
		}
	}
}
