using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.TextBox
{
	[Sample("TextBox")]
	public sealed partial class TextBox_Visibility : Page
	{
		public TextBox_Visibility()
		{
			this.InitializeComponent();
		}

		private void ShowHideButton_Click(object sender, RoutedEventArgs e)
		{
			MyTextBox.Visibility = MyTextBox.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
		}
	}
}
