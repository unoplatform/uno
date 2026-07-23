using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Automation;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UITest.Helpers.Queries;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

// Migrated from SamplesApp.UITests AutomationId_Tests: bound AutomationId on ListView item containers.
[TestClass]
[RunsOnUIThread]
public class Given_AutomationId_UITest : SampleControlUITestBase
{
	private const string SampleName = "UITests.Shared.Windows_UI.Xaml_Automation.AutomationProperties_AutomationId";

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)] // RunAsync + pointer injection are only supported on Skia/WASM runtime hosts.
	public async Task When_ItemTemplate_Sets_AutomationId()
	{
		try
		{
			await RunAsync(SampleName);

			await UITestHelper.WaitFor(
				() => FindByAutomationId("Item01") is not null,
				timeoutMS: 5000,
				message: "Timed out waiting for the bound AutomationProperties.AutomationId to be applied to the list items.");

			for (var i = 1; i <= 3; i++)
			{
				var automationId = $"Item{i:00}";
				Assert.IsNotNull(
					FindByAutomationId(automationId),
					$"No element exposes the bound AutomationProperties.AutomationId '{automationId}'.");
			}
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)] // RunAsync + pointer injection are only supported on Skia/WASM runtime hosts.
	[DataRow("Item01", "Item 01", DisplayName = "Item01")]
	[DataRow("Item02", "Item 02", DisplayName = "Item02")]
	[DataRow("Item03", "Item 03", DisplayName = "Item03")]
	public async Task When_Item_Tapped_Then_Result_Updated(string automationId, string expectedText)
	{
		try
		{
			await RunAsync(SampleName);

			await UITestHelper.WaitFor(
				() => FindByAutomationId(automationId) is not null,
				timeoutMS: 5000,
				message: $"Timed out waiting for the list item with AutomationId '{automationId}'.");

			var target = FindByAutomationId(automationId);
			Assert.IsNotNull(target, $"No element exposes the AutomationId '{automationId}'.");

			var bounds = target.Rect;
			App.TapCoordinates(bounds.CenterX, bounds.CenterY);

			await App.WaitForDependencyPropertyValueAsync(App.Marked("result"), "Text", expectedText);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private QueryResult FindByAutomationId(string automationId)
		=> App.Query(QueryEx.Any)
			.FirstOrDefault(result => AutomationProperties.GetAutomationId(result.Element) == automationId);
}
