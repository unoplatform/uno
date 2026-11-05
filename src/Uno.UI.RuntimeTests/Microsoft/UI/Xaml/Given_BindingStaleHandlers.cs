#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

	// The common case: a page and its view model become unreachable together. The source must not wait for the binding's
	// cleanup to be finalized and dispatched before it can be collected.
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Element_And_Source_Dropped_Together_Then_Source_Collected_Without_Dispatcher()
	{
		await WindowHelper.WaitForIdle();
		var (elementRef, sourceRef) = CreateBoundToOwnSource();

		// Don't yield to the dispatcher here: the source must not depend on queued work to be collected.
		for (var i = 0; i < 10 && sourceRef.IsAlive; i++)
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
		}

		if (elementRef.IsAlive)
		{
			Assert.Inconclusive("The element was not collected without yielding on this platform.");
		}

		Assert.IsFalse(sourceRef.IsAlive, "The source is kept alive by the cleanup of a binding that died with it.");
	}

	// An indexer path also listens to the collection's CollectionChanged: that handler must not outlive the binding either.
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public async Task When_Indexer_Bound_Elements_Collected_Then_Handlers_Removed()
	{
		var vm = new IndexedViewModel(); // outlives the elements, and never raises
		var propertyBaseline = vm.SubscriberCount;
		var collectionBaseline = vm.Items.CollectionChangedSubscriberCount;

		var refs = CreateIndexerBound(vm);
		Assert.AreEqual(collectionBaseline + Count, vm.Items.CollectionChangedSubscriberCount, "Each binding should listen to the collection once.");

		await CollectAsync(refs, () => vm.Items.CollectionChangedSubscriberCount == collectionBaseline && vm.SubscriberCount == propertyBaseline);

		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The bound elements were not collected.");
		Assert.AreEqual(collectionBaseline, vm.Items.CollectionChangedSubscriberCount, "CollectionChanged handlers of collected bindings are still on the collection.");
		Assert.AreEqual(propertyBaseline, vm.SubscriberCount, "PropertyChanged handlers of collected bindings are still on the source.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static List<WeakReference> CreateIndexerBound(IndexedViewModel vm)
	{
		var refs = new List<WeakReference>();
		for (var i = 0; i < Count; i++)
		{
			var element = new TextBlock { DataContext = vm };
			element.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Items[0]") });
			Assert.AreEqual("first", element.Text);
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

	private sealed class IndexedViewModel : INotifyPropertyChanged
	{
		private readonly List<PropertyChangedEventHandler> _handlers = new();

		public int SubscriberCount => _handlers.Count;

		public CountingObservableCollection Items { get; } = new() { "first" };

		public event PropertyChangedEventHandler? PropertyChanged
		{
			add => _handlers.Add(value!);
			remove => _handlers.Remove(value!);
		}
	}

	private sealed class CountingObservableCollection : ObservableCollection<string>
	{
		private int _collectionChangedSubscriberCount;

		public int CollectionChangedSubscriberCount => _collectionChangedSubscriberCount;

		public override event NotifyCollectionChangedEventHandler? CollectionChanged
		{
			add
			{
				base.CollectionChanged += value;
				_collectionChangedSubscriberCount++;
			}
			remove
			{
				base.CollectionChanged -= value;
				_collectionChangedSubscriberCount--;
			}
		}
	}
}
