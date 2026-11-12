#nullable enable

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using SamplesApp.UITests;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel_Resources;

// Migrated from SamplesApp.UITests Windows_ApplicationModel_Resources.ResourceLoader_Simple: exercises
// resource resolution (via x:Uid) against the default (unnamed) resw file and a named resw file, using
// plain, single-prefixed, and nested-prefixed keys.
[TestClass]
[RunsOnUIThread]
public class Given_ResourceLoader_UITest : SampleControlUITestBase
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // RunAsync is only supported on Skia/WASM runtime hosts.
	public async Task When_XUid_Resolves_From_Default_And_Named_Resource_Files()
	{
		using var _ = UITestHelper.ResetWindowContent();
		await RunAsync("UITests.Shared.Windows_ApplicationModel_Resources_ResourceLoader.ResourceLoader_Simple");

		string GetText(string name) => TestServices.WindowHelper.WindowContent.FindFirstDescendantOrThrow<TextBlock>(name).Text;

		Assert.AreEqual(
			@"This is ResourceLoader_Simple_tb01.Text in UITestsStrings\en-US\Resources.resw",
			GetText("tb01"));

		Assert.AreEqual(
			@"This is MyPrefix/ResourceLoader_Simple_tb02 in UITestsStrings\en-US\Resources.resw",
			GetText("tb02"));

		Assert.AreEqual(
			@"This is ResourceLoader_Simple_tb02.Text in UITestsStrings\en-US\NamedResources.resw",
			GetText("tb03"));

		Assert.AreEqual(
			@"This is MyPrefix/ResourceLoader_Simple_tb02 in UITestsStrings\en-US\NamedResources.resw",
			GetText("tb04"));

		Assert.AreEqual(
			@"This is MyPrefix/MyPrefix2/ResourceLoader_Simple_tb03 in UITestsStrings\en-US\NamedResources.resw",
			GetText("tb05"));
	}
}
