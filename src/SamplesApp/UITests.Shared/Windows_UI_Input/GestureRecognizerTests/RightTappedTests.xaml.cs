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
	public sealed partial class RightTappedTests : Page
	{
		public RightTappedTests()
		{
			this.InitializeComponent();
		}

		private void TargetRightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			var target = (FrameworkElement)sender;
			var position = e.GetPosition(target).LogicalToPhysicalPixels();

			LastRightTapped.Text = $"{target.Name}@{position.X:F2},{position.Y:F2}";
		}

		private void ItemRightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			var target = (FrameworkElement)sender;
			var position = e.GetPosition(target).LogicalToPhysicalPixels();

			LastRightTapped.Text = $"Item_{target.DataContext}@{position.X:F2},{position.Y:F2}";
		}
	}
}
