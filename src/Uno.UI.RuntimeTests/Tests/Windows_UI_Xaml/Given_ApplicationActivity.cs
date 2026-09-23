#if HAS_UNO
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.Dispatching;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_ApplicationActivity
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[DynamicDependency("get_Instance", "Microsoft.UI.Xaml.ApplicationActivity", "Uno.UI.Runtime.Skia.Android")]
	[DynamicDependency("Recreate", "Android.App.Activity", "Mono.Android")]
	public async Task When_Recreated_Dispatcher_Stays_Responsive()
	{
		// The runtime tests don't reference Mono.Android, hence the reflection.
		var instanceProperty = Type.GetType("Microsoft.UI.Xaml.ApplicationActivity, Uno.UI.Runtime.Skia.Android")
			?.GetProperty("Instance", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(instanceProperty);

		var original = instanceProperty.GetValue(null);
		Assert.IsNotNull(original);
		original.GetType().GetMethod("Recreate", Type.EmptyTypes)!.Invoke(original, null);

		await TestServices.WindowHelper.WaitFor(() => !ReferenceEquals(instanceProperty.GetValue(null), original), timeoutMS: 10000);
		await Task.Delay(1000);
		await TestServices.WindowHelper.WaitForIdle();

		// A recreated Activity whose window keeps cancelling its draw re-schedules a traversal every frame,
		// and that traversal's sync barrier limits the dispatcher to one item per frame.
		const int ItemCount = 300;
		var stopwatch = Stopwatch.StartNew();
		for (var i = 0; i < ItemCount; i++)
		{
			NativeDispatcher.Main.Enqueue(static () => { });
		}

		await TestServices.WindowHelper.WaitForIdle();
		stopwatch.Stop();

		Assert.IsTrue(
			stopwatch.ElapsedMilliseconds < 2000,
			$"Dispatching {ItemCount} empty items took {stopwatch.ElapsedMilliseconds}ms after the Activity was recreated.");
	}
}
#endif
