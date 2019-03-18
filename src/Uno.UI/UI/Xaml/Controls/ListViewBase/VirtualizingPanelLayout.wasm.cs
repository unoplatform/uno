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
		private double _lastScrollOffset;
		private Size _lastAvailableSize;

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

			if(
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
			var fillDirection = sign > 0 ? GeneratorDirection.Forward : GeneratorDirection.Backward;

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
			var incrementView = fillDirection == GeneratorDirection.Forward ?
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
			ViewportSize = ScrollViewer?.ViewportMeasureSize ?? default;
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Calling {GetMethodTag()}, availableSize={availableSize}, _availableSize={_lastAvailableSize} {GetDebugInfo()}");
			}

			if(_lastAvailableSize != availableSize)
			{
				// Drop the current cells for now, but we need to remeasure the cells instead.
				ClearLines();
			}
			else
			{
				// Size has not changed, remeasure all items with their same sizes
				foreach(var line in _materializedLines)
				{
					foreach (var view in line.ContainerViews)
					{
						//
						// Remeasure the item will the full available width, to determine if it has changed.
						// If it has, rebuild everything. This will have to be adjusted for performance.
						//
						var slotSize = ScrollOrientation == Orientation.Vertical ?
							new Size(AvailableBreadth, double.PositiveInfinity) :
							new Size(double.PositiveInfinity, AvailableBreadth);

						var prevSize = view.DesiredSize;
						view.Measure(slotSize);

						if (this.Log().IsEnabled(LogLevel.Debug))
						{
							this.Log().LogDebug($"{GetMethodTag()} item remeasured prevSize:{prevSize} view.DesiredSize:{view.DesiredSize}");
						}

						if(prevSize != view.DesiredSize)
						{
							// Drop the current cells for now, but we need to remeasure the cells instead.
							ClearLines();
							break;
						}
					}
				}
			}

			_lastAvailableSize = availableSize;
			_availableSize = availableSize;
			UpdateLayout();

			return EstimatePanelSize();
		}

		internal Size ArrangeOverride(Size finalSize)
		{
			ViewportSize = ScrollViewer?.ViewportArrangeSize ?? default;
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Calling {GetMethodTag()}, finalSize={finalSize}, {GetDebugInfo()}");
			}

			_availableSize = finalSize;
			UpdateLayout();

			// This is a hack to propagate the "Arrange" phase to children following
			// an .InvalidateArrange() (or .InvalidateMeasure, obviously)
			foreach (var line in _materializedLines)
			{
				for (var i = 0; i < line.ContainerViews.Length; i++)
				{
					var view = line.ContainerViews[i];
					var rect = line.Rects[i];

					// Adjust the provided rect to use the current available breath
					// as the current list may have changed size.
					if(Orientation == Orientation.Horizontal)
					{
						rect.Height = AvailableBreadth;
					}
					else
					{
						rect.Width = AvailableBreadth;
					}

					// IMPORTANT: THIS HACK WON'T ALLOW THE ITEM TO CHANGE ITS SIZE, BUT
					// WILL FIX THE PROBLEM OF PROPAGATING CORRECTLY THE ARRANGE PHASE.
					view.Arrange(rect);
				}
			}

			return EstimatePanelSize();
		}

		private void UpdateLayout(double? extentAdjustment = null)
		{
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
		}

		private void UpdateCompleted() => Generator.UpdateVisibilities();

		/// <summary>
		/// Fill in extended viewport with views.
		/// </summary>
		/// <param name="extentAdjustment">Adjustment to apply when calculating fillable area.</param>
		private void FillLayout(double extentAdjustment)
		{
			FillBackward();
			FillForward();

			void FillBackward()
			{
				if (GetItemsStart() > ExtendedViewportStart + extentAdjustment)
				{
					var nextItem = GetNextUnmaterializedItem(GeneratorDirection.Backward, GetFirstMaterializedIndexPath());
					while (nextItem != null && GetItemsStart() > ExtendedViewportStart + extentAdjustment)
					{
						// Fill gap at start with views
						AddLine(GeneratorDirection.Backward, nextItem.Value);
						nextItem = GetNextUnmaterializedItem(GeneratorDirection.Backward, GetFirstMaterializedIndexPath());
					}
				}
			}

			void FillForward()
			{
				if ((GetItemsEnd() ?? 0) < ExtendedViewportEnd + extentAdjustment)
				{
					var nextItem = GetNextUnmaterializedItem(GeneratorDirection.Forward, GetLastMaterializedIndexPath());
					while (nextItem != null && (GetItemsEnd() ?? 0) < ExtendedViewportEnd + extentAdjustment)
					{
						AddLine(GeneratorDirection.Forward, nextItem.Value);
						nextItem = GetNextUnmaterializedItem(GeneratorDirection.Forward, GetLastMaterializedIndexPath());
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

		private Size EstimatePanelSize()
		{
			var extent = EstimatePanelExtent();

			var ret = ScrollOrientation == Orientation.Vertical
				? new Size(
					double.IsInfinity(AvailableBreadth) ? _materializedLines.Select(l => l.FirstView.ActualWidth).MaxOrDefault() : AvailableBreadth,
					double.IsInfinity(_availableSize.Height) ? extent : Max(extent, _availableSize.Height)
				)
				: new Size(
					double.IsInfinity(_availableSize.Width) ? extent : Max(extent, _availableSize.Width),
					double.IsInfinity(AvailableBreadth) ? _materializedLines.Select(l => l.FirstView.ActualHeight).MaxOrDefault() : AvailableBreadth
				);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()} => {extent} -> {ret} {ScrollOrientation} {_availableSize.Height} {double.IsInfinity(_availableSize.Height)} AvailableBreadth:{AvailableBreadth}");
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

		internal void Refresh()
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()}");
			}

			ClearLines();

			UpdateCompleted();
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

		private void OnOrientationChanged(Orientation newValue)
		{
			Refresh();

			//TODO: preserve scroll position
			OwnerPanel.InvalidateMeasure();
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
			var extentOffset = fillDirection == GeneratorDirection.Backward ? GetContentStart() : GetContentEnd();

			var line = CreateLine(fillDirection, extentOffset, AvailableBreadth, nextVisibleItem);

			if (fillDirection == GeneratorDirection.Backward)
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

		protected Rect AddView(FrameworkElement view, GeneratorDirection fillDirection, double extentOffset, double breadthOffset)
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

			var extentOffsetAdjustment = fillDirection == GeneratorDirection.Forward ?
				0 :
				-GetExtent(view.DesiredSize);

			var topLeft = ScrollOrientation == Orientation.Vertical ?
				new Point(breadthOffset, extentOffset + extentOffsetAdjustment) :
				new Point(extentOffset + extentOffsetAdjustment, breadthOffset);

			bool isInfiniteBreadth = double.IsInfinity(AvailableBreadth);
			var adjustedDesiredSize = ScrollOrientation == Orientation.Vertical
				? new Size(
					isInfiniteBreadth ? view.DesiredSize.Width : AvailableBreadth,
					view.DesiredSize.Height
				)
				: new Size(
					view.DesiredSize.Width,
					isInfiniteBreadth ? view.DesiredSize.Height : AvailableBreadth
				);

			var finalRect = new Rect(topLeft, adjustedDesiredSize);
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()} finalRect={finalRect} AvailableBreadth={AvailableBreadth} adjustedDesiredSize={adjustedDesiredSize} DC={view.DataContext}");
			}

			view.Arrange(finalRect);

			return finalRect;
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

			return null;
		}

		private double GetContentStart() => GetItemsStart() ?? 0;

		private double GetContentEnd() => GetItemsEnd() ?? 0;

		private double GetStart(FrameworkElement child)
		{
			var offset = child.RelativePosition;
			return ScrollOrientation == Orientation.Vertical ?
				offset.Y :
				offset.X;
		}

		private double GetEnd(FrameworkElement child)
		{
			var offset = child.RelativePosition;
			return ScrollOrientation == Orientation.Vertical ?
				offset.Y + child.ActualHeight :
				offset.X + child.ActualWidth; //TODO: include margins
		}

		private double GetExtent(FrameworkElement child)
		{
			return ScrollOrientation == Orientation.Vertical ?
				child.ActualHeight :
				child.ActualWidth; //TODO: include margins
		}

		private double GetExtent(Size size) => ScrollOrientation == Orientation.Vertical ?
			size.Height :
			size.Width;

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
			public Rect[] Rects { get; }
			public IndexPath FirstItem { get; }
			public IndexPath LastItem { get; }
			public int FirstItemFlat { get; }

			public FrameworkElement FirstView => ContainerViews[0];
			public FrameworkElement LastView => ContainerViews[ContainerViews.Length - 1];

			public Line(FrameworkElement[] containerViews, Rect[] rects, IndexPath firstItem, IndexPath lastItem, int firstItemFlat)
			{
				if (containerViews.Length == 0)
				{
					throw new InvalidOperationException("Line must contain at least one view");
				}

				if (containerViews.Length != rects.Length)
				{
					throw new InvalidOperationException("Must have same number of views and rect!");
				}
				ContainerViews = containerViews;
				Rects = rects;
				FirstItem = firstItem;
				LastItem = lastItem;
				FirstItemFlat = firstItemFlat;
			}
		}
	}
}
