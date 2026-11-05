#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater;

// An items source that outlives a repeater must not keep it alive, even if the repeater never loads (WinUI holds none).
[TestClass]
public class Given_ItemsRepeater_Leak
{
	private const int Count = 5;

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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

#if HAS_UNO // DataTemplate(Func<>) is Uno-only
	[TestMethod]
	[RunsOnUIThread]
	[DataRow(false)]
	[DataRow(true)]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
#endif

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
#if RUNTIME_NATIVE_AOT
	[Ignore("NativeAOT GC behavior may differ for leak detection tests")]
#endif
	public async Task When_Repeater_Collected_Then_Raising_CollectionChanged_Is_Safe()
	{
		var source = new CountingCollection { "a", "b" };
		var refs = CreateNeverLoaded(() => source);
		await WindowHelper.WaitForIdle(); // lets go of work the repeaters queued when they were created

		// Don't yield to the dispatcher here: it would remove the subscriptions of the collected repeaters before the raise.
		for (var i = 0; i < 10 && refs.Any(r => r.IsAlive); i++)
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
		}
		Assert.AreEqual(0, refs.Count(r => r.IsAlive), "The repeaters were not collected.");

		source.Add("c"); // raises to the handlers of collected repeaters
		await WindowHelper.WaitForIdle();
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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

	private static int CountRealized(ItemsRepeater repeater)
		=> Enumerable.Range(0, Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(repeater))
			.Select(i => Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(repeater, i))
			.OfType<UIElement>()
			.Count(e => e.Visibility == Visibility.Visible && repeater.GetElementIndex(e) >= 0);

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

#if HAS_UNO
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateScreenWithFlyout(IEnumerable source, bool show)
	{
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
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool IsLoaded(WeakReference reference) => reference.Target is FrameworkElement { IsLoaded: true };
#endif

	// Collects at least 3 times, pumping the dispatcher in between so the removals posted by finalizers run.
	private static async Task CollectAsync(List<WeakReference> refs, Func<bool>? until = null)
	{
		until ??= () => !refs.Any(r => r.IsAlive);
		var sw = Stopwatch.StartNew();
		var timeout = TimeSpan.FromSeconds(10);
		var minIterations = 3;

		while (minIterations-- > 0 || (!until() && sw.Elapsed < timeout))
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
