#if !NET461
#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using static System.Math;
using static Windows.UI.Xaml.Controls.Primitives.GeneratorDirection;
using Uno.UI.Extensions;
using System.Collections.Specialized;
using Uno.UI.Xaml.Controls;
#if __MACOS__
using AppKit;
#elif __IOS__
using UIKit;
#endif
#if __IOS__ || __ANDROID__
using _Panel = Uno.UI.Controls.ManagedItemsStackPanel;
#else
using _Panel = Windows.UI.Xaml.Controls.Panel;
#endif

namespace Windows.UI.Xaml.Controls
{
#if __IOS__ || __ANDROID__
	internal abstract partial class ManagedVirtualizingPanelLayout : DependencyObject
#else
	public abstract partial class VirtualizingPanelLayout : DependencyObject
#endif
	{
		private _Panel? _ownerPanel;
		private _Panel OwnerPanel
		{
			get => _ownerPanel ?? throw new InvalidOperationException($"{nameof(Initialize)}() was not called properly.");
			set => _ownerPanel = value;
		}
		private VirtualizingPanelGenerator? _generator;
		private protected VirtualizingPanelGenerator Generator
		{
			get => _generator ?? throw new InvalidOperationException($"{nameof(Initialize)}() was not called properly.");
			private set => _generator = value;
		}

		private ScrollViewer? ScrollViewer { get; set; }
		internal ItemsControl? ItemsControl { get; set; }
		public ListViewBase? XamlParent => ItemsControl as ListViewBase;

		/// <summary>
		/// Ordered record of all currently-materialized lines.
		/// </summary>
		private readonly Deque<Line> _materializedLines = new Deque<Line>();

		private Size _availableSize;
		private Size _lastMeasuredSize;
		private double _lastScrollOffset;

		/// <summary>
		/// The current average line height based on materialized lines. Used to estimate scroll extent of unmaterialized items.
		/// </summary>
		private double _averageLineHeight;

		/// <summary>
		/// The previous item to the old first visible item, used when a lightweight layout rebuild is called.
		/// </summary>
		private Uno.UI.IndexPath? _dynamicSeedIndex;
		/// <summary>
		/// Start position of the old first group, used when a lightweight layout rebuild is called.
		/// </summary>
		private double? _dynamicSeedStart;

		/// <summary>
		/// Pending collection changes to be processed when the list is re-measured.
		/// </summary>
		private readonly Queue<CollectionChangedOperation> _pendingCollectionChanges = new Queue<CollectionChangedOperation>();

		/// <summary>
		/// Pending scroll adjustment, if an item has been added/removed backward of the current visible viewport by a collection change.
		/// </summary>
		private double? _scrollAdjustmentForCollectionChanges;

		private double AvailableBreadth => ScrollOrientation == Orientation.Vertical ?
			_availableSize.Width :
			_availableSize.Height;

		/// <summary>
		/// The current offset from the original scroll position.
		/// </summary>
		private double ScrollOffset
		{
			get
			{
				if (ScrollViewer == null)
				{
					return 0;
				}

				return ScrollOrientation == Orientation.Vertical ?
					ScrollViewer.VerticalOffset :
					ScrollViewer.HorizontalOffset;
			}
		}

		/// <summary>
		/// The size of the scroll viewport.
		/// </summary>
		private Size ViewportSize { get; set; }

		/// <summary>
		/// The extent (parallel to scroll direction) of the scroll viewport.
		/// </summary>
		private double ViewportExtent
		{
			get
			{
				if (ScrollViewer == null)
				{
					return double.MaxValue / 1000;
				}

				return ScrollOrientation == Orientation.Vertical ?
					ViewportSize.Height :
					ViewportSize.Width;
			}
		}

		/// <summary>
		/// The start of the visible viewport, relative to the start of the panel.
		/// </summary>
		private double ViewportStart => ScrollOffset;
		/// <summary>
		/// The end of the visible viewport, relative to the end of the panel.
		/// </summary>
		private double ViewportEnd => ScrollOffset + ViewportExtent;
		/// <summary>
		/// The additional length in pixels for which to create buffered views.
		/// </summary>
		private double ViewportExtension => CacheLength * ViewportExtent * 0.5;
		/// <summary>
		/// The start of the 'extended viewport,' the area of the visible viewport plus the buffer area defined by <see cref="CacheLength"/>.
		/// </summary>
		private double ExtendedViewportStart
		{
			get
			{
				var unclampedStart = ViewportStart - ViewportExtension;
				return Math.Max(unclampedStart, 0);
			}
		}

		/// <summary>
		/// The end of the 'extended viewport,' the area of the visible viewport plus the buffer area defined by <see cref="CacheLength"/>.
		/// </summary>
		private double ExtendedViewportEnd
		{
			get
			{
				return ViewportEnd + ViewportExtension;
			}
		}

		private bool ShouldMeasuredBreadthStretch => ShouldBreadthStretch && GetBreadth(_availableSize) < double.MaxValue / 2;

		internal void Initialize(_Panel owner)
		{
			OwnerPanel = owner ?? throw new ArgumentNullException(nameof(owner));
			OwnerPanel.Loaded += OnLoaded;
			OwnerPanel.Unloaded += OnUnloaded;

			Generator = new VirtualizingPanelGenerator(this);
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			foreach (var parent in OwnerPanel.GetVisualAncestry())
			{
				if (parent is ScrollViewer scrollViewer && ScrollViewer == null)
				{
					ScrollViewer = scrollViewer;
					ScrollViewer.ViewChanged += OnScrollChanged;
				}
				else if (parent is ItemsControl itemsControl)
				{
					ItemsControl = itemsControl;
					break;
				}
			}

			var hasPopupPanelParent = OwnerPanel.FindFirstParent<PopupPanel>() != null;
			var hasListViewParent = OwnerPanel.FindFirstParent<ListViewBase>() != null;
			IsInsidePopup = hasPopupPanelParent && !hasListViewParent;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Calling {GetMethodTag()} hasPopupPanelParent={hasPopupPanelParent} hasListViewParent={hasListViewParent}");
			}

			if (
				ItemsControl == null
				&& OwnerPanel.TemplatedParent is ItemsControl popupItemsControl
			)
			{
				// This case is for an ItemsPresenter hosted in a Popup
				ItemsControl = popupItemsControl;
			}
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			if (ScrollViewer != null)
			{
				ScrollViewer.ViewChanged -= OnScrollChanged;
			}

			ScrollViewer = null;
			ItemsControl = null;
		}

