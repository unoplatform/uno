using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_ViewManagement;

[TestClass]
public class Given_InputPane
{
	// Validates the Skia bring-into-view path that InputPane.OccludedRect drives
	// (InputPane.skia.cs -> EnsureFocusedElementInViewPartial -> ScrollContentPresenter.Pad +
	// StartBringIntoView). This is the device-independent half of the WASM soft-keyboard fix:
	// the WASM head now feeds OccludedRect, and this asserts the shared consumer reacts to it.
	[TestMethod]
	[RunsOnUIThread]
	[RequiresFullWindow]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public async Task When_OccludedRect_Set_Then_Focused_TextBox_Scrolled_Into_View()
	{
		var textBox = new TextBox { Height = 40, PlaceholderText = "bottom" };
		var scrollViewer = new ScrollViewer
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			Content = new StackPanel
			{
				Children =
				{
					new Border { Height = 2000 },
					textBox,
				},
			},
		};

		var inputPane = InputPane.GetForCurrentView();
		try
		{
			await UITestHelper.Load(scrollViewer);

			// Bring the bottom TextBox fully into view, then focus it.
			scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null, disableAnimation: true);
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(textBox.Focus(FocusState.Programmatic), "TextBox failed to take focus.");
			await WindowHelper.WaitForIdle();

			var offsetBeforeOcclusion = scrollViewer.VerticalOffset;

			// Simulate the on-screen keyboard occluding the bottom half of the ScrollViewer,
			// where the focused TextBox now sits.
			var scrollViewerTopLeft = scrollViewer.TransformToVisual(null).TransformPoint(default);
			var occludedTop = scrollViewerTopLeft.Y + (scrollViewer.ActualHeight / 2);
			inputPane.OccludedRect = new Rect(scrollViewerTopLeft.X, occludedTop, scrollViewer.ActualWidth, scrollViewer.ActualHeight);

			// OccludedRect -> OnOccludedRectChanged schedules the pad + StartBringIntoView across
			// two dispatcher hops and a layout pass, so wait for the resulting scroll.
			await WindowHelper.WaitFor(
				() => scrollViewer.VerticalOffset > offsetBeforeOcclusion + 1,
				message: $"Focused TextBox was not scrolled above the keyboard (offset stayed at {offsetBeforeOcclusion}).");
		}
		finally
		{
			inputPane.OccludedRect = new Rect(0, 0, 0, 0);
			WindowHelper.WindowContent = null;
		}
	}
}
