using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Microsoft_UI_Xaml_Controls.NumberBoxTests
{
	[Sample("MUX", Name = "NumberBox Keyboard Placement", IsManualTest = true, Description = "Tests that the iPadOS floating keyboard appears near the focused NumberBox")]
	public sealed partial class NumberBox_KeyboardPlacement : UserControl
	{
		public NumberBox_KeyboardPlacement()
		{
			this.InitializeComponent();
		}
	}
}