		private void OnScrollChanged(object? sender, ScrollViewerViewChangedEventArgs e)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Calling {GetMethodTag()} _lastScrollOffset={_lastScrollOffset} ScrollOffset={ScrollOffset}");
			}
			var delta = ScrollOffset - _lastScrollOffset;
			var sign = Sign(delta);
			var unappliedDelta = Abs(delta);
			var fillDirection = sign > 0 ? Forward : Backward;

			while (unappliedDelta > 0)
			{
				var scrollIncrement =
					_scrollAdjustmentForCollectionChanges.HasValue ?
					// If we're adjusting for collection changes, apply scroll in 'one big hit' because we should exactly reuse scrapped items this way (since items in visible viewport should not have changed)
					Abs(_scrollAdjustmentForCollectionChanges.Value) :
					// Apply scroll in bite-sized increments. This is crucial for good performance, since the delta may be in the 100s or 1000s of pixels, and we want to recycle unseen views at the same rate that we dequeue newly-visible views.
					GetScrollConsumptionIncrement(fillDirection);

				_scrollAdjustmentForCollectionChanges = null;

				if (scrollIncrement == 0)
				{
					break;
				}
				unappliedDelta -= scrollIncrement;
				unappliedDelta = Max(0, unappliedDelta);
				UpdateLayout(extentAdjustment: sign * -unappliedDelta, isScroll: true);
			}
			ArrangeElements(_availableSize, ViewportSize);
			UpdateCompleted();

			_lastScrollOffset = ScrollOffset;
		}

		/// <summary>
		/// Get the increment to consume scroll changes, based on the size of the disappearing view.
		/// </summary>
		private double GetScrollConsumptionIncrement(GeneratorDirection fillDirection)
		{
			var incrementView = fillDirection == Forward ?
				GetFirstMaterializedLine()?.FirstView :
				GetLastMaterializedLine()?.LastView;

			if (incrementView == null)
			{
				//TODO: work properly when header/footer/group header are available, and may be larger than extended viewport
				return _averageLineHeight;
			}

			return GetActualExtent(incrementView);
		}

		internal Size MeasureOverride(Size availableSize)
		{
			if (ItemsControl == null)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug("Measured without an ItemsControl: simply return size(0,0) for now...");
				}

				return new Size(0, 0);
			}

			ViewportSize = ScrollViewer?.ViewportMeasureSize ?? default;
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Calling {GetMethodTag()}, availableSize={availableSize}, _availableSize={_availableSize} {GetDebugInfo()}");
			}

			_availableSize = availableSize;
			UpdateAverageLineHeight(); // Must be called before ScrapLayout(), or there won't be items to measure
			ScrapLayout();
			ApplyCollectionChanges();
			UpdateLayout(extentAdjustment: _scrollAdjustmentForCollectionChanges, isScroll: false);

			return _lastMeasuredSize = EstimatePanelSize(isMeasure: true);
		}

		internal Size ArrangeOverride(Size finalSize)
		{
			if (ItemsControl == null)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug("Arranged without an ItemsControl: simply return size(0,0) for now...");
				}

				return new Size(0, 0);
			}

			ViewportSize = ScrollViewer?.ViewportArrangeSize ?? default;
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Calling {GetMethodTag()}, finalSize={finalSize}, {GetDebugInfo()}");
			}

			_availableSize = finalSize;
			var adjustedVisibleWindow = ViewportSize;
			ArrangeElements(finalSize, adjustedVisibleWindow);

			return EstimatePanelSize(isMeasure: false);
		}

		private void ArrangeElements(Size finalSize, Size adjustedVisibleWindow)
		{
			foreach (var line in _materializedLines)
			{
				var indexAdjustment = -1;
				foreach (var item in line.Items)
				{
					indexAdjustment++;

					var bounds = GetBoundsForElement(item.container);
					var arrangedBounds = GetElementArrangeBounds(line.FirstItemFlat + indexAdjustment, bounds, adjustedVisibleWindow, finalSize);
					item.container.Arrange(arrangedBounds);
				}
			}
		}

		/// <summary>
		/// Update the item container layout by removing no-longer-visible views and adding visible views.
		/// </summary>
		/// <param name="extentAdjustment">Adjustment to apply when calculating fillable area.</param>
		private void UpdateLayout(double? extentAdjustment, bool isScroll)
		{
			ResetReorderingIndex();
			OwnerPanel.ShouldInterceptInvalidate = true;

			UnfillLayout(extentAdjustment ?? 0);
			FillLayout(extentAdjustment ?? 0);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Called {GetMethodTag()}, {GetDebugInfo()} extentAdjustment={extentAdjustment}");
			}

			if (!isScroll)
			{
				UpdateCompleted();
			}

			OwnerPanel.ShouldInterceptInvalidate = false;
		}

		/// <summary>
		/// Called after an update cycle is completed. 
		/// </summary>
		private void UpdateCompleted()
		{
			// If we're not stretched, then added views may change the breadth of the list, so we allow measure requests to propagate
			OwnerPanel.ShouldInterceptInvalidate = ShouldMeasuredBreadthStretch;

			Generator.ClearScrappedViews();
			Generator.UpdateVisibilities();

			OwnerPanel.ShouldInterceptInvalidate = false;
		}

		/// <summary>
		/// Fill in extended viewport with views.
		/// </summary>
		/// <param name="extentAdjustment">Adjustment to apply when calculating fillable area.</param>
		private void FillLayout(double extentAdjustment)
		{
			if (!_dynamicSeedStart.HasValue) // Don't fill backward if we're doing a light rebuild (since we are starting from nearest previously-visible item)
			{
				FillBackward();
			}

			FillForward();

			// Make sure that the reorder item has been rendered
			if (GetReorderingIndex() is { } reorderIndex && _materializedLines.None(line => line.Contains(reorderIndex)))
			{
				AddLine(Forward, reorderIndex);
			}

			_dynamicSeedIndex = null;
			_dynamicSeedStart = null;

			void FillBackward()
			{
				if (GetItemsStart() > ExtendedViewportStart + extentAdjustment)
				{
					var nextItem = GetNextUnmaterializedItem(Backward, GetFirstMaterializedIndexPath());
					while (nextItem != null && GetItemsStart() > ExtendedViewportStart + extentAdjustment)
					{
						AddLine(Backward, nextItem.Value);
						nextItem = GetNextUnmaterializedItem(Backward, GetFirstMaterializedIndexPath());
					}
				}
			}

			void FillForward()
			{
				if ((GetItemsEnd() ?? 0) < ExtendedViewportEnd + extentAdjustment)
				{
					var nextItem = GetNextUnmaterializedItem(Forward, _dynamicSeedIndex ?? GetLastMaterializedIndexPath());
					while (nextItem != null && (GetItemsEnd() ?? 0) < ExtendedViewportEnd + extentAdjustment)
					{
						AddLine(Forward, nextItem.Value);
						nextItem = GetNextUnmaterializedItem(Forward, GetLastMaterializedIndexPath());
					}
				}
			}
		}

		/// <summary>
		/// Remove views that lie entirely outside extended viewport.
		/// </summary>
		/// <param name="extentAdjustment">Adjustment to apply when calculating fillable area.</param>
		private void UnfillLayout(double extentAdjustment)
		{
			UnfillBackward();
			UnfillForward();

			void UnfillBackward()
			{
				var firstMaterializedLine = GetFirstMaterializedLine();
				while (firstMaterializedLine != null && GetMeasuredEnd(firstMaterializedLine.FirstView) < ExtendedViewportStart + extentAdjustment)
				{
					// Dematerialize lines that are entirely outside extended viewport
					RecycleLine(firstMaterializedLine);
					_materializedLines.RemoveFromFront();
					firstMaterializedLine = GetFirstMaterializedLine();
				}
			}

			void UnfillForward()
			{
				var lastMaterializedLine = GetLastMaterializedLine();
				while (lastMaterializedLine != null && GetMeasuredStart(lastMaterializedLine.FirstView) > ExtendedViewportEnd + extentAdjustment)
				{
					// Dematerialize lines that are entirely outside extended viewport
					RecycleLine(lastMaterializedLine);
					_materializedLines.RemoveFromBack();
					lastMaterializedLine = GetLastMaterializedLine();
				}
			}
		}

		/// <summary>
		/// Recycle all views for a given line.
		/// </summary>
		private void RecycleLine(Line line)
		{
			for (int i = 0; i < line.Items.Length; i++)
			{
				Generator.RecycleViewForItem(line.Items[i].container, line.FirstItemFlat + i);
			}
		}

		/// <summary>
		/// Send views in line to temporary scrap.
		/// </summary>
		private void ScrapLine(Line line)
		{
			for (int i = 0; i < line.Items.Length; i++)
			{
				Generator.ScrapViewForItem(line.Items[i].container, line.FirstItemFlat + i);
			}
		}

		/// <summary>
		/// If there are pending collection changes, update values and prepare the layouter accordingly.
		/// </summary>
		private void ApplyCollectionChanges()
		{
			if (_pendingCollectionChanges.Count == 0)
			{
				return;
			}

			// Call before applying scroll, because scroll is applied synchronously on some platforms
			Generator.UpdateForCollectionChanges(_pendingCollectionChanges);

			if (_dynamicSeedIndex is Uno.UI.IndexPath dynamicSeedIndex)
			{
				var updated = CollectionChangedOperation.Offset(dynamicSeedIndex, _pendingCollectionChanges);
				if (updated is Uno.UI.IndexPath updatedValue)
				{
					_dynamicSeedIndex = updated;

					var itemOffset = updatedValue.Row - dynamicSeedIndex.Row; // TODO: This will need to change when grouping is supported
					var scrollAdjustment = itemOffset * _averageLineHeight; // TODO: not appropriate for ItemsWrapGrid
					ApplyScrollAdjustmentForCollectionChange(scrollAdjustment);
				}
				// TODO: handle the case where seed was removed
			}

			_pendingCollectionChanges.Clear();
		}

		/// <summary>
		/// Update scroll offset for changes in position from collection change operation
		/// </summary>
		private void ApplyScrollAdjustmentForCollectionChange(double scrollAdjustment)
		{
			if (scrollAdjustment == 0)
			{
				return;
			}

			_dynamicSeedStart += scrollAdjustment;

			if ((scrollAdjustment < 0 && IsScrolledToStart()) ||
				(scrollAdjustment > 0 && IsScrolledToEnd())
			)
			{
				// Don't call ChangeView() if there's no room to scroll, because if we set _scrollAdjustmentForCollectionChanges then we expect it to be
				// handled in OnScrollChanged
				// TODO: Handle this better, because it leads to an unwanted jump in the position of visible elements.
				return;
			}

			// Set adjustment before calling ChangeView(), because OnScrollChanged() will be called synchronously on some platforms
			_scrollAdjustmentForCollectionChanges = scrollAdjustment;

			if (ScrollOrientation == Orientation.Vertical)
			{
				ScrollViewer?.ChangeView(null, ScrollViewer.VerticalOffset + scrollAdjustment, null, disableAnimation: true);
			}
			else
			{
				ScrollViewer?.ChangeView(ScrollViewer.HorizontalOffset + scrollAdjustment, null, null, disableAnimation: true);
			}
		}

		/// <summary>
		/// True if the scroll position is right at the start of the list.
		/// </summary>
		private bool IsScrolledToStart()
		{
			if (ScrollViewer == null)
			{
				return true;
			}

			return ScrollOrientation == Orientation.Vertical ?
				ScrollViewer.VerticalOffset <= 0 :
				ScrollViewer.HorizontalOffset <= 0;
		}

		/// <summary>
		/// True if the scroll position is all the way to the end of the list.
		/// </summary>
		private bool IsScrolledToEnd()
		{
			if (ScrollViewer == null)
			{
				return true;
			}

			return ScrollOrientation == Orientation.Vertical ?
				ScrollViewer.VerticalOffset >= ScrollViewer.ScrollableHeight :
				ScrollViewer.HorizontalOffset >= ScrollViewer.ScrollableWidth;
		}

		/// <summary>
		/// Estimate the 'correct' size of the panel.
		/// </summary>
		/// <param name="isMeasure">True if this is called from measure, false if after arrange.</param>
		private Size EstimatePanelSize(bool isMeasure)
		{
			var extent = EstimatePanelExtent();

			var ret = ScrollOrientation == Orientation.Vertical
				? new Size(
					isMeasure ? CalculatePanelMeasureBreadth() : CalculatePanelArrangeBreadth(),
					double.IsInfinity(_availableSize.Height) ? extent : Max(extent, _availableSize.Height)
				)
				: new Size(
					double.IsInfinity(_availableSize.Width) ? extent : Max(extent, _availableSize.Width),
					isMeasure ? CalculatePanelMeasureBreadth() : CalculatePanelArrangeBreadth()
				);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()} => {extent} -> {ret} {ScrollOrientation} {_availableSize.Height} {double.IsInfinity(_availableSize.Height)} ShouldBreadthStretch:{ShouldBreadthStretch} XamlParent:{XamlParent} AvailableBreadth:{AvailableBreadth}");
			}

			return ret;
		}

		/// <summary>
		/// Estimate the 'correct' extent of the panel, based on number and guessed size of remaining unmaterialized items.
		/// </summary>
		private double EstimatePanelExtent()
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()} Begin");
			}

			// Estimate remaining extent based on current average line height and remaining unmaterialized items
			var lastIndexPath = GetLastMaterializedIndexPath();
			if (lastIndexPath == null)
			{
				return 0;
			}
			var lastItem = GetFlatItemIndex(lastIndexPath.Value);

			var remainingItems = (ItemsControl?.NumberOfItems - lastItem - 1) ?? 0;
			UpdateAverageLineHeight();

			int itemsPerLine = GetItemsPerLine();
			var remainingLines = remainingItems / itemsPerLine + remainingItems % itemsPerLine;

			double estimatedExtent = GetContentEnd() + remainingLines * _averageLineHeight;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()}=>{estimatedExtent}, GetContentEnd()={GetContentEnd()}, remainingLines={remainingLines}, averageLineHeight={_averageLineHeight}");
			}

			return estimatedExtent;
		}

		private void UpdateAverageLineHeight()
		{
			_averageLineHeight = _materializedLines.Count > 0 ? _materializedLines.Select(l => GetMeasuredExtent(l.FirstView)).Average()
				: 0;
		}

		private double CalculatePanelMeasureBreadth() => _materializedLines.Select(l => GetDesiredBreadth(l.FirstView)).MaxOrDefault()
