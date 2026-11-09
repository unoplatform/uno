#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Helpers.LeakTestHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

// A binding dropped without being disposed must not leave its PropertyChanged handler on a source that outlives it.
[TestClass]
[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
public class Given_BindingStaleHandlers
{
	private const int Count = 5;

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinUI removes the handlers of collected bindings on its own schedule, and the loaded case doesn't load within the test's wait
	public async Task When_Bound_Elements_Collected_Then_Handlers_Removed(bool loaded)
	{
		var vm = new CountingViewModel(); // outlives the elements, and never raises
		var baseline = vm.SubscriberCount;

		var refs = await CreateBoundAsync(vm, loaded);
		Assert.AreEqual(baseline + Count, vm.SubscriberCount, "Each binding should subscribe once.");

		await CollectAsync(refs, () => vm.SubscriberCount == baseline);
		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The bound elements were not collected.");
		Assert.AreEqual(baseline, vm.SubscriberCount, "Handlers of collected bindings are still on the source.");
	}

	[TestMethod]
	// NativeWinUI: WinUI removes the handlers of collected bindings on its own schedule: flaky here.
	// SkiaWasm: stale stack slots can keep the elements alive, see the WASM branch below.
	// SkiaUIKit (6.x): the same on the iOS interpreter, 5 of 5 alive on CI.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaUIKit)]
	public async Task When_Raised_Before_Handlers_Removed_Then_Safe()
	{
		var vm = new CountingViewModel();
		var baseline = vm.SubscriberCount;
		var refs = await CreateBoundAsync(vm, loaded: false);

		// Don't yield to the dispatcher here: it would remove the handlers before the raise.
		if (OperatingSystem.IsBrowser())
		{
			// The test is excluded on WASM: no shape of this collection was reliable there. The Mono interpreter scans
			// stack slots conservatively and doesn't zero a frame's temporaries, so the pointers left behind by
			// creating the elements keep all of them alive whenever a scanned slot lands on them. Every change to the
			// frames on the stack moves that, so the outcome depends on code layout, not on the binding:
			// - CollectWithoutDispatcher(refs), the shared helper: 5 of 5 alive, deterministic, alone and with the other leak tests.
			// - This inline loop behind the OperatingSystem.IsBrowser() branch: passes alone, 5 of 5 alive with the other leak tests.
			// - The same loop inline with no branch: passed alone, with the other leak tests and on CI, by layout luck only.
			// Before re-enabling it on WASM, check it with the other leak tests (Given_BindingStaleHandlers and
			// Given_ItemsRepeater_Leak), not only alone.
			for (var i = 0; i < 10 && refs.Any(r => r.IsAlive); i++)
			{
				GC.Collect(2);
				GC.WaitForPendingFinalizers();
			}
		}
		else
		{
			CollectWithoutDispatcher(refs);
		}
		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The bound elements were not collected.");

		vm.Name = "changed"; // raises over the live handler list: removing a handler mid-raise would throw
		await CollectAsync(refs, () => vm.SubscriberCount == baseline);

		Assert.AreEqual(baseline, vm.SubscriberCount, "Handlers of collected bindings are still on the source.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinUI removes the handlers of collected bindings on its own schedule: flaky here
	public async Task When_Stale_Handlers_Removed_Then_Live_Binding_Still_Updates()
	{
		var vm = new CountingViewModel { Name = "first" };
		var baseline = vm.SubscriberCount;

		var live = new TextBlock { DataContext = vm };
		live.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(CountingViewModel.Name)) });
		var refs = await CreateBoundAsync(vm, loaded: false);

		await CollectAsync(refs);

		vm.Name = "second";
		Assert.AreEqual("second", live.Text);
		await WindowHelper.WaitForIdle();

		vm.Name = "third";
		Assert.AreEqual("third", live.Text, "The live binding stopped following its source.");
		Assert.AreEqual(baseline + 1, vm.SubscriberCount, "Only the live binding should still be subscribed.");
		GC.KeepAlive(live);
	}

	[TestMethod]
	public async Task When_Binding_Cleared_Then_Handler_Removed()
	{
		var vm = new CountingViewModel();
		var baseline = vm.SubscriberCount;

		var element = new TextBlock { DataContext = vm };
		element.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(CountingViewModel.Name)) });
		Assert.AreEqual(baseline + 1, vm.SubscriberCount);

		element.ClearValue(TextBlock.TextProperty);
		element.DataContext = null;
		await WindowHelper.WaitForIdle();

		vm.Name = "changed";
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(baseline, vm.SubscriberCount);
		Assert.AreEqual("", element.Text);
	}

	// The common case: a page and its view model become unreachable together. The source must not wait for the binding's
	// cleanup to be finalized and dispatched before it can be collected.
	[TestMethod]
	// 6.x only: the owner's DependencyObjectStore has a finalizer (master removed it), so the store keeps what it references
	// alive until finalizers run. Single-threaded WASM runs them only from the event loop, and the mobile runtimes fail too.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaMobile)]
	public async Task When_Element_And_Source_Dropped_Together_Then_Source_Collected_Without_Dispatcher()
	{
		await WindowHelper.WaitForIdle();
		var (elementRef, sourceRef) = CreateBoundToOwnSource();

		// Don't yield to the dispatcher here: the source must not depend on queued work to be collected.
		CollectWithoutDispatcher([sourceRef]);

		if (elementRef.IsAlive)
		{
			Assert.Inconclusive("The element was not collected without yielding on this platform.");
		}

		Assert.IsFalse(sourceRef.IsAlive, "The source is kept alive by the cleanup of a binding that died with it.");
	}

	// A nested path subscribes once per segment: every segment's handler must go away.
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinUI removes the handlers of collected bindings on its own schedule: flaky here
	public async Task When_Nested_Path_Bound_Elements_Collected_Then_Handlers_Removed()
	{
		var vm = new NestedViewModel(); // outlives the elements, and never raises
		var parentBaseline = vm.SubscriberCount;
		var childBaseline = vm.Child.SubscriberCount;

		var refs = CreateNestedBound(vm);
		Assert.AreEqual(parentBaseline + Count, vm.SubscriberCount, "Each binding should subscribe to the parent once.");
		Assert.AreEqual(childBaseline + Count, vm.Child.SubscriberCount, "Each binding should subscribe to the child once.");

		await CollectAsync(refs, () => vm.SubscriberCount == parentBaseline && vm.Child.SubscriberCount == childBaseline);

		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The bound elements were not collected.");
		Assert.AreEqual(parentBaseline, vm.SubscriberCount, "Handlers of collected bindings are still on the parent.");
		Assert.AreEqual(childBaseline, vm.Child.SubscriberCount, "Handlers of collected bindings are still on the child.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static List<WeakReference> CreateNestedBound(NestedViewModel vm)
	{
		var refs = new List<WeakReference>();
		for (var i = 0; i < Count; i++)
		{
			var element = new TextBlock { DataContext = vm };
			element.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Child.Name") });
			Assert.AreEqual("child", element.Text);
			refs.Add(new WeakReference(element));
		}

		return refs;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (WeakReference element, WeakReference source) CreateBoundToOwnSource()
	{
		var vm = new CountingViewModel { Name = "a" };
		var element = new TextBlock { DataContext = vm };
		element.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(CountingViewModel.Name)) });
		Assert.AreEqual("a", element.Text);

		// Tracks resurrection: a short weak reference is cleared even while finalization still keeps the source alive.
		return (new WeakReference(element), new WeakReference(vm, trackResurrection: true));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<List<WeakReference>> CreateBoundAsync(CountingViewModel vm, bool loaded)
	{
		var refs = new List<WeakReference>();
		var panel = new StackPanel();
		for (var i = 0; i < Count; i++)
		{
			var element = new TextBlock { DataContext = vm };
			element.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(nameof(CountingViewModel.Name)) });
			panel.Children.Add(element);
			refs.Add(new WeakReference(element));
		}

		if (loaded)
		{
			try
			{
				WindowHelper.WindowContent = panel;
				for (var i = 0; i < 20 && !panel.IsLoaded; i++)
				{
					await WindowHelper.WaitForIdle();
				}
				Assert.IsTrue(panel.IsLoaded, "The elements were never loaded.");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}

		await WindowHelper.WaitForIdle();
		return refs;
	}

	// Raises over its live handler list, like a source with a custom add/remove: removing a handler mid-raise throws.
	private sealed class CountingViewModel : INotifyPropertyChanged
	{
		private readonly List<PropertyChangedEventHandler> _handlers = new();
		private string _name = "";

		public int SubscriberCount => _handlers.Count;

		public event PropertyChangedEventHandler? PropertyChanged
		{
			add => _handlers.Add(value!);
			remove => _handlers.Remove(value!);
		}

		public string Name
		{
			get => _name;
			set
			{
				_name = value;
				var args = new PropertyChangedEventArgs(nameof(Name));
				foreach (var handler in _handlers) // throws if the list changes during the raise
				{
					handler(this, args);
				}
			}
		}
	}

	private sealed class NestedViewModel : INotifyPropertyChanged
	{
		private readonly List<PropertyChangedEventHandler> _handlers = new();

		public int SubscriberCount => _handlers.Count;

		public CountingViewModel Child { get; } = new() { Name = "child" };

		public event PropertyChangedEventHandler? PropertyChanged
		{
			add => _handlers.Add(value!);
			remove => _handlers.Remove(value!);
		}
	}
}
