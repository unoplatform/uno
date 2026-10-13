#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class AssemblyFixture
{
	private static Action<Action, NativeDispatcherPriority>? _previousDispatch;
	private static Func<bool>? _previousThreadAccess;

	[AssemblyInitialize]
	public static void Initialize(TestContext _)
	{
		_previousDispatch = NativeDispatcher.DispatchOverride;
		_previousThreadAccess = NativeDispatcher.HasThreadAccessOverride;
		NativeDispatcher.HasThreadAccessOverride = () => true;
		NativeDispatcher.DispatchOverride = (action, _) => action();
	}

	[AssemblyCleanup]
	public static void Cleanup()
	{
		// DataPackage finalizers enqueue callbacks; drain them while the headless dispatcher is still installed.
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		NativeDispatcher.DispatchOverride = _previousDispatch!;
		NativeDispatcher.HasThreadAccessOverride = _previousThreadAccess!;
	}
}
