#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using SamplesApp.UITests;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel_Resources;

// Migrated from SamplesApp.UITests Windows_ApplicationModel_Resources.ResourceLoader_Simple: exercises
// resource resolution (via x:Uid) against the default (unnamed) resw file and a named resw file, using
// plain, single-prefixed, and nested-prefixed keys.
[TestClass]
[RunsOnUIThread]
public class Given_ResourceLoader_UITest : SampleControlUITestBase
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)] // RunAsync is only supported on Skia/WASM runtime hosts.
	public async Task When_XUid_Resolves_From_Default_And_Named_Resource_Files()
	{
		try
		{
			await RunAsync("UITests.Shared.Windows_ApplicationModel_Resources_ResourceLoader.ResourceLoader_Simple");

			// The sample's TextBlocks carry only x:Uid (no x:Name), so App.Marked (Name-based) can't find
			// them; walk the tree instead. Each label TextBlock is immediately followed by the TextBlock
			// whose Text was resolved via its x:Uid.
			var textBlocks = new List<TextBlock>();
			CollectTextBlocks(TestServices.WindowHelper.WindowContent, textBlocks);

			Assert.AreEqual(10, textBlocks.Count, "Expected 5 label/value TextBlock pairs.");

			Assert.AreEqual(
				@"This is ResourceLoader_Simple_tb01.Text in UITestsStrings\en-US\Resources.resw",
				textBlocks[1].Text);

			Assert.AreEqual(
				@"This is MyPrefix/ResourceLoader_Simple_tb02 in UITestsStrings\en-US\Resources.resw",
				textBlocks[3].Text);

			Assert.AreEqual(
				@"This is ResourceLoader_Simple_tb02.Text in UITestsStrings\en-US\NamedResources.resw",
				textBlocks[5].Text);

			Assert.AreEqual(
				@"This is MyPrefix/ResourceLoader_Simple_tb02 in UITestsStrings\en-US\NamedResources.resw",
				textBlocks[7].Text);

			Assert.AreEqual(
				@"This is MyPrefix/MyPrefix2/ResourceLoader_Simple_tb03 in UITestsStrings\en-US\NamedResources.resw",
				textBlocks[9].Text);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private static void CollectTextBlocks(DependencyObject? root, List<TextBlock> results)
	{
		if (root is null)
		{
			return;
		}

		if (root is TextBlock textBlock)
		{
			results.Add(textBlock);
		}

		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			CollectTextBlocks(VisualTreeHelper.GetChild(root, i), results);
		}
	}
}
