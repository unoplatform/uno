using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;


namespace Uno.UI.Samples.Content.UITests.Animations
{
	[Sample("Visual states", Description = "VisualState using only one AdaptiveTrigger. Go from Portrait to Landscape to test")]
	public sealed partial class VisualState_AdaptiveTrigger_UsingOneStateOnly : UserControl
	{
		public VisualState_AdaptiveTrigger_UsingOneStateOnly()
		{
			this.InitializeComponent();
		}
	}
}
