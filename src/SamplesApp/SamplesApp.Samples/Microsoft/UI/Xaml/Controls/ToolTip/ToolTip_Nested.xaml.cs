using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ToolTip;

[Sample(nameof(ToolTip), nameof(ToolTip_Nested), Description = SampleDescription, IsManualTest = true)]
public sealed partial class ToolTip_Nested : UserControl
{
	private const string SampleDescription =
		"Hovering over any of the colored elements should only open a single tooltip, not multiple overlapping ones.";

	public ToolTip_Nested()
	{
		this.InitializeComponent();
	}
}
