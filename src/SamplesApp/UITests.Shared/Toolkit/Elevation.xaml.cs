using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

namespace UITests.Shared.Toolkit
{
	[SampleControlInfo("Toolkit", nameof(Elevation))]
	public sealed partial class Elevation : UserControl
	{
		public Elevation()
		{
			this.InitializeComponent();
		}

		private void TurnElevation_OFF(object sender, RoutedEventArgs args)
		{
			MyDialog_NoElevation.Visibility = Visibility.Visible;
			MyButton_NoElevation.Visibility = Visibility.Visible;
			MyComplexRadius_NoElevation.Visibility = Visibility.Visible;
			MyAppBar_NoElevation.Visibility = Visibility.Visible;
			MyCard_NoElevation.Visibility = Visibility.Visible;

			MyDialog_WithElevation.Visibility = Visibility.Collapsed;
			MyButton_WithElevation.Visibility = Visibility.Collapsed;
			MyComplexRadius_WithElevation.Visibility = Visibility.Collapsed;
			MyAppBar_WithElevation.Visibility = Visibility.Collapsed;
			MyCard_WithElevation.Visibility = Visibility.Collapsed;
		}

		private void TurnElevation_ON(object sender, RoutedEventArgs args)
		{
			MyDialog_NoElevation.Visibility = Visibility.Collapsed;
			MyButton_NoElevation.Visibility = Visibility.Collapsed;
			MyComplexRadius_NoElevation.Visibility = Visibility.Collapsed;
			MyAppBar_NoElevation.Visibility = Visibility.Collapsed;
			MyCard_NoElevation.Visibility = Visibility.Collapsed;

			MyDialog_WithElevation.Visibility = Visibility.Visible;
			MyButton_WithElevation.Visibility = Visibility.Visible;
			MyComplexRadius_WithElevation.Visibility = Visibility.Visible;
			MyAppBar_WithElevation.Visibility = Visibility.Visible;
			MyCard_WithElevation.Visibility = Visibility.Visible;
		}
	}
}
