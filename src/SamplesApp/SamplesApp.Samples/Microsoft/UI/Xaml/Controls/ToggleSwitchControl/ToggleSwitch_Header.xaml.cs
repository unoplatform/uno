using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.ToggleSwitchControl.Models;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ToggleSwitchControl
{
	[Sample("ToggleSwitch", Name = "ToggleSwitch_Header", ViewModelType = typeof(ToggleSwitchViewModel))]
	public sealed partial class ToggleSwitch_Header : UserControl
	{
		public ToggleSwitch_Header()
		{
			this.InitializeComponent();
		}
	}
}
