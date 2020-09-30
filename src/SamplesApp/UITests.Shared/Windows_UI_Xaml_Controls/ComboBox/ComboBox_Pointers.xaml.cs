using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample("ComboBox", "Pointers",
		IgnoreInSnapshotTests = true,
		Description = "The PointerPressed and PointerReleased have to be flagged as handled by the ComboBox")]
	public sealed partial class ComboBox_Pointers : Page
	{
		public ComboBox_Pointers()
		{
			this.InitializeComponent();
		}

		private void HookEvents(object sender, RoutedEventArgs e) => HookEvents(sender as FrameworkElement);
		private void HookEvents(FrameworkElement elt)
		{
			elt.AddHandler(PointerEnteredEvent, new PointerEventHandler((snd, e) => Log("[ENTERED]", e)), handledEventsToo: true);
			elt.AddHandler(PointerPressedEvent, new PointerEventHandler((snd, e) => Log("[PRESSED]", e)), handledEventsToo: true);
			elt.AddHandler(PointerMovedEvent, new PointerEventHandler((snd, e) => Log("[MOVED]", e)), handledEventsToo: true);
			elt.AddHandler(PointerReleasedEvent, new PointerEventHandler((snd, e) => Log("[RELEASED]", e)), handledEventsToo: true);
			elt.AddHandler(PointerExitedEvent, new PointerEventHandler((snd, e) => Log("[EXITED]", e)), handledEventsToo: true);
			elt.AddHandler(PointerCanceledEvent, new PointerEventHandler((snd, e) => Log("[CANCELED]", e)), handledEventsToo: true);
			elt.AddHandler(PointerCaptureLostEvent, new PointerEventHandler((snd, e) => Log("[CAPTURE_LOST]", e)), handledEventsToo: true);
		}

		private void Log(string evt, PointerRoutedEventArgs args)
		{
			var text = $"{evt} | handled={args.Handled}";

			_output.Text += "\r\n" + text;
			System.Diagnostics.Debug.WriteLine(text);
		}
	}
}
