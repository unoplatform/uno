// The threading check itself is platform-agnostic, but this test cannot be expressed
// on WASM: the runtime is single-threaded, so Task.Run executes synchronously on the
// UI thread — there is no way to call from a non-UI thread to trigger the check.
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
	public void When_GetOrCreate_FromBackgroundThread_Throws()
	{
		var registry = new ElementRefHandleRegistry();

		Action act = () => Task.Run(() => registry.GetOrCreate(new Grid())).GetAwaiter().GetResult();
		act.Should().Throw<InvalidOperationException>().WithMessage("*non-UI thread*");
	}

	[TestMethod]
	public void When_TryResolve_FromBackgroundThread_Throws()
	{
		var registry = new ElementRefHandleRegistry();

		Action act = () => Task.Run(() => registry.TryResolve("1", out _)).GetAwaiter().GetResult();
		act.Should().Throw<InvalidOperationException>().WithMessage("*non-UI thread*");
	}
}
#endif
