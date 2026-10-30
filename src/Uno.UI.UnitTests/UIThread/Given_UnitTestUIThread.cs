#nullable enable

using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Dispatching;

namespace Uno.UI.Tests;

[UITestClass]
public class Given_UnitTestUIThread
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25231")]
	public async Task When_Dispatched_From_Another_Thread_Then_Runs_On_The_UI_Thread_After_The_Test_Yields()
	{
		var ranOnUIThread = (bool?)null;

		Task.Run(() => NativeDispatcher.Main.Enqueue(() => ranOnUIThread = UnitTestUIThread.IsCurrent)).Wait();

		// The test holds the UI thread, so the work must not have run, on the UI thread or anywhere else.
		Assert.IsNull(ranOnUIThread);

		await Task.Yield();

		Assert.IsTrue(ranOnUIThread);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25231")]
	public void When_UI_Thread_Enqueues_Behind_Pending_Dispatch_Then_It_Runs_Inline()
	{
		var foreignRan = false;
		var localRan = false;

		Task.Run(() => NativeDispatcher.Main.Enqueue(() => foreignRan = true)).Wait();
		Assert.IsFalse(foreignRan);

		NativeDispatcher.Main.Enqueue(() => localRan = true);

		Assert.IsTrue(localRan, "Work enqueued on the UI thread must run inline even with another thread's work pending.");
		Assert.IsTrue(foreignRan, "The pending work runs first, on the UI thread.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25231")]
	public void When_Class_Has_Tests_Then_It_Runs_On_The_UI_Thread()
	{
		var offThread = typeof(Given_UnitTestUIThread).Assembly
			.GetTypes()
			.Where(type => type.GetCustomAttribute<TestClassAttribute>() is { } attribute && attribute is not UITestClassAttribute)
			.Where(type => type.GetMethods().Any(method => method.GetCustomAttribute<TestMethodAttribute>() is not null))
			.Select(type => type.FullName)
			.ToArray();

		Assert.IsEmpty(offThread, $"Use [UITestClass] instead of [TestClass] on: {string.Join(", ", offThread)}");
	}
}
