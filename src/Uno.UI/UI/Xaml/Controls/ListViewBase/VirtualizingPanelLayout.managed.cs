using System;
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

namespace Windows.UI.Xaml.Controls
{
	public abstract partial class VirtualizingPanelLayout : DependencyObject
	{
		private IVirtualizingPanel Owner { get; set; }
		private Panel OwnerPanel => Owner as Panel;
		private protected VirtualizingPanelGenerator Generator { get; private set; }

		private ScrollViewer ScrollViewer { get; set; }
		internal ItemsControl ItemsControl { get; set; }
		public ListViewBase XamlParent => ItemsControl as ListViewBase;

		/// <summary>
		/// Ordered record of all currently-materialized lines.
		/// </summary>
		private readonly Deque<Line> _materializedLines = new Deque<Line>();

		private Size _availableSize;
		private Size _lastMeasuredSize;
		private double _lastScrollOffset;

		/// <summary>
		/// The previous item to the old first visible item, used when a lightweight layout rebuild is called.
		/// </summary>
		private IndexPath? _dynamicSeedIndex;
		/// <summary>
		/// Start position of the old first group, used when a lightweight layout rebuild is called.
		/// </summary>
		private double? _dynamicSeedStart;

		private double AvailableBreadth => ScrollOrientation == Orientation.Vertical ?
			_availableSize.Width :
			_availableSize.Height;

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

		private Size ViewportSize { get; set; }

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

		internal void Initialize(IVirtualizingPanel owner)
		{
			Owner = owner ?? throw new ArgumentNullException(nameof(owner));
			OwnerPanel.Loaded += OnLoaded;
			OwnerPanel.Unloaded += OnUnloaded;

			Generator = new VirtualizingPanelGenerator(this);
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			FrameworkElement parent = OwnerPanel;
			while (parent != null && ItemsControl == null)
			{
				parent = parent.Parent as FrameworkElement;
				if (parent is ScrollViewer scrollViewer && ScrollViewer == null)
				{
					ScrollViewer = scrollViewer;
					ScrollViewer.ViewChanged += OnScrollChanged;
				}
				else if (parent is ItemsControl itemsControl)
				{
					ItemsControl = itemsControl;
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

		private void OnScrollChanged(object sender, ScrollViewerViewChangedEventArgs e)
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
				// Apply scroll in bite-sized increments. This is crucial for good performance, since the delta may be in the 100s or 1000s of pixels, and we want to recycle unseen views at the same rate that we dequeue newly-visible views.
				var scrollIncrement = GetScrollConsumptionIncrement(fillDirection);
				if (scrollIncrement == 0)
				{
					break;
				}
				unappliedDelta -= scrollIncrement;
				unappliedDelta = Max(0, unappliedDelta);
				UpdateLayout(extentAdjustment: sign * -unappliedDelta);
			}
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
				return 0;
			}

			return GetExtent(incrementView);
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
			ScrapLayout();
			UpdateLayout();

			return _lastMeasuredSize = EstimatePanelSize(isMeasure: true);
		}

		internal Size ArrangeOverride(Size finalSize)
		{
			if (ItemsControl == null)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().LogDebug("Measured without an ItemsControl: simply return size(0,0) for now...");
				}

				return new Size(0, 0);
			}

