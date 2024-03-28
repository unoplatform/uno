using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{

	[SampleControlInfo("Buttons", nameof(RadioButton_IsEnabled_Automated))]
	public sealed partial class RadioButton_IsEnabled_Automated : UserControl
	{

		public RadioButton_IsEnabled_Automated()
		{
			this.InitializeComponent();

			CurrentRadioButton.Text = "None";
		}

		private void MyRadioButton_Click(object sender, RoutedEventArgs e)
		{

			if (MyRadioButton_1.IsChecked.Value)
			{
				CurrentRadioButton.Text = "Radio Button 1";
			}
			else if (MyRadioButton_2.IsChecked.Value)
			{
				CurrentRadioButton.Text = "Radio Button 2";
			}
			else
			{
				CurrentRadioButton.Text = "None";
			}
		}

		private void MyRadioButtonDisabler_Click(object sender, RoutedEventArgs e)
		{
			MyRadioButton_1.IsEnabled = !MyRadioButton_1.IsEnabled;
			MyRadioButton_2.IsEnabled = !MyRadioButton_2.IsEnabled;
		}
	}
}
