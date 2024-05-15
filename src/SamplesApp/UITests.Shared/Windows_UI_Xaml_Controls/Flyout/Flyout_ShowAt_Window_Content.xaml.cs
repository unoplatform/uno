using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Controls.FlyoutTests
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("Flyouts")]
	public sealed partial class Flyout_ShowAt_Window_Content : Page
	{
		public Flyout_ShowAt_Window_Content()
		{
			this.InitializeComponent();
		}

		private void ButtonButton_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.UI.Xaml.Controls.Flyout flyout = new Microsoft.UI.Xaml.Controls.Flyout();
			flyout.Content =
				new Border()
				{
					Width = 300,
					Height = 300,
					Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
				};

			flyout.ShowAt((Button)sender);
		}

		private void WindowButton_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.UI.Xaml.Controls.Flyout flyout = new Microsoft.UI.Xaml.Controls.Flyout();
			flyout.Content =
				new Border()
				{
					Width = 300,
					Height = 300,
					Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
				};

			flyout.ShowAt(XamlRoot?.Content as FrameworkElement);
		}
	}
}
