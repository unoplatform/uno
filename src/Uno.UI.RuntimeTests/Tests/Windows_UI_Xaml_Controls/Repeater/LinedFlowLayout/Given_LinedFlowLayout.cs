#nullable enable

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

#if HAS_UNO
using Microsoft.UI.Private.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public class Given_LinedFlowLayout
{
	[TestMethod]
	public async Task When_HostedInItemsRepeater_Then_RealizesExpectedLineGeometry()
	{
		using var template = new DynamicDataTemplate(() => new Border
		{
			Width = 100,
			Height = 50,
			Background = new SolidColorBrush(Microsoft.UI.Colors.SkyBlue),
		});
		var layout = new LinedFlowLayout
		{
			LineHeight = 50,
			LineSpacing = 4,
			MinItemSpacing = 4,
		};
		var sut = new ItemsRepeater
		{
			Width = 400,
			ItemsSource = Enumerable.Range(0, 20).ToArray(),
			ItemTemplate = template.Value,
			Layout = layout,
		};

		try
		{
			await UITestHelper.Load(sut);
			await UITestHelper.WaitForIdle();

			var first = GetRealizedElement(sut, 0);
			var second = GetRealizedElement(sut, 1);
			var third = GetRealizedElement(sut, 2);
			var fourth = GetRealizedElement(sut, 3);

			sut.ActualHeight.Should().BeGreaterThan(50);
			first.ActualWidth.Should().BeApproximately(100, 1);
			first.ActualHeight.Should().BeApproximately(50, 1);
			((double)second.ActualOffset.Y).Should().BeApproximately(first.ActualOffset.Y, 1);
			((double)third.ActualOffset.Y).Should().BeApproximately(first.ActualOffset.Y, 1);
			((double)fourth.ActualOffset.X).Should().BeApproximately(first.ActualOffset.X, 1);
			((double)fourth.ActualOffset.Y).Should().BeApproximately(first.ActualOffset.Y + 54, 1);
			LayoutsTestHooks.GetLayoutFirstRealizedItemIndex(layout).Should().Be(0);
			LayoutsTestHooks.GetLayoutLastRealizedItemIndex(layout).Should().BeGreaterThanOrEqualTo(3);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Constructed_Then_DefaultsMatchWinUI()
	{
		var sut = new LinedFlowLayout();

		double.IsNaN(sut.LineHeight).Should().BeTrue();
		sut.ActualLineHeight.Should().Be(0.0);
		sut.LineSpacing.Should().Be(0.0);
		sut.MinItemSpacing.Should().Be(0.0);
		sut.ItemsJustification.Should().Be(LinedFlowLayoutItemsJustification.Start);
		sut.ItemsStretch.Should().Be(LinedFlowLayoutItemsStretch.None);
	}

	[TestMethod]
	public void When_PropertiesSet_Then_RoundTrip()
	{
		var sut = new LinedFlowLayout
		{
			LineHeight = 48,
			LineSpacing = 4,
			MinItemSpacing = 6,
			ItemsJustification = LinedFlowLayoutItemsJustification.SpaceEvenly,
			ItemsStretch = LinedFlowLayoutItemsStretch.Fill,
		};

		sut.LineHeight.Should().Be(48);
		sut.LineSpacing.Should().Be(4);
		sut.MinItemSpacing.Should().Be(6);
		sut.ItemsJustification.Should().Be(LinedFlowLayoutItemsJustification.SpaceEvenly);
		sut.ItemsStretch.Should().Be(LinedFlowLayoutItemsStretch.Fill);
	}

	[TestMethod]
	public void When_LayoutsTestHooksSetForcedWrapMultiplier_Then_RoundTripsAndInvalidates()
	{
		var sut = new LinedFlowLayout();
		object? invalidatedSender = null;
		LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs? invalidatedArgs = null;
		LayoutsTestHooks.LinedFlowLayoutInvalidated += OnInvalidated;

		try
		{
			LayoutsTestHooks.SetLinedFlowLayoutForcedWrapMultiplier(sut, 3.0);

			LayoutsTestHooks.GetLinedFlowLayoutForcedWrapMultiplier(sut).Should().Be(3.0);
			invalidatedSender.Should().BeSameAs(sut);
			invalidatedArgs.Should().NotBeNull();
			invalidatedArgs!.InvalidationTrigger.Should().Be(LinedFlowLayoutInvalidationTrigger.InvalidateLayoutCall);
			LayoutsTestHooks.GetLayoutFirstRealizedItemIndex(sut).Should().Be(-1);
			LayoutsTestHooks.GetLayoutLastRealizedItemIndex(sut).Should().Be(-1);
		}
		finally
		{
			LayoutsTestHooks.LinedFlowLayoutInvalidated -= OnInvalidated;
		}

		void OnInvalidated(object sender, LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs args)
		{
			invalidatedSender = sender;
			invalidatedArgs = args;
		}
	}

	[TestMethod]
	public async Task When_ItemsInfoRequested_Then_PublicSizingInfoControlsGeometry()
	{
		double[] desiredAspectRatios = [1.0, 2.0, 0.5, 1.0];
		double[] minWidths = [60.0, 10.0, 30.0, 10.0];
		double[] maxWidths = [100.0, 70.0, 100.0, 100.0];
		LinedFlowLayoutItemsInfoRequestedEventArgs? requestedArgs = null;
		var requestCount = 0;
		var layout = new LinedFlowLayout
		{
			LineHeight = 40,
			MinItemSpacing = 5,
		};
		layout.ItemsInfoRequested += OnItemsInfoRequested;
		using var template = new DynamicDataTemplate(() => new Border
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.SkyBlue),
		});
		var sut = new ItemsRepeater
		{
			Width = 240,
			ItemsSource = Enumerable.Range(0, desiredAspectRatios.Length).ToArray(),
			ItemTemplate = template.Value,
			Layout = layout,
		};

		try
		{
			await UITestHelper.Load(sut);
			await UITestHelper.WaitFor(() => requestCount > 0);
			await UITestHelper.WaitForIdle();

			requestedArgs.Should().NotBeNull();
			requestedArgs!.ItemsRangeStartIndex.Should().Be(0);
			requestedArgs.ItemsRangeRequestedLength.Should().Be(desiredAspectRatios.Length);
			layout.RequestedRangeStartIndex.Should().Be(0);
			layout.RequestedRangeLength.Should().Be(desiredAspectRatios.Length);

			var first = GetRealizedElement(sut, 0);
			var second = GetRealizedElement(sut, 1);
			var third = GetRealizedElement(sut, 2);

			first.ActualWidth.Should().BeApproximately(60, 1);
			second.ActualWidth.Should().BeApproximately(70, 1);
			third.ActualWidth.Should().BeApproximately(30, 1);
			((double)second.ActualOffset.Y).Should().BeApproximately(first.ActualOffset.Y, 1);
			((double)third.ActualOffset.Y).Should().BeApproximately(first.ActualOffset.Y, 1);
		}
		finally
		{
			layout.ItemsInfoRequested -= OnItemsInfoRequested;
			TestServices.WindowHelper.WindowContent = null;
		}

		void OnItemsInfoRequested(LinedFlowLayout sender, LinedFlowLayoutItemsInfoRequestedEventArgs args)
		{
			requestCount++;
			requestedArgs = args;
			var ratios = desiredAspectRatios
				.Skip(args.ItemsRangeStartIndex)
				.Take(args.ItemsRangeRequestedLength)
				.ToArray();
			args.SetDesiredAspectRatios(ratios);
			args.MinWidth = 10.0;
			args.MaxWidth = 100.0;
			args.SetMinWidths(minWidths
				.Skip(args.ItemsRangeStartIndex)
				.Take(args.ItemsRangeRequestedLength)
				.ToArray());
			args.SetMaxWidths(maxWidths
				.Skip(args.ItemsRangeStartIndex)
				.Take(args.ItemsRangeRequestedLength)
				.ToArray());
		}
	}

	[TestMethod]
	public async Task When_InvalidateItemsInfo_Then_RequestsAndAppliesUpdatedSizingInfo()
	{
		double[] desiredAspectRatios = [1.0, 1.0, 1.0, 1.0];
		var requestCount = 0;
		var layout = new LinedFlowLayout
		{
			LineHeight = 40,
			MinItemSpacing = 5,
		};
		layout.ItemsInfoRequested += OnItemsInfoRequested;
		using var template = new DynamicDataTemplate(() => new Border
		{
			Background = new SolidColorBrush(Microsoft.UI.Colors.SkyBlue),
		});
		var sut = new ItemsRepeater
		{
			Width = 240,
			ItemsSource = Enumerable.Range(0, desiredAspectRatios.Length).ToArray(),
			ItemTemplate = template.Value,
			Layout = layout,
		};

		try
		{
			await UITestHelper.Load(sut);
			await UITestHelper.WaitFor(() => requestCount > 0);
			await UITestHelper.WaitForIdle();

			GetRealizedElement(sut, 0).ActualWidth.Should().BeApproximately(40, 1);
			var initialRequestCount = requestCount;
			desiredAspectRatios = [2.0, 1.0, 1.0, 1.0];

			layout.InvalidateItemsInfo();

			await UITestHelper.WaitFor(() => requestCount > initialRequestCount);
			await UITestHelper.WaitFor(
				() => sut.TryGetElement(0) is FrameworkElement element && Math.Abs(element.ActualWidth - 80) < 1);

			GetRealizedElement(sut, 0).ActualWidth.Should().BeApproximately(80, 1);
			layout.RequestedRangeStartIndex.Should().Be(0);
			layout.RequestedRangeLength.Should().Be(desiredAspectRatios.Length);
		}
		finally
		{
			layout.ItemsInfoRequested -= OnItemsInfoRequested;
			TestServices.WindowHelper.WindowContent = null;
		}

		void OnItemsInfoRequested(LinedFlowLayout sender, LinedFlowLayoutItemsInfoRequestedEventArgs args)
		{
			requestCount++;
			args.SetDesiredAspectRatios(
				desiredAspectRatios
					.Skip(args.ItemsRangeStartIndex)
					.Take(args.ItemsRangeRequestedLength)
					.ToArray());
		}
	}

	[TestMethod]
	public async Task When_LockItemToLine_Then_HookReportsLockAndCollectionChangeUnlocksItems()
	{
		var items = new ObservableCollection<int>(Enumerable.Range(0, 6));
		using var template = new DynamicDataTemplate(() => new Border
		{
			Width = 50,
			Height = 40,
			Background = new SolidColorBrush(Microsoft.UI.Colors.SkyBlue),
		});
		var layout = new LinedFlowLayout
		{
			LineHeight = 40,
			MinItemSpacing = 5,
		};
		var sut = new ItemsRepeater
		{
			Width = 170,
			ItemsSource = items,
			ItemTemplate = template.Value,
			Layout = layout,
		};
		object? lockedSender = null;
		LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs? lockedArgs = null;
		LinedFlowLayout? unlockedSender = null;
		var unlockedCount = 0;
		LayoutsTestHooks.LinedFlowLayoutItemLocked += OnItemLocked;
		layout.ItemsUnlocked += OnItemsUnlocked;

		try
		{
			await UITestHelper.Load(sut);
			await UITestHelper.WaitFor(
				() => LayoutsTestHooks.GetLinedFlowLayoutSnappedAverageItemsPerLine(layout) > 0);

			var first = GetRealizedElement(sut, 0);
			var fifth = GetRealizedElement(sut, 4);
			((double)fifth.ActualOffset.Y).Should().BeGreaterThan(first.ActualOffset.Y);

			var lineIndex = layout.LockItemToLine(4);

			lineIndex.Should().BeGreaterThan(0);
			lockedSender.Should().BeSameAs(layout);
			lockedArgs.Should().NotBeNull();
			lockedArgs!.ItemIndex.Should().Be(4);
			lockedArgs.LineIndex.Should().Be(lineIndex);
			LayoutsTestHooks.GetLinedFlowLayoutLineIndex(layout, 4).Should().Be(lineIndex);

			items.Add(6);

			await UITestHelper.WaitFor(() => unlockedCount > 0);
			unlockedSender.Should().BeSameAs(layout);
		}
		finally
		{
			LayoutsTestHooks.LinedFlowLayoutItemLocked -= OnItemLocked;
			layout.ItemsUnlocked -= OnItemsUnlocked;
			TestServices.WindowHelper.WindowContent = null;
		}

		void OnItemLocked(object sender, LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs args)
		{
			lockedSender = sender;
			lockedArgs = args;
		}

		void OnItemsUnlocked(LinedFlowLayout sender, object args)
		{
			unlockedSender = sender;
			unlockedCount++;
		}
	}

	private static FrameworkElement GetRealizedElement(ItemsRepeater repeater, int index)
	{
		var element = repeater.TryGetElement(index) as FrameworkElement;
		element.Should().NotBeNull($"item {index} should be realized");
		return element!;
	}
}
#endif
