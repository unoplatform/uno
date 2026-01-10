using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Input.GestureRecognizerTests
{
	[Sample("Gesture Recognizer")]
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
