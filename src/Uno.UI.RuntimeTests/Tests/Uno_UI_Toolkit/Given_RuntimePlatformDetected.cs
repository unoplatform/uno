using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Toolkit;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Toolkit;

[TestClass]
public class Given_RuntimePlatformDetected
{
	[TestMethod]
	public void When_Platform_Is_Known()
	{
		// Future-proofing: every supported target must resolve to a concrete platform.
		// A new platform that isn't wired into RuntimePlatformHelper would surface here as Unknown.
		RuntimePlatformHelper.Current.Should().NotBe(UnoRuntimePlatform.Unknown);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_Skia_Then_IsSkia()
	{
		RuntimePlatformHelper.Current.IsSkia().Should().BeTrue();
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.NativeWinUI)]
	public void When_WindowsAppSdk_Then_NativeWinUI()
	{
		RuntimePlatformHelper.Current.Should().Be(UnoRuntimePlatform.NativeWinUI);
		RuntimePlatformHelper.Current.IsWindowsAppSdk().Should().BeTrue();
	}
}
