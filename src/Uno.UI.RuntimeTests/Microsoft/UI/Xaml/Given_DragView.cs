#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml;

[TestClass]
public class Given_DragView
{
#if HAS_UNO
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ToolTipPanel_SizeChanged_Then_TranslateX_Is_HalfWidth()
	{
		// Arrange - Create a DragView with tooltip content
		var dragView = new DragView(new DragUI(PointerDeviceType.Mouse));
		dragView.Caption = "Test Caption";
		dragView.TooltipVisibility = Visibility.Visible;

		await UITestHelper.Load(dragView);

		// Wait for layout to complete
		await TestServices.WindowHelper.WaitForIdle();

		// Act - Force a layout pass to ensure OnApplyTemplate and size calculations occur
		dragView.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		// Get the ToolTipPanel from the template
		var toolTipPanel = dragView.GetTemplateChild("ToolTipPanel") as FrameworkElement;

		// Assert
		Assert.IsNotNull(toolTipPanel, "ToolTipPanel should be found in the template");
		Assert.AreEqual(Visibility.Visible, toolTipPanel.Visibility, "ToolTipPanel should be visible");

		// Verify the panel has a TranslateTransform
		Assert.IsNotNull(toolTipPanel.RenderTransform, "ToolTipPanel should have a RenderTransform");
		var translateTransform = toolTipPanel.RenderTransform as TranslateTransform;
		Assert.IsNotNull(translateTransform, "RenderTransform should be a TranslateTransform");

		// The X translation should center the panel (negative half of the width)
		var expectedX = -toolTipPanel.ActualWidth / 2;
		Assert.AreEqual(expectedX, translateTransform.X, 0.1,
			$"TranslateTransform.X should be -{toolTipPanel.ActualWidth / 2} to center the tooltip panel");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ToolTipPanel_Resized_Then_TranslateX_Updates()
	{
		// Arrange
		var dragView = new DragView(new DragUI(PointerDeviceType.Mouse));
		dragView.Caption = "Short";
		dragView.TooltipVisibility = Visibility.Visible;
		dragView.MaxWidth = 200;

		await UITestHelper.Load(dragView);
		await TestServices.WindowHelper.WaitForIdle();
		dragView.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		var toolTipPanel = dragView.GetTemplateChild("ToolTipPanel") as FrameworkElement;
		Assert.IsNotNull(toolTipPanel, "ToolTipPanel should be found");

		var translateTransform = toolTipPanel.RenderTransform as TranslateTransform;
		Assert.IsNotNull(translateTransform, "TranslateTransform should exist");

		// Capture initial translation
		var initialWidth = toolTipPanel.ActualWidth;
		var initialX = translateTransform.X;

		// Act - Change the caption to something longer, causing a resize
		dragView.Caption = "This is a much longer caption that will cause the panel to resize";
		await TestServices.WindowHelper.WaitForIdle();
		dragView.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		// Assert - Width and translation should have changed
		var newWidth = toolTipPanel.ActualWidth;
		var newX = translateTransform.X;

		Assert.AreNotEqual(initialWidth, newWidth, "Width should have changed with longer caption");
		Assert.AreNotEqual(initialX, newX, "TranslateX should have updated when width changed");

		// Verify the new translation centers the panel
		var expectedX = -newWidth / 2;
		Assert.AreEqual(expectedX, newX, 0.1,
			$"TranslateTransform.X should be -{newWidth / 2} after resize");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ToolTipPanel_NotVisible_Then_TranslateX_NotSet()
	{
		// Arrange - Create a DragView with tooltip hidden
		var dragView = new DragView(new DragUI(PointerDeviceType.Mouse));
		dragView.Caption = "Test Caption";

		await UITestHelper.Load(dragView);

		dragView.TooltipVisibility = Visibility.Collapsed;

		await TestServices.WindowHelper.WaitForIdle();
		dragView.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		var toolTipPanel = dragView.GetTemplateChild("ToolTipPanel") as FrameworkElement;

		// Assert
		Assert.IsNotNull(toolTipPanel, "ToolTipPanel should be found in the template");
		Assert.AreEqual(Visibility.Collapsed, toolTipPanel.Visibility, "ToolTipPanel should be collapsed");
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ToolTipPanel_Visibility_Changes_Then_Centering_Applied()
	{
		// Arrange
		var dragView = new DragView(new DragUI(PointerDeviceType.Mouse));
		dragView.Caption = "Test Caption";

		await UITestHelper.Load(dragView);

		dragView.TooltipVisibility = Visibility.Collapsed;

		await TestServices.WindowHelper.WaitForIdle();

		// Act - Make the tooltip visible
		dragView.TooltipVisibility = Visibility.Visible;
		await TestServices.WindowHelper.WaitForIdle();
		dragView.UpdateLayout();
		await TestServices.WindowHelper.WaitForIdle();

		var toolTipPanel = dragView.GetTemplateChild("ToolTipPanel") as FrameworkElement;

		// Assert
		Assert.IsNotNull(toolTipPanel, "ToolTipPanel should be found");
		Assert.AreEqual(Visibility.Visible, toolTipPanel.Visibility, "ToolTipPanel should now be visible");

		var translateTransform = toolTipPanel.RenderTransform as TranslateTransform;
		Assert.IsNotNull(translateTransform, "TranslateTransform should exist");

		// Verify centering is applied
		var expectedX = -toolTipPanel.ActualWidth / 2;
		Assert.AreEqual(expectedX, translateTransform.X, 0.1,
			"TranslateTransform.X should center the panel after visibility change");
	}
#endif
}
