using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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
			LocationTestRelativeToRootLocation.Text = e.GetPosition(LocationTestRoot).ToString();
			LocationTestRelativeToTargetLocation.Text = e.GetPosition(LocationTestTarget).ToString();
		}
	}
}
