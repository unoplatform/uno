using System.Reflection;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Samples.UnitTests
{
	[Sample(
		"Unit Tests",
		Name = "Unit Tests Runner",
		Description = "Test bench to run UI tests that does not require interaction (a.k.a. runtime tests)",
		UsesFrame = false,
		IgnoreInSnapshotTests = true,
		DisableKeyboardShortcuts = true)]
	public sealed partial class UnitTestsPage : Page
	{
		public UnitTestsPage()
		{
			this.InitializeComponent();

			// Manually load the runtime tests assembly
			Assembly.Load("Uno.UI.RuntimeTests");

#if __WASM__
			var t = typeof(SamplesApp.UnitTests.TSBindings.TSBindingsTests);
#endif
		}
	}
}
