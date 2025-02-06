using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Input.GestureRecognizerTests
{
	[SampleControlInfo("Gesture Recognizer")]
	public sealed partial class GestureEventsCommons : Page
	{
		public GestureEventsCommons()
		{
			this.InitializeComponent();
		}

		private void WhenTappedThenArgsLocationIsValid_OnTargetTapped(object sender, TappedRoutedEventArgs e)
		{
			var relativeToRoot = e.GetPosition(WhenTappedThenArgsLocationIsValid_Root).LogicalToPhysicalPixels();
			var relativeToTarget = e.GetPosition(WhenTappedThenArgsLocationIsValid_Target).LogicalToPhysicalPixels();

			WhenTappedThenArgsLocationIsValid_Result_RelativeToRoot.Text = $"({(int)relativeToRoot.X:D},{(int)relativeToRoot.Y:D})";
			WhenTappedThenArgsLocationIsValid_Result_RelativeToTarget.Text = $"({(int)relativeToTarget.X:D},{(int)relativeToTarget.Y:D})";
		}

		private void HandlePointerEvent(object sender, PointerRoutedEventArgs e)
			=> e.Handled = true;

		private void WhenChildHandlesPointers_OnParentTapped(object sender, TappedRoutedEventArgs e)
			=> WhenChildHandlesPointers_Result.Text = "Yes";

		private void WhenMultipleTappedRecognizer_OnParentTapped(object sender, TappedRoutedEventArgs e)
			=> WhenMultipleTappedRecognizer_Result_Parent.Text = int.TryParse(WhenMultipleTappedRecognizer_Result_Parent.Text, out var count)
				? (count + 1).ToString()
				: "1";

		private void WhenMultipleTappedRecognizer_OnTargetTapped(object sender, TappedRoutedEventArgs e)
			=> WhenMultipleTappedRecognizer_Result_Target.Text = int.TryParse(WhenMultipleTappedRecognizer_Result_Target.Text, out var count)
				? (count + 1).ToString()
				: "1";

		private void WhenParentCapturesPointer_OnParentPointerPressed(object sender, PointerRoutedEventArgs e)
			=> WhenParentCapturesPointer_Result_Captured.Text = ((UIElement)sender).CapturePointer(e.Pointer).ToString();

		private void WhenParentCapturesPointer_OnParentTapped(object sender, TappedRoutedEventArgs e)
			=> WhenParentCapturesPointer_Result_Parent.Text = "Yes";

		private void WhenParentCapturesPointer_OnTargetTapped(object sender, TappedRoutedEventArgs e)
			=> WhenParentCapturesPointer_Result_Target.Text = "Yes";
	}
}
