using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

// Repro tests for https://github.com/unoplatform/uno/issues/4032

namespace Uno.UI.RuntimeTests.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_PageResourceName_Issue4032
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/4032")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Named_Resource_In_Page_Resources_ResourceDictionary_Is_Accessible()
	{
		// Issue: Named entries (x:Name) in Page.Resources > ResourceDictionary don't generate
		// named fields in the generated code-behind. The XAML generator uses the name
		// in the generated InitializeComponent() but never declares the field, causing
		// CS0103 in the generated .g.cs file.
		//
		// NOTE: If this project FAILS TO BUILD with CS0103 in the generated file,
		// the bug is confirmed. The runtime test below only runs if the build succeeds.

		var page = new PageResourceNameTest();
		WindowHelper.WindowContent = page;
		await WindowHelper.WaitForLoaded(page);
		await WindowHelper.WaitForIdle();

		// Verify the resource is accessible via the ResourceDictionary
		var brush = page.Resources["MyTopLevelBrush"] as SolidColorBrush;

		Assert.IsNotNull(brush,
			"Expected MyTopLevelBrush to be in page.Resources.");

		Assert.AreEqual(Microsoft.UI.Colors.Blue, brush.Color,
			$"Expected brush color to be Blue, but got {brush.Color}.");
	}
}
