using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

// Expected behaviours come from WinUI 2.5.1 ListViewBase_Partial_DataVirtualization.cpp and the public
// ISupportIncrementalLoading / IncrementalLoadingTrigger / IncrementalLoadingThreshold / DataFetchSize docs.
[TestClass]
[RunsOnUIThread]
public class Given_ListView_IncrementalLoading
{
	private const double Tolerance = 1.0;

	// Completes every request through the dispatcher (like a real async source), or on demand when Manual is set.
	private sealed class IncrementalSource : ObservableCollection<string>, ISupportIncrementalLoading
	{
		private readonly DispatcherQueue _queue = DispatcherQueue.GetForCurrentThread();
		private readonly Queue<Action> _pending = new();

		public IncrementalSource(int initial, int total, string prefix = "Item ")
		{
			Total = total;
			Prefix = prefix;
			for (int i = 0; i < initial; i++)
			{
				Add($"{prefix}{i}");
			}
		}

		public int Total { get; }
		public string Prefix { get; }
		public bool Manual { get; set; }
		public int FailCount { get; set; }
		public int InFlight { get; private set; }
		public int MaxInFlight { get; private set; }
		public List<uint> Requests { get; } = new();
		public int PendingCount => _pending.Count;

		public bool HasMoreItems => Count < Total;

		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
		{
			Requests.Add(count);
			InFlight++;
			MaxInFlight = Math.Max(MaxInFlight, InFlight);

			TaskCompletionSource<LoadMoreItemsResult> tcs = new();
			void Finish()
			{
				InFlight--;
				if (FailCount > 0)
				{
					FailCount--;
					tcs.SetException(new InvalidOperationException("Simulated failure"));
					return;
				}

				int toAdd = Math.Min((int)count, Total - Count);
				for (int i = 0; i < toAdd; i++)
				{
					Add($"{Prefix}{Count}");
				}

				tcs.SetResult(new LoadMoreItemsResult { Count = (uint)toAdd });
			}

			if (Manual)
			{
				_pending.Enqueue(Finish);
			}
			else
			{
				_queue.TryEnqueue(Finish);
			}

			return AsyncInfo.Run(_ => tcs.Task);
		}

		public void CompleteNext() => _pending.Dequeue()();
	}

	[TestCleanup]
	public void Cleanup() => WindowHelper.WindowContent = null;

	private static ListViewBase CreateList(bool grid, IncrementalSource source, IncrementalLoadingTrigger trigger = IncrementalLoadingTrigger.Edge)
	{
		ListViewBase list = grid ? new GridView() : new ListView();
		list.Width = 400;
		list.Height = 320;
		list.IncrementalLoadingTrigger = trigger;
		list.ItemTemplate = (DataTemplate)XamlReader.Load(
			"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Width='80' Height='40'><TextBlock Text='{Binding}' /></Border></DataTemplate>");
		list.ItemsSource = source;
		return list;
	}

	private static async Task<ListViewBase> Load(ListViewBase list)
	{
		WindowHelper.WindowContent = list;
		await WindowHelper.WaitForLoaded(list);
		await WindowHelper.WaitForIdle();
		return list;
	}

	private static ScrollViewer GetScrollViewer(ListViewBase list) => (ScrollViewer)list.GetTemplateChild("ScrollViewer");

	private static int LastVisible(ListViewBase list) => list.ItemsPanelRoot switch
	{
		ItemsStackPanel p => p.LastVisibleIndex,
		ItemsWrapGrid g => g.LastVisibleIndex,
		_ => throw new InvalidOperationException("Unexpected items panel"),
	};

	// Waits until neither the request count nor the item count moves for a few idle ticks.
	private static async Task WaitUntilQuiet(IncrementalSource source)
	{
		var last = (source.Requests.Count, source.Count);
		int stable = 0;
		for (int i = 0; i < 100 && stable < 5; i++)
		{
			await WindowHelper.WaitForIdle();
			var now = (source.Requests.Count, source.Count);
			stable = now == last ? stable + 1 : 0;
			last = now;
		}
	}

	private static async Task ScrollTo(ListViewBase list, double offset)
	{
		var sv = GetScrollViewer(list);
		sv.ChangeView(null, offset, null, true);
		await WindowHelper.WaitFor(() => Math.Abs(sv.VerticalOffset - offset) < Tolerance, timeoutMS: 3000, message: $"Scroll to {offset}");
		await WindowHelper.WaitForIdle();
	}

