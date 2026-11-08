#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Collections;
using static Private.Infrastructure.TestServices;
using static Uno.UI.RuntimeTests.Helpers.LeakTestHelper;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater;

// An items source that outlives a repeater must not keep it alive, even if the repeater never loads (WinUI holds none).
[TestClass]
public class Given_ItemsRepeater_Leak
{
	private const int Count = 5;

	[TestMethod]
	[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_Never_Loaded_Then_Not_Rooted_By_ItemsSource()
	{
		var source = new ObservableCollection<string> { "a", "b" }; // outlives the repeaters
		var refs = CreateNeverLoaded(() => source);

		await CollectAsync(refs);

		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "A never-loaded ItemsRepeater is still rooted by its ItemsSource.");
		GC.KeepAlive(source);
	}

	[TestMethod]
	[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_Never_Loaded_Then_Not_Rooted_By_ItemsSourceView()
	{
		var view = new ItemsSourceView(new ObservableCollection<string> { "a", "b" }); // outlives the repeaters
		var refs = CreateNeverLoaded(() => view);

		await CollectAsync(refs);

		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "A never-loaded ItemsRepeater is still rooted by its ItemsSourceView.");
		GC.KeepAlive(view);
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // The screen is built with DataTemplate(Func<>), which is Uno-only
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_Screen_With_Unopened_Flyout_Dropped_Then_Not_Rooted_By_ItemsSource(bool shown)
	{
		var source = new ObservableCollection<string> { "a", "b" };
		var screenRef = CreateScreenWithFlyout(source, shown);
		try
		{
			if (shown)
			{
				for (var i = 0; i < 20 && !IsLoaded(screenRef); i++)
				{
					await WindowHelper.WaitForIdle();
				}
				Assert.IsTrue(IsLoaded(screenRef), "The screen was never loaded.");

				WindowHelper.WindowContent = null;
				await WindowHelper.WaitForIdle();
			}

			var refs = new List<WeakReference> { screenRef };
			await CollectAsync(refs);

			Assert.IsFalse(screenRef.IsAlive, "The screen is still rooted by the items source of the repeater in its flyout.");
			GC.KeepAlive(source);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_Repeater_Collected_Then_Subscription_Removed_From_Source()
	{
		var source = new CountingCollection { "a", "b" };
		var refs = CreateNeverLoaded(() => source);
		Assert.AreEqual(Count, source.SubscriberCount);

		await CollectAsync(refs, () => source.SubscriberCount == 0);

		Assert.AreEqual(0, source.SubscriberCount, "Subscriptions of collected repeaters are still on the items source.");
	}

	[TestMethod]
	[RunsOnUIThread]
	// 6.x: on the Mono interpreter (WASM, iOS), stale stack slots can keep a repeater alive: 1 of 5 alive on CI now and then.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaUIKit)]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_Repeater_Collected_Then_Raising_CollectionChanged_Is_Safe_And_Subscription_Removed()
	{
		var source = new CountingCollection { "a", "b" };
		var refs = CreateNeverLoaded(() => source);
		await WindowHelper.WaitForIdle(); // lets go of work the repeaters queued when they were created

		// Don't yield to the dispatcher here: it would remove the subscriptions of the collected repeaters before the raise.
		CollectWithoutDispatcher(refs);
		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The repeaters were not collected.");

		source.Add("c"); // raises to the handlers of collected repeaters
		await CollectAsync(refs, () => source.SubscriberCount == 0);

		Assert.AreEqual(0, source.SubscriberCount, "A raise to collected repeaters kept their subscriptions on the items source.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // On WinUI, the added item isn't realized yet when the test checks it
	public async Task When_Loaded_Then_Source_Changes_Are_Applied()
	{
		var source = new ObservableCollection<string> { "a", "b" };
		var sut = new ItemsRepeater { ItemsSource = source };

		try
		{
			WindowHelper.WindowContent = new Border { Width = 200, Height = 400, Child = sut };
			await WindowHelper.WaitForLoaded(sut);
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(2, CountRealized(sut));

			source.Add("c");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(3, CountRealized(sut), "An item added to the source was not realized.");

			// Unloaded and loaded again: the repeater must still follow the source.
			var host = (Border)WindowHelper.WindowContent;
			host.Child = null;
			await WindowHelper.WaitForIdle();
			source.Add("d");
			host.Child = sut;
			await WindowHelper.WaitForLoaded(sut);
			await WindowHelper.WaitForIdle();
			source.Add("e");
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(5, CountRealized(sut), "The repeater stopped following its source after an unload.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// Each kind of observable source has its own subscription: all of them must go away with a view nobody references,
	// like the throwaway views NavigationView creates to inspect its items.
	[TestMethod]
	[RunsOnUIThread]
	[DataRow(SourceKind.CollectionChanged)]
	[DataRow(SourceKind.ObservableVector)]
#if HAS_UNO
	[DataRow(SourceKind.UntypedObservableVector)]
#endif
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_ItemsSourceView_Dropped_Then_Subscription_Removed_From_Source(SourceKind kind)
	{
		var (source, subscriberCount) = CreateCountingSource(kind);
		CreateItemsSourceViews(source);
		Assert.AreEqual(Count, subscriberCount(), "Each view should subscribe to the source once.");

		await CollectAsync(new List<WeakReference>(), () => subscriberCount() == 0);

		Assert.AreEqual(0, subscriberCount(), "Subscriptions of collected views are still on the source.");
		GC.KeepAlive(source);
	}

	// As on WinUI, the source doesn't keep a view alive, even one with a subscriber: once nothing else references the
	// view, it is collected and its subscriber stops receiving the source's changes.
	[TestMethod]
	[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_Subscribed_ItemsSourceView_Dropped_Then_Collected_And_Subscriber_No_Longer_Notified()
	{
		var source = new CountingCollection { "a", "b" };
		var baseline = source.SubscriberCount;
		var notified = 0;
		var viewRef = CreateSubscribedItemsSourceView(source, () => notified++);
		Assert.AreEqual(baseline + 1, source.SubscriberCount, "The view should subscribe to the source once.");

		await CollectAsync([viewRef], () => !viewRef.IsAlive && source.SubscriberCount == baseline);

		Assert.IsFalse(viewRef.IsAlive, "A view with a subscriber is kept alive by its source.");
		Assert.AreEqual(baseline, source.SubscriberCount, "The subscription of the collected view is still on the source.");

		source.Add("c");
		Assert.AreEqual(0, notified, "The subscriber of a collected view was notified of a change of the source.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateSubscribedItemsSourceView(CountingCollection source, Action onChanged)
	{
		var view = new ItemsSourceView(source);
		view.CollectionChanged += (_, _) => onChanged();
		return new WeakReference(view);
	}

	public enum SourceKind
	{
		CollectionChanged,
		ObservableVector,
		UntypedObservableVector,
	}

	private static (object source, Func<int> subscriberCount) CreateCountingSource(SourceKind kind)
	{
		switch (kind)
		{
			case SourceKind.CollectionChanged:
				var collection = new CountingCollection { "a", "b" };
				return (collection, () => collection.SubscriberCount);
			case SourceKind.ObservableVector:
				var vector = new CountingObservableVector { "a", "b" };
				return (vector, () => vector.SubscriberCount);
#if HAS_UNO
			case SourceKind.UntypedObservableVector:
				var untypedVector = new CountingUntypedObservableVector { "a", "b" };
				return (untypedVector, () => untypedVector.SubscriberCount);
#endif
			default:
				throw new ArgumentOutOfRangeException(nameof(kind));
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void CreateItemsSourceViews(object source)
	{
		for (var i = 0; i < Count; i++)
		{
			_ = new ItemsSourceView(source);
		}
	}

	// The common case: a page and its view model become unreachable together. The items source must not wait for the
	// removal of the subscription of a view that died with it, queued to the dispatcher, before it can be collected.
	[TestMethod]
	// 6.x only: the owner's DependencyObjectStore has a finalizer (master removed it), so the store keeps what it references
	// alive until finalizers run. Single-threaded WASM runs them only from the event loop, and the mobile runtimes fail too.
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaMobile)]
	[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_Repeater_And_Source_Dropped_Together_Then_Source_Collected_Without_Dispatcher()
	{
		var (repeaterRef, sourceRef) = CreateNeverLoadedWithOwnSource();
		await WindowHelper.WaitForIdle(); // lets go of work the repeater queued when it was created

		// Don't yield to the dispatcher here: the source must not depend on queued work to be collected.
		CollectWithoutDispatcher([sourceRef]);

		if (repeaterRef.IsAlive)
		{
			Assert.Inconclusive("The repeater was not collected without yielding on this platform.");
		}

		Assert.IsFalse(sourceRef.IsAlive, "The items source is kept alive by the cleanup of a view that died with it.");
	}

	// A view that outlives its repeaters, e.g. one a view model exposes, and rarely raises: every rebuilt page must not
	// leave a handler on it.
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)] // Counts the handlers of Uno's ItemsSourceView
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_Repeaters_Collected_Then_Handlers_Removed_From_Shared_ItemsSourceView_Without_Raise()
	{
		var view = new ItemsSourceView(new ObservableCollection<string> { "a", "b" });
		var baseline = CountHandlers(view);
		var refs = CreateNeverLoaded(() => view);
		Assert.AreEqual(baseline + Count, CountHandlers(view));

		await CollectAsync(refs, () => CountHandlers(view) == baseline);

		Assert.AreEqual(baseline, CountHandlers(view), "Handlers of collected repeaters are still on the shared ItemsSourceView.");
	}

	private static int CountHandlers(ItemsSourceView view)
#if HAS_UNO
		=> (typeof(ItemsSourceView).GetField(nameof(ItemsSourceView.CollectionChanged), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(view) as Delegate)?.GetInvocationList().Length ?? 0;
#else
		=> throw new NotSupportedException("WinUI's ItemsSourceView doesn't expose its handlers.");
#endif

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (WeakReference repeater, WeakReference source) CreateNeverLoadedWithOwnSource()
	{
		var source = new ObservableCollection<string> { "a", "b" };
		var repeater = new ItemsRepeater { ItemsSource = source };

		// Tracks resurrection: a short weak reference is cleared even while finalization still keeps the source alive.
		return (new WeakReference(repeater), new WeakReference(source, trackResurrection: true));
	}

	private static int CountRealized(ItemsRepeater repeater)
		=> Enumerable.Range(0, Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(repeater))
			.Select(i => Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(repeater, i))
			.OfType<UIElement>()
			.Count(e => e.Visibility == Visibility.Visible && repeater.GetElementIndex(e) >= 0);

	// A repeater that never loads never unloads to detach from its transition provider: a provider that outlives it,
	// e.g. one shared through a resource, must not keep it alive either.
	[TestMethod]
	[RunsOnUIThread]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_Never_Loaded_Then_Not_Rooted_By_Shared_ItemTransitionProvider()
	{
		var provider = new TestTransitionProvider(); // outlives the repeaters
		var refs = CreateNeverLoadedWithTransitionProvider(provider);

		await CollectAsync(refs);

		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "A never-loaded ItemsRepeater is still rooted by its ItemTransitionProvider.");
		GC.KeepAlive(provider);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static List<WeakReference> CreateNeverLoadedWithTransitionProvider(ItemCollectionTransitionProvider provider)
	{
		var refs = new List<WeakReference>();
		for (var i = 0; i < Count; i++)
		{
			refs.Add(new WeakReference(new ItemsRepeater { ItemsSource = new[] { "a", "b" }, ItemTransitionProvider = provider }));
		}

		return refs;
	}

	private sealed class TestTransitionProvider : ItemCollectionTransitionProvider
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static List<WeakReference> CreateNeverLoaded(Func<object> source)
	{
		var refs = new List<WeakReference>();
		for (var i = 0; i < Count; i++)
		{
			refs.Add(new WeakReference(new ItemsRepeater { ItemsSource = source() }));
		}

		return refs;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateScreenWithFlyout(IEnumerable source, bool show)
	{
#if HAS_UNO
		var screen = new Grid();

		// Like the code generated for x:Bind in a page, the item template references the page that declares it.
		var repeater = new ItemsRepeater
		{
			ItemsSource = source,
			ItemTemplate = new DataTemplate(() => new TextBlock { Tag = screen }),
		};
		screen.Children.Add(new Button
		{
			Content = "Views",
			Flyout = new Flyout { Content = repeater },
		});

		if (show)
		{
			WindowHelper.WindowContent = screen;
		}

		return new WeakReference(screen);
#else
		throw new NotSupportedException("DataTemplate(Func<>) is Uno-only.");
#endif
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool IsLoaded(WeakReference reference) => reference.Target is FrameworkElement { IsLoaded: true };

	private sealed class CountingObservableVector : List<object>, IObservableVector<object>
	{
		private readonly List<VectorChangedEventHandler<object>> _handlers = new();

		public int SubscriberCount => _handlers.Count;

		public event VectorChangedEventHandler<object>? VectorChanged
		{
			add => _handlers.Add(value!);
			remove => _handlers.Remove(value!);
		}
	}

#if HAS_UNO
	private sealed class CountingUntypedObservableVector : List<object>, IObservableVector
	{
		private readonly List<VectorChangedEventHandler> _handlers = new();

		public int SubscriberCount => _handlers.Count;

		public event VectorChangedEventHandler? UntypedVectorChanged
		{
			add => _handlers.Add(value!);
			remove => _handlers.Remove(value!);
		}
	}
#endif

	// Raises over its live handler list: removing a handler mid-raise would throw.
	private sealed class CountingCollection : IList, INotifyCollectionChanged
	{
		private readonly List<object?> _items = new();
		private readonly List<NotifyCollectionChangedEventHandler> _handlers = new();

		public int SubscriberCount => _handlers.Count;

		public event NotifyCollectionChangedEventHandler? CollectionChanged
		{
			add => _handlers.Add(value!);
			remove => _handlers.Remove(value!);
		}

		public int Add(object? value)
		{
			_items.Add(value);
			var index = _items.Count - 1;
			var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, index);
			foreach (var handler in _handlers) // throws if the list changes during the raise
			{
				handler(this, args);
			}

			return index;
		}

		public object? this[int index] { get => _items[index]; set => throw new NotSupportedException(); }
		public bool IsFixedSize => false;
		public bool IsReadOnly => false;
		public int Count => _items.Count;
		public bool IsSynchronized => false;
		public object SyncRoot => this;
		public void Clear() => throw new NotSupportedException();
		public bool Contains(object? value) => _items.Contains(value);
		public int IndexOf(object? value) => _items.IndexOf(value);
		public void Insert(int index, object? value) => throw new NotSupportedException();
		public void Remove(object? value) => throw new NotSupportedException();
		public void RemoveAt(int index) => throw new NotSupportedException();
		public void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);
		public IEnumerator GetEnumerator() => _items.GetEnumerator();
	}
}
