#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Helpers.LeakTestHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

// A binding dropped without being disposed must not leave subscriptions on a source that outlives it,
// whatever kind of source it binds to: a collection it indexes into, a DependencyObject, or an x:Bind source.
[TestClass]
[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
public class Given_BindingSourceStaleHandlers
{
	private const int Count = 5;
	private const string IndexerPath = nameof(CountingViewModel.Items) + "[0]";

	// Path=Items[0]: the binding subscribes to Items.CollectionChanged, with a handler that held the binding strongly.
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinUI removes the handlers of collected bindings on its own schedule: flaky here
	public async Task When_Indexer_Path_Elements_Collected_Then_CollectionChanged_Handlers_Removed(bool loaded)
	{
		var vm = new CountingViewModel(); // outlives the elements, and never raises
		var baseline = vm.Items.CollectionChangedCount;

		var refs = await CreateBoundAsync(vm, IndexerPath, loaded);
		Assert.AreEqual(baseline + Count, vm.Items.CollectionChangedCount, "Each binding should subscribe once.");

		await CollectAsync(refs, () => vm.Items.CollectionChangedCount == baseline);
		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The bound elements were not collected.");
		Assert.AreEqual(baseline, vm.Items.CollectionChangedCount, "CollectionChanged handlers of collected bindings are still on the collection.");
	}

	// The binding that the collection's handler kept alive also kept its PropertyChanged subscriptions alive:
	// on the view model (for Items) and on the collection (for the indexer). Requires PropertyChanged
	// handlers of collected bindings to be removed (BindingPath.SubscribeToNotifyPropertyChanged).
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinUI removes the handlers of collected bindings on its own schedule: flaky here
	public async Task When_Indexer_Path_Elements_Collected_Then_PropertyChanged_Handlers_Removed()
	{
		var vm = new CountingViewModel();
		var vmBaseline = vm.SubscriberCount;
		var itemsBaseline = vm.Items.PropertyChangedCount;

		var refs = await CreateBoundAsync(vm, IndexerPath, loaded: true);
		Assert.AreEqual(vmBaseline + Count, vm.SubscriberCount, "Each binding should subscribe once to the view model.");

		await CollectAsync(refs, () => vm.SubscriberCount == vmBaseline && vm.Items.PropertyChangedCount == itemsBaseline);
		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The bound elements were not collected.");
		Assert.AreEqual(vmBaseline, vm.SubscriberCount, "PropertyChanged handlers of collected bindings are still on the view model.");
		Assert.AreEqual(itemsBaseline, vm.Items.PropertyChangedCount, "PropertyChanged handlers of collected bindings are still on the collection.");
	}

	// The handler of a collected binding must not remove itself during the raise: LiveCollection raises over its live
	// handler list, which throws if it changes, where ObservableCollection's multicast delegate wouldn't notice.
	[TestMethod]
	// NativeWinUI: WinUI removes the handlers of collected bindings on its own schedule: flaky here.
	// SkiaWasm: stale stack slots can keep the elements alive, as in Given_BindingStaleHandlers.When_Raised_Before_Handlers_Removed_Then_Safe.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI | RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_Indexer_Path_Collection_Raises_After_Collection_Then_Safe()
	{
		var vm = new CountingViewModel();
		var refs = await CreateBoundAsync(vm, nameof(CountingViewModel.LiveItems) + "[0]", loaded: false);
		Assert.AreEqual(Count, vm.LiveItems.CollectionChangedCount, "Each binding should subscribe once.");

		// Let the work queued while creating the elements run, then stop yielding to the dispatcher:
		// it would remove the handlers before the raise.
		for (var i = 0; i < 3; i++)
		{
			await WindowHelper.WaitForIdle();
		}

		CollectWithoutDispatcher(refs);
		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The bound elements were not collected.");

		vm.LiveItems[0] = "changed"; // reaches the handlers of the collected bindings
		await CollectAsync(refs, () => vm.LiveItems.CollectionChangedCount == 0);

		Assert.AreEqual(0, vm.LiveItems.CollectionChangedCount, "CollectionChanged handlers of collected bindings are still on the collection.");
	}

	// The subscription is made by the indexer on the collection it indexes into, so it follows a replaced collection.
	[TestMethod]
	public async Task When_Indexer_Path_Collection_Replaced_Then_Handler_Moves_To_New_Collection()
	{
		var vm = new CountingViewModel();
		var original = vm.Items;

		var element = new TextBlock { DataContext = vm };
		element.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(IndexerPath) });
		Assert.AreEqual(1, original.CollectionChangedCount);

		var replacement = new CountingCollection { "replaced" };
		vm.Items = replacement;
		await WindowHelper.WaitForIdle();

		Assert.AreEqual("replaced", element.Text);
		Assert.AreEqual(0, original.CollectionChangedCount, "The binding is still subscribed to the replaced collection.");
		Assert.AreEqual(1, replacement.CollectionChangedCount, "The binding didn't subscribe to the new collection.");
		GC.KeepAlive(element);
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinUI removes the handlers of collected bindings on its own schedule: flaky here
	public async Task When_Indexer_Path_Stale_Handlers_Removed_Then_Live_Binding_Still_Updates()
	{
		var vm = new CountingViewModel();
		var baseline = vm.Items.CollectionChangedCount;

		var live = new TextBlock { DataContext = vm };
		live.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(IndexerPath) });
		Assert.AreEqual("first", live.Text);

		var refs = await CreateBoundAsync(vm, IndexerPath, loaded: false);
		await CollectAsync(refs, () => vm.Items.CollectionChangedCount == baseline + 1);

		vm.Items[0] = "second";
		Assert.AreEqual("second", live.Text);

		vm.Items.Insert(0, "third");
		Assert.AreEqual("third", live.Text, "The live binding stopped following its collection.");
		Assert.AreEqual(baseline + 1, vm.Items.CollectionChangedCount, "Only the live binding should still be subscribed.");
		GC.KeepAlive(live);
	}

	// The indexer re-reads its value on CollectionChanged, including when the index goes out of range.
	[TestMethod]
	public async Task When_Indexer_Path_Collection_Emptied_Then_Refilled_Then_Live_Binding_Follows()
	{
		var vm = new CountingViewModel();
		var live = new TextBlock { DataContext = vm };
		live.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(IndexerPath) });
		Assert.AreEqual("first", live.Text);

		vm.Items.Clear(); // Reset, index now out of range
		vm.Items.Add("second");
		Assert.AreEqual("second", live.Text);

		vm.Items.RemoveAt(0); // Remove, index out of range again
		vm.Items.Add("third");
		Assert.AreEqual("third", live.Text);

		await WindowHelper.WaitForIdle();
		GC.KeepAlive(live);
	}

	[TestMethod]
	public async Task When_Indexer_Path_Collection_Replaced_Then_Live_Binding_Follows_New_Collection()
	{
		var vm = new CountingViewModel();
		var original = vm.Items;
		var live = new TextBlock { DataContext = vm };
		live.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(IndexerPath) });

		vm.Items = new CountingCollection { "replaced" };
		Assert.AreEqual("replaced", live.Text);

		vm.Items[0] = "changed";
		Assert.AreEqual("changed", live.Text, "The binding doesn't follow the new collection.");

		original[0] = "stale";
		Assert.AreEqual("changed", live.Text, "The binding still follows the replaced collection.");

		await WindowHelper.WaitForIdle();
		GC.KeepAlive(live);
	}

	// The indexer's new value must reach the rest of the path, not only a target bound to the indexer itself.
	[TestMethod]
	public async Task When_Longer_Indexer_Path_Collection_Changes_Then_Live_Binding_Follows()
	{
		var vm = new CountingViewModel();
		var live = new TextBlock { DataContext = vm };
		live.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(IndexerPath + ".Length") });
		Assert.AreEqual("5", live.Text); // "first"

		vm.Items[0] = "abc";
		Assert.AreEqual("3", live.Text);

		vm.Items.Insert(0, "ab");
		Assert.AreEqual("2", live.Text);

		await WindowHelper.WaitForIdle();
		GC.KeepAlive(live);
	}

	[TestMethod]
	public async Task When_XBind_Indexer_Path_Collection_Changes_Then_Follows()
	{
		var vm = new CountingViewModel();
		var control = new XBindIndexerControl(vm);
		try
		{
			WindowHelper.WindowContent = control;
			await WindowHelper.WaitForLoaded(control);
			Assert.AreEqual("first", control.ItemText.Text);
			Assert.AreEqual("5", control.LengthText.Text);

			vm.Items[0] = "abc";
			Assert.AreEqual("abc", control.ItemText.Text);
			Assert.AreEqual("3", control.LengthText.Text);

			vm.Items.Insert(0, "ab");
			Assert.AreEqual("ab", control.ItemText.Text);
			Assert.AreEqual("2", control.LengthText.Text);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Indexer_Path_Binding_Cleared_Then_CollectionChanged_Handler_Removed()
	{
		var vm = new CountingViewModel();
		var baseline = vm.Items.CollectionChangedCount;

		var element = new TextBlock { DataContext = vm };
		element.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(IndexerPath) });
		Assert.AreEqual(baseline + 1, vm.Items.CollectionChangedCount);

		element.ClearValue(TextBlock.TextProperty);
		await WindowHelper.WaitForIdle();

		Assert.AreEqual(baseline, vm.Items.CollectionChangedCount);
		vm.Items[0] = "changed";
		Assert.AreEqual("", element.Text);
	}

	// A DependencyObject source: the binding registers a property-changed callback on it.
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // Reads Uno's property-changed callbacks
	public async Task When_DependencyObject_Source_Elements_Collected_Then_Callbacks_Removed(bool loaded)
	{
		var source = new Border { Tag = "tag" }; // outlives the elements
		var baseline = CallbackCount(source, FrameworkElement.TagProperty);

		var refs = await CreateBoundToDependencyObjectAsync(source, loaded);
		Assert.AreEqual(baseline + Count, CallbackCount(source, FrameworkElement.TagProperty), "Each binding should register one callback.");

		await CollectAsync(refs, () => CallbackCount(source, FrameworkElement.TagProperty) == baseline);
		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The bound elements were not collected.");
		Assert.AreEqual(baseline, CallbackCount(source, FrameworkElement.TagProperty), "Callbacks of collected bindings are still on the source.");

		source.Tag = "changed"; // must not reach a collected binding
	}

	// {x:Bind Mode=OneWay} goes through the same BindingPath as {Binding}. Requires PropertyChanged
	// handlers of collected bindings to be removed (BindingPath.SubscribeToNotifyPropertyChanged).
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // WinUI removes the handlers of collected bindings on its own schedule: flaky here
	public async Task When_XBind_OneWay_Elements_Collected_Then_PropertyChanged_Handlers_Removed()
	{
		var vm = new CountingViewModel();
		var baseline = vm.SubscriberCount;

		var refs = await CreateXBoundAsync(vm);
		Assert.AreEqual(baseline + 2 * Count, vm.SubscriberCount, "Each x:Bind should subscribe once.");

		await CollectAsync(refs, () => vm.SubscriberCount == baseline);
		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The x:Bind controls were not collected.");
		Assert.AreEqual(baseline, vm.SubscriberCount, "PropertyChanged handlers of collected x:Bind bindings are still on the source.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<List<WeakReference>> CreateBoundAsync(CountingViewModel vm, string path, bool loaded)
	{
		var refs = new List<WeakReference>();
		var panel = new StackPanel();
		for (var i = 0; i < Count; i++)
		{
			var element = new TextBlock { DataContext = vm };
			element.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath(path) });
			Assert.AreEqual("first", element.Text);
			panel.Children.Add(element);
			refs.Add(new WeakReference(element));
		}

		await ShowAndRemoveAsync(panel, loaded);
		return refs;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<List<WeakReference>> CreateBoundToDependencyObjectAsync(DependencyObject source, bool loaded)
	{
		var refs = new List<WeakReference>();
		var panel = new StackPanel();
		for (var i = 0; i < Count; i++)
		{
			var element = new TextBlock();
			element.SetBinding(TextBlock.TextProperty, new Binding { Source = source, Path = new PropertyPath(nameof(FrameworkElement.Tag)) });
			Assert.AreEqual("tag", element.Text);
			panel.Children.Add(element);
			refs.Add(new WeakReference(element));
		}

		await ShowAndRemoveAsync(panel, loaded);
		return refs;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task<List<WeakReference>> CreateXBoundAsync(CountingViewModel vm)
	{
		var refs = new List<WeakReference>();
		var panel = new StackPanel();
		var controls = new List<XBindOneWayLeakControl>();
		for (var i = 0; i < Count; i++)
		{
			var control = new XBindOneWayLeakControl(vm);
			panel.Children.Add(control);
			controls.Add(control);
			refs.Add(new WeakReference(control));
		}

		await ShowAndRemoveAsync(panel, loaded: true);
		Assert.IsTrue(controls.All(c => c.ShownName == vm.Name), "The x:Bind bindings were not applied.");
		return refs;
	}

	private static async Task ShowAndRemoveAsync(Panel panel, bool loaded)
	{
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
	}

	private static int CallbackCount(DependencyObject source, DependencyProperty property)
	{
#if HAS_UNO
		var details = ((IDependencyObjectStoreProvider)source).Store.GetPropertyDetails(property);
		var manager = typeof(DependencyPropertyDetails)
			.GetField("_callbackManager", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(details);
		if (manager is null)
		{
			return 0;
		}

		var callbacks = (ICollection)manager.GetType()
			.GetField("_callbacks", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(manager)!;
		return callbacks.Count;
#else
		throw new NotSupportedException("The property-changed callbacks are read from Uno internals.");
#endif
	}

	public sealed class CountingViewModel : INotifyPropertyChanged
	{
		private readonly List<PropertyChangedEventHandler> _handlers = new();
		private string _name = "first";

		public int SubscriberCount => _handlers.Count;

		private CountingCollection _items = new() { "first" };

		public CountingCollection Items
		{
			get => _items;
			set
			{
				_items = value;
				var args = new PropertyChangedEventArgs(nameof(Items));
				foreach (var handler in _handlers.ToArray())
				{
					handler(this, args);
				}
			}
		}

		public LiveCollection LiveItems { get; } = new();

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

	// Counts the subscribers of both events an indexer binding subscribes to.
	public sealed class CountingCollection : ObservableCollection<string>
	{
		public int CollectionChangedCount { get; private set; }

		public int PropertyChangedCount { get; private set; }

		public override event NotifyCollectionChangedEventHandler? CollectionChanged
		{
			add
			{
				CollectionChangedCount++;
				base.CollectionChanged += value;
			}
			remove
			{
				CollectionChangedCount--;
				base.CollectionChanged -= value;
			}
		}

		protected override event PropertyChangedEventHandler? PropertyChanged
		{
			add
			{
				PropertyChangedCount++;
				base.PropertyChanged += value;
			}
			remove
			{
				PropertyChangedCount--;
				base.PropertyChanged -= value;
			}
		}
	}

	// Raises over its live handler list, like a source with a custom add/remove: removing a handler mid-raise throws.
	public sealed class LiveCollection : INotifyCollectionChanged, IEnumerable<string>
	{
		private readonly List<NotifyCollectionChangedEventHandler> _handlers = new();
		private readonly List<string> _items = new() { "first" };

		public int CollectionChangedCount => _handlers.Count;

		public event NotifyCollectionChangedEventHandler? CollectionChanged
		{
			add => _handlers.Add(value!);
			remove => _handlers.Remove(value!);
		}

		public string this[int index]
		{
			get => _items[index];
			set
			{
				var oldItem = _items[index];
				_items[index] = value;
				var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, oldItem, index);
				foreach (var handler in _handlers) // throws if the list changes during the raise
				{
					handler(this, args);
				}
			}
		}

		public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
