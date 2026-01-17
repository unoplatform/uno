using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Samples.Help
{
	[Sample(
		"Help",
		Name = "Help",
		Description = "Help and keyboard shortcuts for the Samples app",
		UsesFrame = false,
		IgnoreInSnapshotTests = true,
		DisableKeyboardShortcuts = true,
		HideFromBrowser = true)]
	public sealed partial class HelpPage : Page
	{
		public HelpPage()
		{
			this.InitializeComponent();
		}
	}
}
