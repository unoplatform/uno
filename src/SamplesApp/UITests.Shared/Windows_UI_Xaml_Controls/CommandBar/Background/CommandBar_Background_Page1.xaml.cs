using System;
using System.Threading.Tasks;
using Windows.UI;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.CommandBar.Background
{
	public sealed partial class CommandBar_Background_Page1 : Page
	{
		public CommandBar_Background_Page1()
		{
			this.InitializeComponent();
		}

		public async void OnButtonClicked(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(CommandBar_Background_Page2));

			await Task.Delay(TimeSpan.FromSeconds(0.5));

			Page1CommandBar.Background = new SolidColorBrush(Colors.Green);
		}
	}
}
