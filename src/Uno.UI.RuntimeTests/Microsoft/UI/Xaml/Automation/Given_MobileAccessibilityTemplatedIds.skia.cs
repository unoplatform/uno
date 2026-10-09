#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Covers the templated list of the AutomationProperties_AutomationId fixture through the native
/// adapter, in place of the iOS Xamarin.UITest suite that can no longer drive the Skia app.
/// </summary>
[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid | RuntimeTestPlatforms.SkiaIOS)]
public class Given_MobileAccessibilityTemplatedIds
{
	private static readonly string[] Items = { "Item01", "Item02", "Item03" };

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ItemTemplate_Binds_AutomationId_Then_Each_Item_Is_Exposed_With_Its_Id()
	{
		var listView = new ListView
		{
			ItemsSource = Items,
			SelectionMode = ListViewSelectionMode.Single,
			ItemTemplate = (DataTemplate)XamlReader.Load(
				"""
				<DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
					<TextBlock AutomationProperties.AutomationId="{Binding}" AutomationProperties.Name="{Binding}" Text="{Binding}" />
				</DataTemplate>
				"""),
		};
		await UITestHelper.Load(listView);

		for (var index = 0; index < Items.Length; index++)
		{
			var container = (ListViewItem)listView.ContainerFromIndex(index);
			Assert.IsNotNull(container, $"Container {index} must be realized.");
			var templateRoot = container.ContentTemplateRoot;
			Assert.IsNotNull(templateRoot, $"Container {index} must have applied its template.");

			var snapshot = MobileAccessibilityTestHelper.TryGetNativeSnapshot(templateRoot);

			Assert.IsNotNull(snapshot, $"'{Items[index]}' must be exposed as a native node.");
			Assert.IsTrue(
				snapshot.AutomationId?.EndsWith(Items[index], StringComparison.Ordinal) is true,
				$"Expected AutomationId ending '{Items[index]}', got '{snapshot.AutomationId}'.");
		}
	}
}
