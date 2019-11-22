using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;


namespace Uno.UI.Samples.Content.UITests.Animations
{
	[SampleControlInfo("Visual states", description: "VisualState using only one AdaptiveTrigger. Go from Portrait to Landscape to test")]
	public sealed partial class VisualState_AdaptiveTrigger_UsingOneStateOnly : UserControl
	{
		public VisualState_AdaptiveTrigger_UsingOneStateOnly()
		{
			this.InitializeComponent();
		}
	}
}
