using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_ViewManagement;

[TestClass]
public class Given_StatusBar
{
#if __ANDROID__
#if false // this test doesnt work on the device from ci
	[ConditionalTest(IgnoredPlatforms = ~RuntimeTestPlatforms.SkiaAndroid)]
	[RunsOnUIThread]
	[RequiresFullWindow]
	public async Task StatusBar_Background_Value_ShouldDisplaceRenderArea()
	{
		if (!FeatureConfiguration.AndroidSettings.IsEdgeToEdgeEnabled)
		{
			Assert.Inconclusive("This test is only relevant when Edge2Edge is enabled.");
		}

		var sb = StatusBar.GetForCurrentView();
		var av = ApplicationView.GetForCurrentView();
		if (sb.OccludedRect.Height == 0 || av.IsFullScreenMode)
		{
			Assert.Inconclusive("The host device doesn't use a status bar or the app is in full-screen/android-immersive mode.");
		}

		var color = sb.BackgroundColor;
		try
		{
			// prepare initial state with no background, so we have the entire screen available.
			sb.BackgroundColor = null;

			var outerGrid = XamlHelper.LoadXaml<Grid>("""
				<Grid BorderBrush="Red" BorderThickness="5">
					<Grid toolkit:VisibleBoundsPadding.PaddingMask="All">
						<Border Background="SkyBlue" />
					</Grid>
				</Grid>
				""");
			var innerGrid = (Grid)outerGrid.Children[0];
			await UITestHelper.Load(outerGrid, x => x.IsLoaded);

			//var tree1 = outerGrid.TreeGraph();
			var snapshot1 = new { outerGrid.ActualHeight, innerGrid.Padding };

			// set a background to push down the "skia-canvas"
			sb.BackgroundColor = Colors.Pink;
			await UITestHelper.WaitForIdle();

			//var tree2 = outerGrid.TreeGraph();
			var snapshot2 = new { outerGrid.ActualHeight, innerGrid.Padding };

			// assumption: the test device SHOULD have a status-bar, and MAY have something at the bottom
			Assert.IsGreaterThan(snapshot2.ActualHeight, snapshot1.ActualHeight, $"The top-level element actual-height should have decreased: {snapshot1.ActualHeight} -> {snapshot2.ActualHeight}");
			Assert.IsTrue(
				snapshot2.Padding.Top == 0 && double.Abs(snapshot2.Padding.Bottom - snapshot1.Padding.Bottom) < 1,
				$"VisibleBoundsPadding on the inner Grid should lost the top side, and kept the bottom: {PrettyPrint.FormatThickness(snapshot1.Padding)} -> {PrettyPrint.FormatThickness(snapshot2.Padding)}");
		}
		finally
		{
			sb.BackgroundColor = color;
		}
	}
#endif
#endif
}
