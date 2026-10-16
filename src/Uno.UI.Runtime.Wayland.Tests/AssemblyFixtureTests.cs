#nullable enable

using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class AssemblyFixtureTests
{
	[TestMethod]
	public void DataPackageFinalizerDispatchesDestroyed()
	{
		var destroyed = new TaskCompletionSource();
		CreatePackage(destroyed);

		GC.Collect();
		GC.WaitForPendingFinalizers();

		Assert.IsTrue(destroyed.Task.IsCompletedSuccessfully);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void CreatePackage(TaskCompletionSource destroyed)
	{
		var package = new DataPackage();
		package.Destroyed += (_, _) => destroyed.SetResult();
	}
}