#if __WASM__
			+ GetBreadth(XamlParent.ScrollViewer.ScrollBarSize)
#endif
				;

		private double CalculatePanelArrangeBreadth() => ShouldMeasuredBreadthStretch ? AvailableBreadth :
					_materializedLines.Select(l => GetActualBreadth(l.FirstView)).MaxOrDefault();

		internal void AddItems(int firstItem, int count, int section)
		{
			_pendingCollectionChanges.Enqueue(new CollectionChangedOperation(
				startingIndex: Uno.UI.IndexPath.FromRowSection(firstItem, section),
				range: count,
				action: NotifyCollectionChangedAction.Add,
				elementType: CollectionChangedOperation.Element.Item
				));

			LightRefresh();
		}

		internal void RemoveItems(int firstItem, int count, int section)
		{
			_pendingCollectionChanges.Enqueue(new CollectionChangedOperation(
				startingIndex: Uno.UI.IndexPath.FromRowSection(firstItem, section),
				range: count,
				action: NotifyCollectionChangedAction.Remove,
				elementType: CollectionChangedOperation.Element.Item
				));

			LightRefresh();
		}

		/// <summary>
		/// Update the display of the panel without clearing caches.
		/// </summary>
		private void LightRefresh()
			=> OwnerPanel?.InvalidateMeasure();

		internal void Refresh()
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()}");
			}

			ClearLines();

			UpdateCompleted();
			Generator.ClearIdCache();
			_pendingCollectionChanges.Clear();
			OwnerPanel?.InvalidateMeasure();
		}

		private void ClearLines()
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()}");
			}

			foreach (var line in _materializedLines)
			{
				RecycleLine(line);
			}
			_materializedLines.Clear();
		}

		/// <summary>
		/// Sends all views to temporary scrap, in preparation for a lightweight layout rebuild.
		/// </summary>
		private void ScrapLayout()
		{
			var firstVisibleItem = GetFirstMaterializedIndexPath();
			if (GetReorderingIndex() is { } reorderIndex && reorderIndex == firstVisibleItem)
			{
				firstVisibleItem = _materializedLines.SelectMany(line => line.Items).Skip(1).FirstOrDefault().index;
			}

			_dynamicSeedIndex = GetDynamicSeedIndex(firstVisibleItem);
			_dynamicSeedStart = GetContentStart();

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()} seed index={_dynamicSeedIndex} seed start={_dynamicSeedStart}");
			}

			foreach (var line in _materializedLines)
			{
				ScrapLine(line);
			}
			_materializedLines.Clear();
		}

		/// <summary>
		/// Get 'seed' index for recreating the visual state of the list after <see cref="ScrapLayout()"/>;
		/// </summary>
		protected virtual Uno.UI.IndexPath? GetDynamicSeedIndex(Uno.UI.IndexPath? firstVisibleItem)
		{
			var lastItem = XamlParent?.GetLastItem();
			if (lastItem == null ||
				(firstVisibleItem != null && firstVisibleItem.Value > lastItem.Value)
			)
			{
				// None of the previously-visible indices are now present in the updated items source
				return null;
			}
			return GetNextUnmaterializedItem(Backward, firstVisibleItem);
		}

		private void OnOrientationChanged(Orientation newValue)
		{
			Refresh();
		}

		private Uno.UI.IndexPath GetFirstVisibleIndexPath()
		{
			throw new NotImplementedException(); //TODO: FirstVisibleIndex
		}

		private Uno.UI.IndexPath GetLastVisibleIndexPath()
		{
			throw new NotImplementedException(); //TODO: LastVisibleIndex
		}

		private IEnumerable<float> GetSnapPointsInner(SnapPointsAlignment alignment)
		{
			throw new NotImplementedException(); //TODO: snap points support
		}

		private float AdjustOffsetForSnapPointsAlignment(float offset) => throw new NotImplementedException();

		private void AddLine(GeneratorDirection fillDirection, Uno.UI.IndexPath nextVisibleItem)
		{
			var extentOffset = fillDirection == Backward ? GetContentStart() : GetContentEnd();
			var line = CreateLine(fillDirection, extentOffset, AvailableBreadth, nextVisibleItem);

			if (fillDirection == Backward)
			{
				_materializedLines.AddToFront(line);
			}
			else
			{
				_materializedLines.AddToBack(line);
			}

			// The layout might have decided to insert another item (pending reorder item), so make sure to add the requested item anyway.
			// Note: We must ensure to add the requested item so the Get<First|Last>MaterializedLine()
			//		 and Get<Content|Items><Start|End>() will still return a meaningful values.
			if (!line.Contains(nextVisibleItem))
			{
				AddLine(fillDirection, nextVisibleItem);
			}
		}

		/// <summary>
		/// Create a new line.
		/// </summary>
		protected abstract Line CreateLine(GeneratorDirection fillDirection, double extentOffset, double availableBreadth, Uno.UI.IndexPath nextVisibleItem);

		protected abstract int GetItemsPerLine();

		protected int GetFlatItemIndex(Uno.UI.IndexPath indexPath) => ItemsControl?.GetIndexFromIndexPath(indexPath) ?? -1;

		protected void AddView(FrameworkElement view, GeneratorDirection fillDirection, double extentOffset, double breadthOffset)
		{
			if (view.Parent == null)
			{
				//Freshly-created view
				OwnerPanel.Children.Add(view);
			}

			var slotSize = ScrollOrientation == Orientation.Vertical ?
				new Size(AvailableBreadth, double.PositiveInfinity) :
				new Size(double.PositiveInfinity, AvailableBreadth);

			view.Measure(slotSize);

			var extentOffsetAdjustment = fillDirection == Forward ?
				0 :
				-GetExtent(view.DesiredSize);

			var topLeft = ScrollOrientation == Orientation.Vertical ?
				new Point(breadthOffset, extentOffset + extentOffsetAdjustment) :
				new Point(extentOffset + extentOffsetAdjustment, breadthOffset);

			// TODO: GetElementBounds()
			var finalRect = new Rect(topLeft, view.DesiredSize);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()} finalRect={finalRect} AvailableBreadth={AvailableBreadth} DesiredSize={view.DesiredSize} DC={view.DataContext}");
			}

			SetBounds(view, finalRect);
		}

		protected abstract Rect GetElementArrangeBounds(/*TODO ElementType, */int elementIndex, Rect containerBounds, Size windowConstraint, Size finalSize);

		private void SetBounds(FrameworkElement view, Rect bounds)
		{
			if (view is ContentControl container)
			{
				container.GetVirtualizationInformation().Bounds = bounds;
			}
			else if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"Non-ContentControl containers aren't supported for virtualizing panel types.");
			}
		}

		private Line? GetFirstMaterializedLine() => _materializedLines.Count > 0 ? _materializedLines[0] : null;

		private Line? GetLastMaterializedLine() => _materializedLines.Count > 0 ? _materializedLines[_materializedLines.Count - 1] : null;

		private Uno.UI.IndexPath? GetFirstMaterializedIndexPath() => GetFirstMaterializedLine()?.FirstItem;

		private Uno.UI.IndexPath? GetLastMaterializedIndexPath() => GetLastMaterializedLine()?.LastItem;

		private double? GetItemsStart()
		{
			var firstView = GetFirstMaterializedLine()?.FirstView;
			if (firstView != null)
			{
				return GetMeasuredStart(firstView);
			}

			return null;
		}

		private double? GetItemsEnd()
		{
			var lastView = GetLastMaterializedLine()?.LastView;
			if (lastView != null)
			{
				return GetMeasuredEnd(lastView);
			}

			// This will be null except immediately after ScrapLayout(), when it will be the previous start of materialized items
			return _dynamicSeedStart;
		}

		private double GetContentStart() => GetItemsStart() ?? 0;

		private double GetContentEnd() => GetItemsEnd() ?? 0;

		private double GetMeasuredStart(FrameworkElement child)
		{
			var bounds = GetBoundsForElement(child);

			return ScrollOrientation == Orientation.Vertical ?
				bounds.Top :
				bounds.Left;
		}

		private double GetMeasuredEnd(FrameworkElement child)
		{
			var bounds = GetBoundsForElement(child);

			return ScrollOrientation == Orientation.Vertical ?
				bounds.Bottom :
				bounds.Right;
		}

		private double GetMeasuredExtent(FrameworkElement child)
		{
			var bounds = GetBoundsForElement(child);

			return ScrollOrientation == Orientation.Vertical ?
				bounds.Height :
				bounds.Width;

		}

		private double GetActualExtent(FrameworkElement child)
		{
			return ScrollOrientation == Orientation.Vertical ?
				child.ActualHeight :
				child.ActualWidth;
		}

		private Rect GetBoundsForElement(FrameworkElement child)
		{
			if (!(child is ContentControl container))
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning($"Non-ContentControl containers aren't supported for virtualizing panel types.");
				}

				return default;
			}

			return container.GetVirtualizationInformation().Bounds;

		}

		private double GetExtent(Size size) => ScrollOrientation == Orientation.Vertical ?
			size.Height :
			size.Width;

		protected double GetBreadth(Size size) => ScrollOrientation == Orientation.Vertical ?
			size.Width :
			size.Height;

		protected double GetBreadth(Rect rect) => ScrollOrientation == Orientation.Vertical ?
			rect.Width :
			rect.Height;

		protected void SetBreadth(ref Rect rect, double breadth)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				rect.Width = breadth;
			}
			else
			{
				rect.Height = breadth;
			}
		}


		private double GetActualBreadth(FrameworkElement view) => ScrollOrientation == Orientation.Vertical ?
			view.ActualWidth :
			view.ActualHeight;

		private double GetDesiredBreadth(FrameworkElement view) => GetBreadth(view.DesiredSize);

		private string GetMethodTag([CallerMemberName] string? caller = null)
		{
			return $"{nameof(VirtualizingPanelLayout)}.{caller}()";
		}

		private string GetDebugInfo()
		{
			return $"Parent ItemsControl={ItemsControl} ItemsSource={ItemsControl?.ItemsSource} NoOfItems={ItemsControl?.NumberOfItems} FirstMaterialized={GetFirstMaterializedIndexPath()} LastMaterialized={GetLastMaterializedIndexPath()} ExtendedViewportStart={ExtendedViewportStart} ExtendedViewportEnd={ExtendedViewportEnd} GetItemsStart()={GetItemsStart()} GetItemsEnd()={GetItemsEnd()}";
		}

