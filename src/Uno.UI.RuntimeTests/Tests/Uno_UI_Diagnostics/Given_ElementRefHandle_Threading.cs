// The threading check itself is platform-agnostic, but this test cannot be expressed
// on single-threaded runtimes: Task.Run executes synchronously on the UI thread, so
// there is no way to call from a non-UI thread to trigger the check.
// Native WASM (!__WASM__) is always single-threaded. Skia WASM Browser is excluded
// at the method level via [PlatformCondition] for the same reason.
#if HAS_UNO && !__WASM__
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Diagnostics;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Diagnostics;

[TestClass]
[RunsOnUIThread]
public class Given_ElementRefHandle_Threading
{
	private bool _savedDisableThreadingCheck;

	[TestInitialize]
	public void Initialize()
	{
		_savedDisableThreadingCheck = FeatureConfiguration.ElementRefHandle.DisableThreadingCheck;
		FeatureConfiguration.ElementRefHandle.DisableThreadingCheck = false;
	}

	[TestCleanup]
	public void Cleanup()
	{
		FeatureConfiguration.ElementRefHandle.DisableThreadingCheck = _savedDisableThreadingCheck;
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
	public void When_GetOrCreate_FromBackgroundThread_Throws()
	{
		var registry = new ElementRefHandleRegistry();

		Action act = () => Task.Run(() => registry.GetOrCreate(new Grid())).GetAwaiter().GetResult();
		act.Should().Throw<InvalidOperationException>().WithMessage("*non-UI thread*");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm)]
	public void When_TryResolve_FromBackgroundThread_Throws()
	{
		var registry = new ElementRefHandleRegistry();

		Action act = () => Task.Run(() => registry.TryResolve("1", out _)).GetAwaiter().GetResult();
		act.Should().Throw<InvalidOperationException>().WithMessage("*non-UI thread*");
	}
}
#endif
