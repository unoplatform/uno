using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.Flyout
{
	[SampleControlInfo("Flyouts", "Flyout_Simple")]
	public sealed partial class Flyout_Simple : UserControl
	{
		public Flyout_Simple()
		{
			this.InitializeComponent();
			this.SampleRoot.SampleDescription = "note: On smaller devices, buttons with * are not closable without native back button/gesture. They can also be closed by going to home screen or switching app.";
		}
	}
}
