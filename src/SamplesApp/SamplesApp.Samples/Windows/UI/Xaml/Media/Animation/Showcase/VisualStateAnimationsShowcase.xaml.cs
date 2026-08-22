using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media_Animation.Showcase;

[Sample(
	"Animations Showcase",
	IsManualTest = true,
	Description = "VisualStateManager-driven animations: a custom Pulse/Highlight state group plus an AdaptiveTrigger-driven layout switch.")]
public sealed partial class VisualStateAnimationsShowcase : Page
{
	public VisualStateAnimationsShowcase()
	{
		this.InitializeComponent();
	}
}
