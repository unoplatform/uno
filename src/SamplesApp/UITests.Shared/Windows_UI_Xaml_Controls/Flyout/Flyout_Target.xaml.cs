using Uno.UI.Samples.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Samples.Content.UITests.Flyout
{
	[SampleControlInfo("Flyouts", "Flyout_Target")]
	public sealed partial class Flyout_Target : UserControl
	{
		public Flyout_Target()
		{
			this.InitializeComponent();

		}

		private void OnClick(object s, RoutedEventArgs e)
		{
			result.Text = "";

			var flyout = new Windows.UI.Xaml.Controls.Flyout()
			{
				Content = new Border
				{
					Name = "innerContent",
					Height = 100,
					Width = 100,
					Background = new SolidColorBrush(Colors.Red)
				}
			};

			var target = s as FrameworkElement;
			flyout.ShowAt(target);

			var success = flyout.Target == target;

			result.Text = success ? "success" : "failed";
		}

		private void OnClickFull(object s, RoutedEventArgs e)
		{
			result.Text = "";
			var target = s as FrameworkElement;

			var flyout = new Windows.UI.Xaml.Controls.Flyout()
			{
				Content = new Border
				{
					Name = "innerContent",
					Height = 100,
					Width = 100,
					Background = new SolidColorBrush(Colors.Red)
				},
				Placement = Windows.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.Full
			};

			flyout.ShowAt(target);
		}
	}
}
