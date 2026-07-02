using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UITest.Helpers.Queries;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

/// <summary>
/// Migrated from SamplesApp.UITests UnoSamples_Tests.ContentControl: exercises ContentPresenter /
/// ContentControl content-template rendering and toggling by loading the real sample pages.
/// The samples self-initialize (DataContext / template are set in their constructors), so
/// RunAsync (which only calls the parameterless ctor) is sufficient — no ViewModel wiring needed.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_ContentControl_UITest : SampleControlUITestBase
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)] // RunAsync + pointer injection are only supported on Skia/WASM runtime hosts.
	public async Task When_ContentPresenter_Template_Binds_And_Updates_On_Content_Change()
	{
		try
		{
			await RunAsync("Uno.UI.Samples.Content.UITests.ContentPresenter.ContentPresenter_Template");

			// Content is null, so the template's {Binding} resolves to the inherited DataContext ("DataContext").
			Assert.AreEqual("ContentPresenter:  DataContext", App.Marked("innerText").GetDependencyPropertyValue<string>("Text"));
			Assert.AreEqual("ContentControl:  DataContext", App.Marked("innerText2").GetDependencyPropertyValue<string>("Text"));

			// Toggles both Content values to 42; the templates should re-bind to the new content.
			App.Tap("actionButton");
			await TestServices.WindowHelper.WaitForIdle();

			await App.WaitForDependencyPropertyValueAsync(App.Marked("innerText"), "Text", "ContentPresenter:  42");
			await App.WaitForDependencyPropertyValueAsync(App.Marked("innerText2"), "Text", "ContentControl:  42");

			// Toggles back to null Content; the templated text elements must still be present.
			App.Tap("actionButton");
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(App.Query(App.Marked("innerText")).Length > 0, "innerText should still be present after toggling Content back to null.");
			Assert.IsTrue(App.Query(App.Marked("innerText2")).Length > 0, "innerText2 should still be present after toggling Content back to null.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)] // RunAsync + pointer injection are only supported on Skia/WASM runtime hosts.
	public async Task When_ContentPresenter_ContentTemplate_Toggled()
	{
		try
		{
			await RunAsync("Uno.UI.Samples.Content.UITests.ContentPresenter.ContentPresenter_Changing_ContentTemplate");

			// While the ContentTemplate is set, the raw content border is not materialized.
			Assert.AreEqual(0, App.Query(App.Marked("ContentViewBorder")).Length, "ContentViewBorder should be hidden while the ContentTemplate is set.");

			App.Tap("ToggleTemplateButton");

			// Clearing the ContentTemplate should surface the raw content border.
			await UITestHelper.WaitFor(
				() => App.Query(App.Marked("ContentViewBorder")).Length > 0,
				timeoutMS: 5000,
				message: "ContentViewBorder was not shown after clearing the ContentTemplate.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)] // RunAsync + pointer injection are only supported on Skia/WASM runtime hosts.
	public async Task When_ContentControl_ContentTemplate_Toggled()
	{
		try
		{
			await RunAsync("Uno.UI.Samples.Content.UITests.ContentControlTestsControl.ContentControl_Changing_ContentTemplate");

			// While the ContentTemplate is set, the raw content border is not materialized.
			Assert.AreEqual(0, App.Query(App.Marked("ContentViewBorder")).Length, "ContentViewBorder should be hidden while the ContentTemplate is set.");

			App.Tap("ToggleTemplateButton");

			// Clearing the ContentTemplate should surface the raw content border.
			await UITestHelper.WaitFor(
				() => App.Query(App.Marked("ContentViewBorder")).Length > 0,
				timeoutMS: 5000,
				message: "ContentViewBorder was not shown after clearing the ContentTemplate.");
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}
}
