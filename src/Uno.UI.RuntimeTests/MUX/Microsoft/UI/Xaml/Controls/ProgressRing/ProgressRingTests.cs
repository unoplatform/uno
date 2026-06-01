using System.Threading.Tasks;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;
using Combinatorial.MSTest;

namespace Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls.ProgressRingTests;

[TestClass]
public class ProgressRingTests
{
	[TestMethod]
	[RunsOnUIThread]
	[CombinatorialData]
#if !(__WASM__ || HAS_SKOTTIE)
	[Ignore("Skottie is not supported on net6+ UWP targets")]
#endif
	public async Task ProgressRingDefaultHeightShouldBe32(bool useFluent)
	{
		using (useFluent ? Disposable.Empty : StyleHelper.UseUwpStyles())
		{
			var grid = new Grid();
			grid.Width = 100;
			grid.Height = 100;
			var progressRing = new Microsoft.UI.Xaml.Controls.ProgressRing() { IsActive = true };
			grid.Children.Add(progressRing);
			RunOnUIThread.Execute(() => TestServices.WindowHelper.WindowContent = grid);

			await TestServices.WindowHelper.WaitForLoaded(progressRing);

			Assert.AreEqual(32, progressRing.ActualHeight);
			Assert.AreEqual(32, progressRing.ActualWidth);
		}
	}
}