	[TestMethod]
	public void When_Created_Then_Defaults_Match_WinUI()
	{
		ListView list = new();

		Assert.AreEqual(IncrementalLoadingTrigger.Edge, list.IncrementalLoadingTrigger);
		Assert.AreEqual(0.0, list.IncrementalLoadingThreshold);
		Assert.AreEqual(3.0, list.DataFetchSize);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Edge_Trigger_And_Viewport_Not_Filled_Then_Loads_Until_Filled(bool grid)
	{
		IncrementalSource source = new(3, 1000);
		var list = await Load(CreateList(grid, source));

		await WindowHelper.WaitFor(() => source.Count > 3, timeoutMS: 3000, message: "Items were loaded");
		await WaitUntilQuiet(source);

		Assert.IsGreaterThan(0, source.Requests.Count);
		Assert.IsLessThan(source.Total, source.Count, "Should stop once the viewport is filled past the threshold");
		Assert.IsGreaterThan(LastVisible(list), source.Count - 1, "More items than the viewport shows");
		Assert.AreEqual(source.Count, list.Items.Count);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Trigger_None_Then_Nothing_Loads_Automatically(bool grid)
	{
		IncrementalSource source = new(100, 1000);
		var list = await Load(CreateList(grid, source, IncrementalLoadingTrigger.None));
		var sv = GetScrollViewer(list);

		await ScrollTo(list, sv.ScrollableHeight);
		await WaitUntilQuiet(source);

		Assert.AreEqual(0, source.Requests.Count);
		Assert.AreEqual(100, source.Count);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Edge_Trigger_And_Far_From_End_Then_Does_Not_Load(bool grid)
	{
		IncrementalSource source = new(100, 1000);
		await Load(CreateList(grid, source));

		await WaitUntilQuiet(source);

		Assert.AreEqual(0, source.Requests.Count);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_LoadMoreItemsAsync_Called_Then_Items_Are_Added_And_Count_Returned(bool grid)
	{
		IncrementalSource source = new(100, 1000);
		var list = await Load(CreateList(grid, source, IncrementalLoadingTrigger.None));

		var result = await list.LoadMoreItemsAsync().AsTask();

		Assert.AreEqual(1, source.Requests.Count);
		Assert.IsGreaterThan(0u, result.Count);
		Assert.AreEqual(100 + (int)result.Count, source.Count);
		Assert.AreEqual(source.Count, list.Items.Count);
	}

	[TestMethod]
	[DataRow(1.0)]
	[DataRow(3.0)]
	[DataRow(5.0)]
	public async Task When_DataFetchSize_Set_Then_Request_Is_That_Many_Pages(double pages)
	{
		IncrementalSource source = new(100, 10000);
		var list = CreateList(false, source, IncrementalLoadingTrigger.None);
		list.DataFetchSize = pages;
		await Load(list);
		var sv = GetScrollViewer(list);
		var container = (FrameworkElement)list.ContainerFromIndex(0);
		double itemsPerPage = sv.ViewportHeight / container.ActualHeight;

		await list.LoadMoreItemsAsync().AsTask();

		Assert.AreEqual(1, source.Requests.Count);
		Assert.AreEqual(pages * itemsPerPage, source.Requests[0], pages * 1.5, $"Requested {source.Requests[0]} items for {itemsPerPage} items per page");
	}

	[TestMethod]
	[DataRow(0.0, false)]
	[DataRow(1.0, true)]
	public async Task When_Within_Threshold_Pages_Of_End_Then_Loads(double threshold, bool expectLoad)
	{
		// 12 items remain below the viewport: 1.5 pages. WinUI compares floor(remaining / itemsPerPage) to the threshold.
		IncrementalSource source = new(100, 1000);
		var list = CreateList(false, source);
		list.IncrementalLoadingThreshold = threshold;
		await Load(list);
		var sv = GetScrollViewer(list);
		double size = ((FrameworkElement)list.ContainerFromIndex(0)).ActualHeight;
		Assert.AreEqual(0, source.Requests.Count, "Nothing to load while far from the end");

		await ScrollTo(list, 88 * size - sv.ViewportHeight);

		if (expectLoad)
		{
			await WindowHelper.WaitFor(() => source.Requests.Count > 0, timeoutMS: 3000, message: "Load requested inside the threshold");
		}
		else
		{
			await WaitUntilQuiet(source);
			Assert.AreEqual(0, source.Requests.Count, "Outside the threshold nothing should load");
		}
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_HasMoreItems_False_Then_Loading_Stops(bool grid)
	{
		IncrementalSource source = new(10, 30);
		var list = await Load(CreateList(grid, source));

		await WindowHelper.WaitFor(() => source.Count == 30, timeoutMS: 3000, message: "Source exhausted");
		await WaitUntilQuiet(source);
		int requests = source.Requests.Count;

		var sv = GetScrollViewer(list);
		await ScrollTo(list, sv.ScrollableHeight);
		await WaitUntilQuiet(source);
		var manual = await list.LoadMoreItemsAsync().AsTask();

		Assert.AreEqual(requests, source.Requests.Count, "No request once HasMoreItems is false");
		Assert.AreEqual(0u, manual.Count);
		Assert.AreEqual(30, list.Items.Count);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Load_In_Flight_Then_No_Overlapping_Request(bool grid)
	{
		IncrementalSource source = new(3, 1000) { Manual = true };
		var list = await Load(CreateList(grid, source));
		await WindowHelper.WaitFor(() => source.Requests.Count == 1, timeoutMS: 3000, message: "First request issued");

		// Layout passes and scrolling must not start a second request while the first one is running.
		for (int i = 0; i < 10; i++)
		{
			list.InvalidateMeasure();
			await WindowHelper.WaitForIdle();
		}

		Assert.AreEqual(1, source.Requests.Count);
		Assert.AreEqual(1, source.MaxInFlight);

		source.CompleteNext();
		await WindowHelper.WaitFor(() => list.Items.Count > 3, timeoutMS: 3000, message: "Items from the completed request");
		Assert.AreEqual(0, source.InFlight);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Load_Completes_And_Scrolled_To_New_End_Then_Loads_Again(bool grid)
	{
		IncrementalSource source = new(100, 10000);
		var list = await Load(CreateList(grid, source));
		var sv = GetScrollViewer(list);
		Assert.AreEqual(0, source.Requests.Count);

		await ScrollTo(list, sv.ScrollableHeight);
		await WindowHelper.WaitFor(() => source.Requests.Count == 1 && source.Count > 100, timeoutMS: 3000, message: "First page loaded");
		await WaitUntilQuiet(source);
		Assert.AreEqual(1, source.Requests.Count, "Should not keep loading while far from the new end");
		int afterFirst = source.Count;

		await WindowHelper.WaitFor(() => sv.ScrollableHeight > sv.VerticalOffset + Tolerance, timeoutMS: 3000, message: "Extent grew");
		await ScrollTo(list, sv.ScrollableHeight);
		await WindowHelper.WaitFor(() => source.Requests.Count == 2 && source.Count > afterFirst, timeoutMS: 3000, message: "Second page loaded");
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_ItemsSource_Swapped_During_Load_Then_New_Source_Loads_After_It_Completes(bool grid)
	{
		IncrementalSource first = new(3, 1000, "First ") { Manual = true };
		IncrementalSource second = new(3, 1000, "Second ");
		var list = await Load(CreateList(grid, first));
		await WindowHelper.WaitFor(() => first.Requests.Count == 1, timeoutMS: 3000, message: "First source request issued");

		list.ItemsSource = second;
		await WindowHelper.WaitForIdle();
		Assert.AreEqual(second.Count, list.Items.Count);

		// ListViewBase has no cancel on source change: the in-flight flag clears when the old request completes.
		first.CompleteNext();
		await WindowHelper.WaitFor(() => second.Count > 3, timeoutMS: 3000, message: "Second source loaded");
		await WaitUntilQuiet(second);

		Assert.AreEqual(1, first.Requests.Count, "Old source must not be asked for more");
		Assert.AreEqual(second.Count, list.Items.Count);
		Assert.IsTrue(list.Items.Cast<string>().All(i => i.StartsWith("Second ")));
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task When_Load_Fails_Then_Later_Loads_Still_Work(bool grid)
	{
		IncrementalSource source = new(3, 1000) { FailCount = 1 };
		var list = await Load(CreateList(grid, source));

		await WindowHelper.WaitFor(() => source.Requests.Count >= 2 && source.Count > 3, timeoutMS: 3000, message: "Second request succeeded after the first failed");

		Assert.AreEqual(0, source.FailCount);
		Assert.AreEqual(source.Count, list.Items.Count);
	}

	[TestMethod]
	public async Task When_Manual_Load_Fails_Then_Next_Manual_Load_Works()
	{
		IncrementalSource source = new(100, 1000) { FailCount = 1 };
		var list = await Load(CreateList(false, source, IncrementalLoadingTrigger.None));

		var failing = list.LoadMoreItemsAsync().AsTask();
		await WindowHelper.WaitFor(() => failing.IsCompleted, timeoutMS: 3000, message: "Failing request completes");
		_ = failing.Exception;

		var result = await list.LoadMoreItemsAsync().AsTask();

		Assert.AreEqual(2, source.Requests.Count);
		Assert.IsGreaterThan(0u, result.Count);
		Assert.AreEqual(100 + (int)result.Count, source.Count);
	}
}
