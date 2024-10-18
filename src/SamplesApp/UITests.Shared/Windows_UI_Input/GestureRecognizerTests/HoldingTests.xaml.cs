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
	public sealed partial class HoldingTests : Page
	{
		public HoldingTests()
		{
			this.InitializeComponent();
		}

		private void TargetHeld(object sender, HoldingRoutedEventArgs e)
		{
			var target = (FrameworkElement)sender;
			var position = e.GetPosition(target).LogicalToPhysicalPixels();

			LastHeld.Text = $"{target.Name}-{e.HoldingState}@{position.X:F2},{position.Y:F2}";
		}

		private void ItemHeld(object sender, HoldingRoutedEventArgs e)
		{
			var target = (FrameworkElement)sender;
			var position = e.GetPosition(target).LogicalToPhysicalPixels();

			LastHeld.Text = $"Item_{target.DataContext}-{e.HoldingState}@{position.X:F2},{position.Y:F2}";
		}
	}
}
