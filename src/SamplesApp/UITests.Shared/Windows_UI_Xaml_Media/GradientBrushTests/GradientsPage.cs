using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Media.GradientBrushTests
{
	[Sample("Brushes"
#if __ANDROID__
		// Complete emulator crash when viewing this page
		// (not just the app, the full emulator!)
		, IgnoreInSnapshotTests = true
#endif
	)]
	public sealed partial class GradientsPage : Page
	{
		public GradientsPage()
		{
			this.InitializeComponent();
		}
	}
}
