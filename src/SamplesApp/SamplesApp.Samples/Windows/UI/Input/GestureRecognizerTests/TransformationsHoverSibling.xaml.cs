using System;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Input.GestureRecognizerTests
{
	[Sample("Gesture Recognizer")]
	public sealed partial class TransformationsHoverSibling : Page
	{
		public TransformationsHoverSibling()
		{
			this.InitializeComponent();
		}

		private void OnMovedOnPink(object sender, PointerRoutedEventArgs e)
			=> PinkLocation.Text = F(e.GetCurrentPoint(PinkZone).Position);

		private void OnEnteredPink(object sender, PointerRoutedEventArgs e)
			=> PinkState.Text = "Hover";

		private void OnExitedPink(object sender, PointerRoutedEventArgs e)
			=> PinkState.Text = "Out";

		private void OnMovedOnBlue(object sender, PointerRoutedEventArgs e)
			=> BlueLocation.Text = F(e.GetCurrentPoint(BlueZone).Position);

		private void OnEnteredBlue(object sender, PointerRoutedEventArgs e)
			=> BlueState.Text = "Hover";

		private void OnExitedBlue(object sender, PointerRoutedEventArgs e)
			=> BlueState.Text = "Out";

		private static string F(Point position) => $"({position.X:F2},{position.Y:F2})";
	}
}
