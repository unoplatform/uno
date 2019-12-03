using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Input.GestureRecognizerTests
{
	[SampleControlInfo("Gesture recognizer")]
	public sealed partial class TappedTest : Page
	{
		public TappedTest()
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
			=> WhenChildHandlesPointers_Result.Text = "Tapped";

		private void WhenMultipleTappedRecognizer_OnParentTapped(object sender, TappedRoutedEventArgs e)
			=> WhenMultipleTappedRecognizer_Result_Parent.Text = int.TryParse(WhenMultipleTappedRecognizer_Result_Parent.Text, out var count)
				? (count + 1).ToString()
				: "1";

		private void WhenMultipleTappedRecognizer_OnTargetTapped(object sender, TappedRoutedEventArgs e)
			=> WhenMultipleTappedRecognizer_Result_Target.Text = int.TryParse(WhenMultipleTappedRecognizer_Result_Target.Text, out var count)
				? (count + 1).ToString()
				: "1";
	}
}
