using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_Application_Launch
{
	/// <summary>
	/// Whether the default fonts had finished loading when OnLaunched ran, recorded by the SamplesApp.
	/// </summary>
	public static bool? DefaultFontsLoadedAtLaunch { get; set; }

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26617")]
	public void When_Launched_Then_Default_Fonts_Are_Loaded()
		=> Assert.IsTrue(DefaultFontsLoadedAtLaunch, "Content created in OnLaunched would measure with fallback fonts.");
}
