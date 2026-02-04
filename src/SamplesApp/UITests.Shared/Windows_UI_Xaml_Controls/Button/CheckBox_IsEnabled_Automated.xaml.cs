using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{

	[Sample("Buttons", Name = nameof(CheckBox_IsEnabled_Automated))]
	public sealed partial class CheckBox_IsEnabled_Automated : UserControl
	{

		public CheckBox_IsEnabled_Automated()
		{
			this.InitializeComponent();

			CheckBoxIsCheckedState.Text = MyCheckBox.IsChecked.ToString();
		}

		private void MyCheckBox_Click(object sender, RoutedEventArgs e)
		{
			CheckBoxIsCheckedState.Text = MyCheckBox.IsChecked.ToString();
		}

		private void MyCheckBoxDisabler_Click(object sender, RoutedEventArgs e)
		{
			MyCheckBox.IsEnabled = !MyCheckBox.IsEnabled;
		}
	}
}
