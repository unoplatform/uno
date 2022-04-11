using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Controls.Flyout
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample]
	public sealed partial class Flyout_ShowAt_Window_Content : Page
	{
		public Flyout_ShowAt_Window_Content()
		{
			this.InitializeComponent();

			Button button = new Button
			{
				Content = "Show at Window",
				Tag = Window.Current.Content,
			};
			Button button2 = new Button
			{
				Content = "Show at this button",				
			};
			button2.Tag = button2;

			var stackPanel = new StackPanel();
			stackPanel.Margin = new Thickness(60);
			stackPanel.Children.Add(button);
			stackPanel.Children.Add(button2);
			button.Click += Button_Click;
			button2.Click += Button_Click;

			Content = stackPanel;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Windows.UI.Xaml.Controls.Flyout flyout = new Windows.UI.Xaml.Controls.Flyout();
			flyout.Content = new TextBlock { Text = "Hi there" };

			flyout.ShowAt(((Button)sender).Tag as FrameworkElement);
		}
	}
}
