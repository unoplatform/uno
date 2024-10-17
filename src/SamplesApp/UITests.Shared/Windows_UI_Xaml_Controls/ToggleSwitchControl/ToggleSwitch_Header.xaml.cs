using Windows.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.ToggleSwitchControl.Models;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ToggleSwitchControl
{
	[SampleControlInfo("ToggleSwitch", "ToggleSwitch_Header", typeof(ToggleSwitchViewModel))]
	public sealed partial class ToggleSwitch_Header : UserControl
	{
		public ToggleSwitch_Header()
		{
			this.InitializeComponent();
		}
	}
}
