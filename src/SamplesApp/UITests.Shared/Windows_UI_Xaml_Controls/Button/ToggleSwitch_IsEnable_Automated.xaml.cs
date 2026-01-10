using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{

	[Sample("Buttons", nameof(ToggleSwitch_IsEnable_Automated))]
	public sealed partial class ToggleSwitch_IsEnable_Automated : UserControl
	{

		public ToggleSwitch_IsEnable_Automated()
		{
			this.InitializeComponent();

			ToggleSwitch_1_IsOn.Text = "ToggleSwitch_1 is OFF";
			ToggleSwitch_2_IsOn.Text = "ToggleSwitch_2 is OFF";
		}

		private void ToggleSwitch_1_Toggled(object sender, RoutedEventArgs e)
		{
			if (((ToggleSwitch)sender).IsOn)
			{
				ToggleSwitch_1_IsOn.Text = "ToggleSwitch_1 is ON";
			}
			else
			{
				ToggleSwitch_1_IsOn.Text = "ToggleSwitch_1 is OFF";
			}
		}

		private void ToggleSwitch_2_Toggled(object sender, RoutedEventArgs e)
		{
			if (((ToggleSwitch)sender).IsOn)
			{
				ToggleSwitch_2_IsOn.Text = "ToggleSwitch_2 is ON";
			}
			else
			{
				ToggleSwitch_2_IsOn.Text = "ToggleSwitch_2 is OFF";
			}
		}
	}
}
