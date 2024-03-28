using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{

	[SampleControlInfo("Buttons", nameof(ToggleButton_IsEnabled_Automated))]
	public sealed partial class ToggleButton_IsEnabled_Automated : UserControl
	{

		public ToggleButton_IsEnabled_Automated()
		{
			this.InitializeComponent();

			ToggleButtonIsCheckedState.Text = MyToggleButton.IsChecked.ToString();
		}

		private void MyToggleButton_Click(object sender, RoutedEventArgs e)
		{
			ToggleButtonIsCheckedState.Text = MyToggleButton.IsChecked.ToString();
		}

		private void MyToggleButtonDisabler_Click(object sender, RoutedEventArgs e)
		{
			MyToggleButton.IsEnabled = !MyToggleButton.IsEnabled;
		}
	}
}
