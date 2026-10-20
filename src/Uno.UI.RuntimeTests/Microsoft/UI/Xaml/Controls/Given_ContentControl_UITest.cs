using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UITest.Helpers.Queries;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Migrated from SamplesApp.UITests UnoSamples_Tests.ContentControl: ContentPresenter/ContentControl
// template rendering, driven through RunAsync since the samples self-initialize in their constructors.
[TestClass]
[RunsOnUIThread]
public class Given_ContentControl_UITest : SampleControlUITestBase
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // RunAsync + pointer injection are only supported on Skia/WASM runtime hosts.
	public async Task When_ContentPresenter_Template_Binds_And_Updates_On_Content_Change()
	{
		using var _ = UITestHelper.ResetWindowContent();
		await RunAsync("Uno.UI.Samples.Content.UITests.ContentPresenter.ContentPresenter_Template");

		// Content is null, so the template's {Binding} resolves to the inherited DataContext ("DataContext").
		Assert.AreEqual("ContentPresenter:  DataContext", TestServices.WindowHelper.WindowContent.FindFirstDescendantOrThrow<TextBlock>("innerText").Text);
		Assert.AreEqual("ContentControl:  DataContext", TestServices.WindowHelper.WindowContent.FindFirstDescendantOrThrow<TextBlock>("innerText2").Text);

		// Toggles both Content values to 42; the templates should re-bind to the new content.
		App.Tap("actionButton");
		await TestServices.WindowHelper.WaitForIdle();

		await UITestHelper.WaitFor(() => TestServices.WindowHelper.WindowContent.FindFirstDescendant<TextBlock>("innerText")?.Text == "ContentPresenter:  42", timeoutMS: 5000);
		await UITestHelper.WaitFor(() => TestServices.WindowHelper.WindowContent.FindFirstDescendant<TextBlock>("innerText2")?.Text == "ContentControl:  42", timeoutMS: 5000);

		// Toggles back to null Content; the templated text elements must still be present.
		App.Tap("actionButton");
		await TestServices.WindowHelper.WaitForIdle();

		Assert.IsNotNull(TestServices.WindowHelper.WindowContent.FindFirstDescendant<TextBlock>("innerText"), "innerText should still be present after toggling Content back to null.");
		Assert.IsNotNull(TestServices.WindowHelper.WindowContent.FindFirstDescendant<TextBlock>("innerText2"), "innerText2 should still be present after toggling Content back to null.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // RunAsync + pointer injection are only supported on Skia/WASM runtime hosts.
	public async Task When_ContentPresenter_ContentTemplate_Toggled()
	{
		using var _ = UITestHelper.ResetWindowContent();
		await RunAsync("Uno.UI.Samples.Content.UITests.ContentPresenter.ContentPresenter_Changing_ContentTemplate");

		// While the ContentTemplate is set, the raw content border is not materialized.
		Assert.IsNull(TestServices.WindowHelper.WindowContent.FindFirstDescendant<Border>("ContentViewBorder"), "ContentViewBorder should be hidden while the ContentTemplate is set.");

		App.Tap("ToggleTemplateButton");

		// Clearing the ContentTemplate should surface the raw content border.
		await UITestHelper.WaitFor(
			() => TestServices.WindowHelper.WindowContent.FindFirstDescendant<Border>("ContentViewBorder") is not null,
			timeoutMS: 5000,
			message: "ContentViewBorder was not shown after clearing the ContentTemplate.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // RunAsync + pointer injection are only supported on Skia/WASM runtime hosts.
	public async Task When_ContentControl_ContentTemplate_Toggled()
	{
		using var _ = UITestHelper.ResetWindowContent();
		await RunAsync("Uno.UI.Samples.Content.UITests.ContentControlTestsControl.ContentControl_Changing_ContentTemplate");

		// While the ContentTemplate is set, the raw content border is not materialized.
		Assert.IsNull(TestServices.WindowHelper.WindowContent.FindFirstDescendant<Border>("ContentViewBorder"), "ContentViewBorder should be hidden while the ContentTemplate is set.");

		App.Tap("ToggleTemplateButton");

		// Clearing the ContentTemplate should surface the raw content border.
		await UITestHelper.WaitFor(
			() => TestServices.WindowHelper.WindowContent.FindFirstDescendant<Border>("ContentViewBorder") is not null,
			timeoutMS: 5000,
			message: "ContentViewBorder was not shown after clearing the ContentTemplate.");
	}
}
