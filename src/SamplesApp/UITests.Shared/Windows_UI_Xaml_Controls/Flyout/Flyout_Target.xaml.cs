using Uno.UI.Samples.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Samples.Content.UITests.Flyout
{
	[SampleControlInfo("Flyout", "Flyout_Target")]
	public sealed partial class Flyout_Target : UserControl
	{
		private Windows.UI.Xaml.Controls.Flyout _flyout;

		public Flyout_Target()
		{
			this.InitializeComponent();

			_flyout = new Windows.UI.Xaml.Controls.Flyout()
			{
				Content = new Border
				{
					Height = 100,
					Width = 100,
					Background = new SolidColorBrush(Colors.Red)
				}
			};
		}

		private void OnClick(object s, RoutedEventArgs e)
		{
			var target = s as FrameworkElement;
			_flyout.ShowAt(target);
			var success = _flyout.Target == target;
			var t = new Windows.UI.Popups.MessageDialog(
				success
					? "Flyout.Target updated correctly."
					: "Flyout.Target updated incorrectly.")
				.ShowAsync();
		}
	}
}
