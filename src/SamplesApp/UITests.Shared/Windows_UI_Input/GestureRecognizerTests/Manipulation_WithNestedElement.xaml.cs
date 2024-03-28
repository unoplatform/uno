using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Input.GestureRecognizerTests
{
	[Sample("Gesture Recognizer")]
	public sealed partial class Manipulation_WithNestedElement : Page
	{
		public Manipulation_WithNestedElement()
		{
			this.InitializeComponent();
		}

		private void OnParentManipStarted(object sender, ManipulationStartedRoutedEventArgs e)
			=> _result.Text += "[PARENT] Manip started\r\n";

		private void OnParentManipDelta(object sender, ManipulationDeltaRoutedEventArgs e)
			=> _result.Text += "[PARENT] Manip delta\r\n";

		private void OnParentManipCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
			=> _result.Text += "[PARENT] Manip completed\r\n";

		private void OnChildPointerPressed(object sender, PointerRoutedEventArgs e)
			=> _result.Text += "[CHILD] Pointer pressed\r\n";

		private void OnChildPointerMoved(object sender, PointerRoutedEventArgs e)
			=> _result.Text += "[CHILD] Pointer moved\r\n";

		private void OnChildPointerReleased(object sender, PointerRoutedEventArgs e)
			=> _result.Text += "[CHILD] Pointer released\r\n";
	}
}
