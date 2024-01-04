using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.RadioButtonTests
{

	[Sample("Buttons")]
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
