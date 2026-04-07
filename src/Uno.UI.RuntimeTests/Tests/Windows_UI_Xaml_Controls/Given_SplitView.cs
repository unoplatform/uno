using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_SplitView
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task Update_OpenPaneLength()
	{
		// This test asserts changes to the OpenPaneLength property are reflected in to the column definition's width.
		// We assume here that the control-template is setup with: SplitView\Grid\@ColumnDefinition[0].Width bound to TemplateSettings.OpenPaneLength
		// Should the template change, this test will need to be updated accordingly or voided.

		var sut = new SplitView()
		{
			OpenPaneLength = 100,
			CompactPaneLength = 50
		};
		await UITestHelper.Load(sut, x => x.IsLoaded);

		var rootGrid = sut.FindFirstDescendant<Grid>() ?? throw new InvalidOperationException("failed to find root grid.");
		var columnDefinition = rootGrid.ColumnDefinitions.ElementAtOrDefault(0) ?? throw new InvalidOperationException("root grid doesnt contains any column definition");

		Assert.AreEqual(sut.OpenPaneLength, columnDefinition.Width.Value, "ColumnDefinition Width should be equal to OpenPaneLength");

		sut.OpenPaneLength = 105;
		await UITestHelper.WaitForIdle();

		Assert.AreEqual(sut.OpenPaneLength, columnDefinition.Width.Value, "ColumnDefinition Width should be equal to OpenPaneLength after update");
	}

	// Repro tests for https://github.com/unoplatform/uno/issues/3747
	[TestMethod]
	[RunsOnUIThread]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/3747")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_SplitView_DisplayModeStates_Transition_Does_Not_Throw()
	{
		// Issue: Using the WinUI style for SplitView results in "multiple states are invalids".
		// The WinUI template has complex DisplayModeStates with VisualTransitions and
		// ObjectAnimationUsingKeyFrames for Visibility that Uno may not handle correctly.
		// Expected: Transitioning between DisplayModeStates should not throw exceptions.

		var sut = new SplitView
		{
			Width = 400,
			Height = 300,
			DisplayMode = SplitViewDisplayMode.Overlay,
			IsPaneOpen = false,
			Pane = new TextBlock { Text = "Pane" },
			Content = new TextBlock { Text = "Content" },
		};

		await UITestHelper.Load(sut, x => x.IsLoaded);
		await UITestHelper.WaitForIdle();

		Exception caught = null;
		try
		{
			// Toggle pane open/closed to trigger DisplayModeStates transitions
			sut.IsPaneOpen = true;
			await UITestHelper.WaitForIdle();
			sut.IsPaneOpen = false;
			await UITestHelper.WaitForIdle();

			// Change display mode to trigger more state transitions
			sut.DisplayMode = SplitViewDisplayMode.Inline;
			await UITestHelper.WaitForIdle();
			sut.IsPaneOpen = true;
			await UITestHelper.WaitForIdle();
			sut.DisplayMode = SplitViewDisplayMode.CompactOverlay;
			await UITestHelper.WaitForIdle();
			sut.DisplayMode = SplitViewDisplayMode.CompactInline;
			await UITestHelper.WaitForIdle();
		}
		catch (Exception ex)
		{
			caught = ex;
		}

		Assert.IsNull(caught,
			$"Expected SplitView DisplayModeStates transitions to not throw, but got: {caught?.Message}");

		// Verify the SplitView is in the expected final state
		Assert.IsTrue(sut.IsPaneOpen,
			"Expected SplitView pane to be open after the last IsPaneOpen=true.");
	}
}
