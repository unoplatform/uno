#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Helpers;

[TestClass]
public class Given_RuntimePlatformHelper
{
	[TestMethod]
	public void When_Current_Then_Matches_Test_Platform()
	{
		// RuntimeTestsPlatformHelper detects the platform independently, so a host that forgets to register shows up here.
		var expected = RuntimeTestsPlatformHelper.CurrentPlatform switch
		{
			RuntimeTestPlatforms.NativeWinUI => UnoRuntimePlatform.NativeWinUI,
			RuntimeTestPlatforms.SkiaWin32 => UnoRuntimePlatform.SkiaWin32,
			RuntimeTestPlatforms.SkiaX11 => UnoRuntimePlatform.SkiaX11,
			RuntimeTestPlatforms.SkiaMacOS => UnoRuntimePlatform.SkiaMacOS,
			RuntimeTestPlatforms.SkiaIslands => UnoRuntimePlatform.SkiaIslands,
			RuntimeTestPlatforms.SkiaWasm => UnoRuntimePlatform.SkiaWasm,
			RuntimeTestPlatforms.SkiaAndroid => UnoRuntimePlatform.SkiaAndroid,
			RuntimeTestPlatforms.SkiaIOS => UnoRuntimePlatform.SkiaIOS,
			RuntimeTestPlatforms.SkiaTvOS => UnoRuntimePlatform.SkiaTvOS,
			RuntimeTestPlatforms.SkiaFrameBuffer => UnoRuntimePlatform.SkiaFrameBuffer,
			var other => throw new AssertFailedException($"Unmapped test platform {other}"),
		};

		Assert.AreEqual(expected, RuntimePlatformHelper.Current);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_Skia_Then_IsSkia()
	{
		Assert.IsTrue(RuntimePlatformHelper.Current.IsSkia());
		Assert.IsFalse(RuntimePlatformHelper.Current.IsWindowsAppSdk());
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.NativeWinUI)]
	public void When_WindowsAppSdk_Then_IsWindowsAppSdk()
	{
		Assert.IsTrue(RuntimePlatformHelper.Current.IsWindowsAppSdk());
		Assert.IsFalse(RuntimePlatformHelper.Current.IsSkia());
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public void When_Desktop_Then_IsDesktop()
	{
		Assert.IsTrue(RuntimePlatformHelper.Current.IsDesktop());
		Assert.IsFalse(RuntimePlatformHelper.Current.IsMobile());
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMobile)]
	public void When_Mobile_Then_IsMobile()
	{
		Assert.IsTrue(RuntimePlatformHelper.Current.IsMobile());
		Assert.IsFalse(RuntimePlatformHelper.Current.IsDesktop());
	}
}
