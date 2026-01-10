using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{

	[Sample("Buttons", nameof(Button_IsEnabled_Automated))]
	public sealed partial class Button_IsEnabled_Automated : UserControl
	{
		private int _clickTotal = 0;

		public Button_IsEnabled_Automated()
		{
			this.InitializeComponent();
		}

		private void IncreaseClick(object sender, RoutedEventArgs e)
		{
			_clickTotal++;
			TotalClicks.Text = _clickTotal.ToString();
		}
	}
}
