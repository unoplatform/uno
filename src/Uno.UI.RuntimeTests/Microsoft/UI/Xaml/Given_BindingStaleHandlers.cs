#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Raised_Before_Handlers_Removed_Then_Safe()
	{
		var vm = new CountingViewModel();
		var baseline = vm.SubscriberCount;
		var refs = await CreateBoundAsync(vm, loaded: false);

		// Don't yield to the dispatcher here: it would remove the handlers before the raise.
		for (var i = 0; i < 10 && refs.Any(r => r.IsAlive); i++)
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
		}
		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The bound elements were not collected.");

		vm.Name = "changed"; // raises over the live handler list: removing a handler mid-raise would throw
		await CollectAsync(refs, () => vm.SubscriberCount == baseline);

		Assert.AreEqual(baseline, vm.SubscriberCount, "Handlers of collected bindings are still on the source.");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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

	// Collects at least 3 times, pumping the dispatcher in between so the removals posted by finalizers run.
	private static async Task CollectAsync(List<WeakReference> refs, Func<bool>? until = null)
	{
		until ??= () => !refs.Any(r => r.IsAlive);
		var sw = Stopwatch.StartNew();
		var minIterations = 3;

		while (minIterations-- > 0 || (!until() && sw.Elapsed < TimeSpan.FromSeconds(10)))
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);

			// Some platforms need a yield before weak references are cleared, as in Given_BindingMemoryLeak.
#if __SKIA__
			if (OperatingSystem.IsBrowser() || OperatingSystem.IsLinux() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
#endif
			{
				await Task.Yield();
				GC.Collect(2);
				GC.WaitForPendingFinalizers();
				GC.Collect(2);
			}

			await WindowHelper.WaitForIdle();
		}
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
}
