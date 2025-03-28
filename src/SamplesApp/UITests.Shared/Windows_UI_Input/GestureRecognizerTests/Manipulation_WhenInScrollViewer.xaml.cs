using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;

#if HAS_UNO_WINUI || WINAPPSDK
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace UITests.Shared.Windows_UI_Input.GestureRecognizerTests
{
	[Sample("Gesture Recognizer", IgnoreInSnapshotTests = true)]
	public sealed partial class Manipulation_WhenInScrollViewer : Page
	{
		public Manipulation_WhenInScrollViewer()
		{
			this.InitializeComponent();
		}

		private void SetManipModeNone(object sender, RoutedEventArgs e)
			=> SetMode(ManipulationModes.None);
		private void SetTranslateXandY(object sender, RoutedEventArgs e)
			=> SetMode(ManipulationModes.TranslateX | ManipulationModes.TranslateY);
		private void SetTranslateXonly(object sender, RoutedEventArgs e)
			=> SetMode(ManipulationModes.TranslateX);
		private void SetTranslateYonly(object sender, RoutedEventArgs e)
			=> SetMode(ManipulationModes.TranslateY);

		private void SetMode(ManipulationModes mode)
		{
			Output.Text = "";
			TheScroller.ChangeView(horizontalOffset: 0, verticalOffset: 0, null);
			TouchTarget.ManipulationMode = mode;
		}

		private void OnManipStarting(object sender, ManipulationStartingRoutedEventArgs e)
			=> Write("[Starting]");

		private void OnManipStarted(object sender, ManipulationStartedRoutedEventArgs e)
			=> Write($"[Started] {F(e.Position, new ManipulationDelta { Scale = 1f }, e.Cumulative)}");

		private void OnManipDelta(object sender, ManipulationDeltaRoutedEventArgs e)
			=> Write($"[Delta] {F(e.Position, e.Delta, e.Cumulative)}");

		private void OnManipCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
			=> Write($"[Completed] {F(e.Position, new ManipulationDelta { Scale = 1f }, e.Cumulative)}");

		private void Write(string text)
		{
			Output.Text += "\r\n" + text;
		}

		private static string F(Point position, ManipulationDelta delta, ManipulationDelta cumulative)
			=> $"@=[{position.X:000.00},{position.Y:000.00}] "
				+ $"| X=(Σ:{cumulative.Translation.X:' '000.00;'-'000.00} / Δ:{delta.Translation.X:' '00.00;'-'00.00}) "
				+ $"| Y=(Σ:{cumulative.Translation.Y:' '000.00;'-'000.00} / Δ:{delta.Translation.Y:' '00.00;'-'00.00}) "
				+ $"| θ=(Σ:{cumulative.Rotation:' '000.00;'-'000.00} / Δ:{delta.Rotation:' '00.00;'-'00.00}) "
				+ $"| s=(Σ:{cumulative.Scale:000.00} / Δ:{delta.Scale:00.00}) "
				+ $"| e=(Σ:{cumulative.Expansion:' '000.00;'-'000.00} / Δ:{delta.Expansion:' '00.00;'-'00.00})";
	}
}
