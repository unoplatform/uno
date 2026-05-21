#if HAS_UNO
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

		Assert.ThrowsException<InvalidOperationException>(
			() => Task.Run(() => registry.GetOrCreate(new Grid())).GetAwaiter().GetResult());
	}

	[TestMethod]
	public void When_TryResolve_FromBackgroundThread_Throws()
	{
		var registry = new ElementRefHandleRegistry();

		Assert.ThrowsException<InvalidOperationException>(
			() => Task.Run(() => registry.TryResolve("1", out _)).GetAwaiter().GetResult());
	}
}
#endif
