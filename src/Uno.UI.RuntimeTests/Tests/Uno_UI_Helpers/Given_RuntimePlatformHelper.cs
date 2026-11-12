#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Helpers;

[TestClass]
[RunsOnUIThread]
public class Given_RuntimePlatformHelper
{
	[TestMethod]
	public void When_Current_Then_Matches_Test_Platform()
	{
		// RuntimeTestsPlatformHelper detects the platform independently, so a host that forgets to register shows up here.
		var expected = RuntimeTestsPlatformHelper.CurrentPlatform switch
		{
			RuntimeTestPlatforms.NativeWinUI => UnoRuntimePlatform.WindowsAppSdk,
			RuntimeTestPlatforms.SkiaWin32 => UnoRuntimePlatform.Win32,
			RuntimeTestPlatforms.SkiaX11 => UnoRuntimePlatform.X11,
			RuntimeTestPlatforms.SkiaMacOS => UnoRuntimePlatform.MacOS,
			RuntimeTestPlatforms.SkiaIslands => UnoRuntimePlatform.Islands,
			RuntimeTestPlatforms.SkiaWasm => UnoRuntimePlatform.Wasm,
			RuntimeTestPlatforms.SkiaAndroid => UnoRuntimePlatform.Android,
			RuntimeTestPlatforms.SkiaIOS => UnoRuntimePlatform.IOS,
			RuntimeTestPlatforms.SkiaTvOS => UnoRuntimePlatform.TvOS,
			RuntimeTestPlatforms.SkiaFrameBuffer => UnoRuntimePlatform.FrameBuffer,
			var other => throw new AssertFailedException($"Unmapped test platform {other}"),
		};

		Assert.AreEqual(expected, RuntimePlatformHelper.Current);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop | RuntimeTestPlatforms.NativeWinUI)]
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

	[TestMethod]
	public void When_Renderer_Then_Matches_Test_Backend()
	{
		var xamlRoot = TestServices.WindowHelper.XamlRoot;

		// RuntimeTestsBackendHelper reads the negotiated backend on its own, so it also covers a WebGPU fallback.
		var backend = RuntimeTestsBackendHelper.CurrentBackend;

		Assert.AreEqual(backend == RuntimeTestBackends.Skia, RuntimePlatformHelper.IsSkia(xamlRoot));
		Assert.AreEqual(backend == RuntimeTestBackends.WebGpu, RuntimePlatformHelper.IsWebGpu(xamlRoot));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
	public void When_Uno_Renders_Then_Exactly_One_Renderer()
	{
		var xamlRoot = TestServices.WindowHelper.XamlRoot;

		Assert.AreNotEqual(RuntimePlatformHelper.IsSkia(xamlRoot), RuntimePlatformHelper.IsWebGpu(xamlRoot));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.NativeWinUI)]
	public void When_WindowsAppSdk_Then_No_Uno_Renderer()
	{
		var xamlRoot = TestServices.WindowHelper.XamlRoot;

		Assert.IsFalse(RuntimePlatformHelper.IsSkia(xamlRoot));
		Assert.IsFalse(RuntimePlatformHelper.IsWebGpu(xamlRoot));
	}
}
