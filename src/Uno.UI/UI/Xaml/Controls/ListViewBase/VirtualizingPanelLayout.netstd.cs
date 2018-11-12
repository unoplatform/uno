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

		private double _lastScrollOffset;

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

		public double ViewportExtension => CacheLength * ViewportExtent * 0.5;
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
				this.Log().LogDebug($"Calling {GetMethodTag()},availableSize={availableSize}, {GetDebugInfo()}");
			}

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

			return EstimatePanelSize();
		}

		private void UpdateLayout(double extentAdjustment = 0)
		{
			UnfillLayout(extentAdjustment);
			FillLayout(extentAdjustment);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Called {GetMethodTag()}, {GetDebugInfo()} extentAdjustment={extentAdjustment}");
			}
		}

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
				var nextItem = GetNextUnmaterializedItem(GeneratorDirection.Backward, GetFirstMaterializedIndexPath());
				while (nextItem != null && GetItemsStart() > ExtendedViewportStart + extentAdjustment)
				{
					// Fill gap at start with views
					AddLine(GeneratorDirection.Backward, nextItem.Value);
					nextItem = GetNextUnmaterializedItem(GeneratorDirection.Backward, GetFirstMaterializedIndexPath());
				}
			}

			void FillForward()
			{
				var nextItem = GetNextUnmaterializedItem(GeneratorDirection.Forward, GetLastMaterializedIndexPath());
				while (nextItem != null && (GetItemsEnd() ?? 0) < ExtendedViewportEnd + extentAdjustment)
				{
					AddLine(GeneratorDirection.Forward, nextItem.Value);
					nextItem = GetNextUnmaterializedItem(GeneratorDirection.Forward, GetLastMaterializedIndexPath());
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
					for (int i = 0; i < firstMaterializedLine.ContainerViews.Length; i++)
					{
						Generator.RecycleViewForItem(firstMaterializedLine.ContainerViews[i], firstMaterializedLine.FirstItemFlat + i);
					}
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
					for (int i = 0; i < lastMaterializedLine.ContainerViews.Length; i++)
					{
						Generator.RecycleViewForItem(lastMaterializedLine.ContainerViews[i], lastMaterializedLine.FirstItemFlat + i);
					}
					_materializedLines.RemoveFromBack();
					lastMaterializedLine = GetLastMaterializedLine();
				}
			}
		}

		private Size EstimatePanelSize()
		{
			return ScrollOrientation == Orientation.Vertical ?
			new Size(AvailableBreadth, EstimatePanelExtent()) :
			new Size(EstimatePanelExtent(), AvailableBreadth);

			double EstimatePanelExtent()
			{
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
				Console.WriteLine($"{GetMethodTag()}=>{estimatedExtent}, GetContentEnd()={GetContentEnd()}, remainingLines={remainingLines}, averageLineHeight={averageLineHeight}");
				return estimatedExtent;
			}
		}

		private void OnOrientationChanged(Orientation newValue)
		{
			throw new NotImplementedException();
		}

		private IndexPath GetFirstVisibleIndexPath()
		{
			throw new NotImplementedException();
		}

		private IndexPath GetLastVisibleIndexPath()
		{
			throw new NotImplementedException();
		}

		private IEnumerable<float> GetSnapPointsInner(SnapPointsAlignment alignment)
		{
			throw new NotImplementedException();
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

			var extentOffsetAdjustment = fillDirection == GeneratorDirection.Forward ?
				0 :
				-GetExtent(view.DesiredSize);

			var topLeft = ScrollOrientation == Orientation.Vertical ?
				new Point(breadthOffset, extentOffset + extentOffsetAdjustment) :
				new Point(extentOffset + extentOffsetAdjustment, breadthOffset);

			var finalRect = new Rect(topLeft, view.DesiredSize);
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"{GetMethodTag()} finalRect={finalRect} DC={view.DataContext}");
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
