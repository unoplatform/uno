using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Content.UITests.Flyout;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Flyout
{
	[Sample("Flyouts", "Flyout_ButtonInContent", viewModelType: typeof(FlyoutButonViewModel))]
	public sealed partial class Flyout_ButtonInContent : Page
	{
		public Flyout_ButtonInContent()
		{
			this.InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			output.Text += "Button clicked\n";
		}
	}
}
