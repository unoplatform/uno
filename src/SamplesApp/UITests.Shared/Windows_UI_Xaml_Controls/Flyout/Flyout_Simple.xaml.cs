using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.Flyout
{
	[SampleControlInfo("Flyout", "Flyout_Simple")]
	public sealed partial class Flyout_Simple : UserControl
	{
		public Flyout_Simple()
		{
			this.InitializeComponent();
			this.SampleRoot.SampleDescription = "note: buttons with * are not closable without native back button/gesture";
		}
	}
}
