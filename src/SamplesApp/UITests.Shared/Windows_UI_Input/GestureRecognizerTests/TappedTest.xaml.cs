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

		private void OnLocationTestTargetTapped(object sender, TappedRoutedEventArgs e)
		{
			var relativeToRoot = e.GetPosition(LocationTestRoot).LogicalToPhysicalPixels();
			var relativeToTarget = e.GetPosition(LocationTestTarget).LogicalToPhysicalPixels();

			LocationTestRelativeToRootLocation.Text = $"({(int)relativeToRoot.X:D},{(int)relativeToRoot.Y:D})";
			LocationTestRelativeToTargetLocation.Text = $"({(int)relativeToTarget.X:D},{(int)relativeToTarget.Y:D})";
		}
	}
}