			ViewportSize = ScrollViewer?.ViewportArrangeSize ?? default;
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Calling {GetMethodTag()}, finalSize={finalSize}, {GetDebugInfo()}");
			}

			_availableSize = finalSize;
			UpdateLayout();

			return EstimatePanelSize(isMeasure: false);
		}

		private void UpdateLayout(double? extentAdjustment = null)
		{
			OwnerPanel.ShouldInterceptInvalidate = true;

			UnfillLayout(extentAdjustment ?? 0);
			FillLayout(extentAdjustment ?? 0);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Called {GetMethodTag()}, {GetDebugInfo()} extentAdjustment={extentAdjustment}");
			}

			if (!extentAdjustment.HasValue)
			{
				UpdateCompleted();
			}

			OwnerPanel.ShouldInterceptInvalidate = false;
		}

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

			_dynamicSeedIndex = null;
			_dynamicSeedStart = null;

			void FillBackward()
			{
				if (GetItemsStart() > ExtendedViewportStart + extentAdjustment)
				{
					var nextItem = GetNextUnmaterializedItem(Backward, GetFirstMaterializedIndexPath());
					while (nextItem != null && GetItemsStart() > ExtendedViewportStart + extentAdjustment)
					{
						// Fill gap at start with views
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
				while (firstMaterializedLine != null && GetEnd(firstMaterializedLine.FirstView) < ExtendedViewportStart + extentAdjustment)
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
				while (lastMaterializedLine != null && GetStart(lastMaterializedLine.FirstView) > ExtendedViewportEnd + extentAdjustment)
				{
					// Dematerialize lines that are entirely outside extended viewport
					RecycleLine(lastMaterializedLine);
					_materializedLines.RemoveFromBack();
					lastMaterializedLine = GetLastMaterializedLine();
				}
			}
		}

		private void RecycleLine(Line firstMaterializedLine)
		{
			for (int i = 0; i < firstMaterializedLine.ContainerViews.Length; i++)
			{
				Generator.RecycleViewForItem(firstMaterializedLine.ContainerViews[i], firstMaterializedLine.FirstItemFlat + i);
			}
		}

		/// <summary>
		/// Send views in line to temporary scrap.
		/// </summary>
		/// <param name="firstMaterializedLine"></param>
		private void ScrapLine(Line firstMaterializedLine)
		{
			for (int i = 0; i < firstMaterializedLine.ContainerViews.Length; i++)
			{
				Generator.ScrapViewForItem(firstMaterializedLine.ContainerViews[i], firstMaterializedLine.FirstItemFlat + i);
			}
		}

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

			var remainingItems = ItemsControl.NumberOfItems - lastItem - 1;

			var averageLineHeight = _materializedLines.Select(l => GetExtent(l.FirstView)).Average();

			int itemsPerLine = GetItemsPerLine();
			var remainingLines = remainingItems / itemsPerLine + remainingItems % itemsPerLine;

			double estimatedExtent = GetContentEnd() + remainingLines * averageLineHeight;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()}=>{estimatedExtent}, GetContentEnd()={GetContentEnd()}, remainingLines={remainingLines}, averageLineHeight={averageLineHeight}");
			}

			return estimatedExtent;
		}

		private double CalculatePanelMeasureBreadth() => ShouldMeasuredBreadthStretch ? AvailableBreadth :
					_materializedLines.Select(l => GetDesiredBreadth(l.FirstView)).MaxOrDefault() + GetBreadth(XamlParent.ScrollViewer.ScrollBarSize);

		private double CalculatePanelArrangeBreadth() => ShouldMeasuredBreadthStretch ? AvailableBreadth :
					_materializedLines.Select(l => GetActualBreadth(l.FirstView)).MaxOrDefault();

		internal void Refresh()
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()}");
			}

			ClearLines();

			UpdateCompleted();
			Generator.ClearIdCache();
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
		protected virtual IndexPath? GetDynamicSeedIndex(IndexPath? firstVisibleItem)
		{

			var lastItem = XamlParent.GetLastItem();
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

		private IndexPath GetFirstVisibleIndexPath()
		{
			throw new NotImplementedException(); //TODO: FirstVisibleIndex
		}

		private IndexPath GetLastVisibleIndexPath()
		{
			throw new NotImplementedException(); //TODO: LastVisibleIndex
		}

		private IEnumerable<float> GetSnapPointsInner(SnapPointsAlignment alignment)
		{
			throw new NotImplementedException(); //TODO: snap points support
		}

		private float AdjustOffsetForSnapPointsAlignment(float offset) => throw new NotImplementedException();

		private void AddLine(GeneratorDirection fillDirection, IndexPath nextVisibleItem)
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
		}

		/// <summary>
		/// Create a new line.
		/// </summary>
		protected abstract Line CreateLine(GeneratorDirection fillDirection, double extentOffset, double availableBreadth, IndexPath nextVisibleItem);

		protected abstract int GetItemsPerLine();

		protected int GetFlatItemIndex(IndexPath indexPath) => ItemsControl.GetIndexFromIndexPath(indexPath);

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

			var adjustedDesiredSize = ScrollOrientation == Orientation.Vertical
				? new Size(
					ShouldMeasuredBreadthStretch ? AvailableBreadth :
					(_lastMeasuredSize != default ? GetBreadth(_lastMeasuredSize) : view.DesiredSize.Width),
					view.DesiredSize.Height
				)
				: new Size(
					view.DesiredSize.Width,
					ShouldMeasuredBreadthStretch ? AvailableBreadth :
					(_lastMeasuredSize != default ? GetBreadth(_lastMeasuredSize) : view.DesiredSize.Height)
				);

			var finalRect = new Rect(topLeft, adjustedDesiredSize);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()} finalRect={finalRect} AvailableBreadth={AvailableBreadth} adjustedDesiredSize={adjustedDesiredSize} DC={view.DataContext}");
			}

			view.Arrange(finalRect);
		}

		private Line GetFirstMaterializedLine() => _materializedLines.Count > 0 ? _materializedLines[0] : null;

		private Line GetLastMaterializedLine() => _materializedLines.Count > 0 ? _materializedLines[_materializedLines.Count - 1] : null;

		private IndexPath? GetFirstMaterializedIndexPath() => GetFirstMaterializedLine()?.FirstItem;

		private IndexPath? GetLastMaterializedIndexPath() => GetLastMaterializedLine()?.LastItem;

		private double? GetItemsStart()
		{
			var firstView = GetFirstMaterializedLine()?.FirstView;
			if (firstView != null)
			{
				return GetStart(firstView);
			}

			return null;
		}

		private double? GetItemsEnd()
		{
			var lastView = GetLastMaterializedLine()?.LastView;
			if (lastView != null)
			{
				return GetEnd(lastView);
			}

			// This will be null except immediately after ScrapLayout(), when it will be the previous start of materialized items
			return _dynamicSeedStart;
		}

		private double GetContentStart() => GetItemsStart() ?? 0;

		private double GetContentEnd() => GetItemsEnd() ?? 0;

		private double GetStart(FrameworkElement child)
		{
			var offset = child.RelativePosition;
			return ScrollOrientation == Orientation.Vertical ?
				offset.Y - child.Margin.Top :
				offset.X - child.Margin.Left;
		}

		private double GetEnd(FrameworkElement child)
		{
			var offset = child.RelativePosition;
			return ScrollOrientation == Orientation.Vertical ?
				offset.Y + child.ActualHeight + child.Margin.Bottom :
				offset.X + child.ActualWidth + child.Margin.Right;
		}

		private double GetExtent(FrameworkElement child)
		{
			return ScrollOrientation == Orientation.Vertical ?
				child.ActualHeight + child.Margin.Top + child.Margin.Bottom :
				child.ActualWidth + child.Margin.Left + child.Margin.Right;
		}

		private double GetExtent(Size size) => ScrollOrientation == Orientation.Vertical ?
			size.Height :
			size.Width;

		private double GetBreadth(Size size) => ScrollOrientation == Orientation.Vertical ?
			size.Width :
			size.Height;

		private double GetActualBreadth(FrameworkElement view) => ScrollOrientation == Orientation.Vertical ?
			view.ActualWidth :
			view.ActualHeight;

		private double GetDesiredBreadth(FrameworkElement view) => GetBreadth(view.DesiredSize);

		private string GetMethodTag([CallerMemberName] string caller = null)
		{
			return $"{nameof(VirtualizingPanelLayout)}.{caller}()";
		}

		private string GetDebugInfo()
		{
			return $"Parent ItemsControl={ItemsControl} ItemsSource={ItemsControl?.ItemsSource} NoOfItems={ItemsControl?.NumberOfItems} FirstMaterialized={GetFirstMaterializedIndexPath()} LastMaterialized={GetLastMaterializedIndexPath()} ExtendedViewportStart={ExtendedViewportStart} ExtendedViewportEnd={ExtendedViewportEnd} GetItemsStart()={GetItemsStart()} GetItemsEnd()={GetItemsEnd()}";
		}

		/// <summary>
		/// Represents a single row in a vertically-scrolling panel, or a column in a horizontally-scrolling panel.
		/// </summary>
		protected class Line
		{
			public FrameworkElement[] ContainerViews { get; }
			public IndexPath FirstItem { get; }
			public IndexPath LastItem { get; }
			public int FirstItemFlat { get; }

			public FrameworkElement FirstView => ContainerViews[0];
			public FrameworkElement LastView => ContainerViews[ContainerViews.Length - 1];

			public Line(FrameworkElement[] containerViews, IndexPath firstItem, IndexPath lastItem, int firstItemFlat)
			{
				if (containerViews.Length == 0)
				{
					throw new InvalidOperationException("Line must contain at least one view");
				}

				ContainerViews = containerViews;
				FirstItem = firstItem;
				LastItem = lastItem;
				FirstItemFlat = firstItemFlat;
			}

		}
	}
}
