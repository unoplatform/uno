using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{

	[SampleControlInfo("Buttons", nameof(HyperlinkButton_IsEnabled_Automated))]
	public sealed partial class HyperlinkButton_IsEnabled_Automated : UserControl
	{
		private int _clickTotal = 0;

		public HyperlinkButton_IsEnabled_Automated()
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
