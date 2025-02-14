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
	public sealed partial class DoubleTappedTests : Page
	{
		public DoubleTappedTests()
		{
			this.InitializeComponent();
		}

		private void TargetDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			var target = (FrameworkElement)sender;
			var position = e.GetPosition(target).LogicalToPhysicalPixels();

			LastDoubleTapped.Text = FormattableString.Invariant($"{target.Name}@{position.X:F2},{position.Y:F2}");
		}

		private void ItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			var target = (FrameworkElement)sender;
			var position = e.GetPosition(target).LogicalToPhysicalPixels();

			LastDoubleTapped.Text = FormattableString.Invariant($"Item_{target.DataContext}@{position.X:F2},{position.Y:F2}");
		}
	}
}