#if __WASM__ || __SKIA__
		private static Point GetRelativePosition(FrameworkElement child) => child.RelativePosition;
#elif __NETSTD_REFERENCE__
		private static Point GetRelativePosition(FrameworkElement child) => throw new NotSupportedException();
#elif __MACOS__ || __IOS__
		private static Point GetRelativePosition(FrameworkElement child) => child.Frame.Location;
#elif __ANDROID__
		private static Point GetRelativePosition(FrameworkElement child) => new Point(ViewHelper.PhysicalToLogicalPixels(child.Left), ViewHelper.PhysicalToLogicalPixels(child.Top));
#endif

		private (double offset, double breadth, object item, Uno.UI.IndexPath? index)? _pendingReorder;
		internal void UpdateReorderingItem(Point location, FrameworkElement element, object item)
		{
			_pendingReorder = Orientation == Orientation.Horizontal
				? (location.X + ScrollOffset, element.ActualWidth, item, default(Uno.UI.IndexPath?))
				: (location.Y + ScrollOffset, element.ActualHeight, item, default(Uno.UI.IndexPath?));

			LightRefresh();
		}

		internal Uno.UI.IndexPath? CompleteReorderingItem(FrameworkElement element, object item)
		{
			var updatedIndex = default(Uno.UI.IndexPath?);
			if (_pendingReorder?.index is { } index)
			{
				var nextItem = _materializedLines
					.SelectMany(line => line.Items)
					.SkipWhile(i => i.index != index)
					.Skip(1)
					.FirstOrDefault();

				updatedIndex = nextItem.container is null
					? Uno.UI.IndexPath.FromRowSection(int.MaxValue, int.MaxValue) // There is no "nextItem", i.e. the item has been moved at the end.
					: nextItem.index;
			}
			_pendingReorder = null;

			// We need a full refresh to properly re-arrange all items at their right location,
			// ignoring the temp location of the dragged / reordered item.
			Refresh();

			return updatedIndex;
		}

		protected bool ShouldInsertReorderingView(double extentOffset)
			=> _pendingReorder is { } reorder && reorder.offset > extentOffset && reorder.offset <= extentOffset + reorder.breadth;

		protected Uno.UI.IndexPath? GetReorderingIndex()
		{
			if (_pendingReorder is { } reorder)
			{
				if (reorder.index is null)
				{
					reorder.index = XamlParent!.GetIndexPathFromItem(reorder.item);
					_pendingReorder = reorder; // _pendingReorder is a struct!
				}

				return reorder.index;
			}

			return null;
		}

		private void ResetReorderingIndex()
		{
			if (_pendingReorder is { } reorder)
			{
				_pendingReorder = (reorder.offset, reorder.breadth, reorder.item, null);
			}
		}

		/// <summary>
		/// Represents a single row in a vertically-scrolling panel, or a column in a horizontally-scrolling panel.
		/// </summary>
		protected class Line
		{
			public (FrameworkElement container, Uno.UI.IndexPath index)[] Items { get; }
			public Uno.UI.IndexPath FirstItem { get; }
			public Uno.UI.IndexPath LastItem { get; }
			public int FirstItemFlat { get; }

			public FrameworkElement FirstView => Items[0].container;
			public FrameworkElement LastView => Items[Items.Length - 1].container;

			public Line(int firstItemFlat, params (FrameworkElement container, Uno.UI.IndexPath index)[] items)
			{
				if (items.Length == 0)
				{
					throw new InvalidOperationException("Line must contain at least one view");
				}

				Items = items;
				FirstItem = items[0].index;
				LastItem = items.Last().index;
				FirstItemFlat = firstItemFlat;
			}

			public bool Contains(Uno.UI.IndexPath index)
				=> Items.Any(i => i.index == index);
		}
	}
}

#endif
