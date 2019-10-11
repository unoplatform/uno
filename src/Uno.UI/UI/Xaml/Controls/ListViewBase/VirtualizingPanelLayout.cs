#if !NET461 && !__MACOS__
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using System.Linq;
using Uno.Extensions;
using Uno.UI;
using Uno;

namespace Windows.UI.Xaml.Controls
{
	abstract partial class VirtualizingPanelLayout : IScrollSnapPointsInfo
	{
		protected enum RelativeHeaderPlacement { Inline, Adjacent }

		/// <summary>
		/// The direction of scroll.
		/// </summary>
		/// <remarks>For <see cref="ItemsStackPanel"/> layouting this is identical to <see cref="Orientation"/> but for <see cref="ItemsWrapGrid"/> it is the opposite of <see cref="Orientation"/>.</remarks>
		public abstract Orientation ScrollOrientation { get; }
#if !__WASM__
		protected readonly ILayouter _layouter = new VirtualizingPanelLayouter();
		internal ILayouter Layouter => _layouter;
#endif

#pragma warning disable 67 // Unused member
		[NotImplemented]
		public event EventHandler<object> HorizontalSnapPointsChanged;

		[NotImplemented]
		public event EventHandler<object> VerticalSnapPointsChanged;
#pragma warning restore 67 // Unused member


		private double GroupPaddingExtentStart => ScrollOrientation == Orientation.Vertical ? GroupPadding.Top : GroupPadding.Left;
		private double GroupPaddingExtentEnd => ScrollOrientation == Orientation.Vertical ? GroupPadding.Bottom : GroupPadding.Right;
		private double GroupPaddingBreadthStart => ScrollOrientation == Orientation.Vertical ? GroupPadding.Left : GroupPadding.Top;
		private double GroupPaddingBreadthEnd => ScrollOrientation == Orientation.Vertical ? GroupPadding.Right : GroupPadding.Bottom;

		public int FirstVisibleIndex => XamlParent?.GetIndexFromIndexPath(GetFirstVisibleIndexPath()) ?? -1;
		public int LastVisibleIndex => XamlParent?.GetIndexFromIndexPath(GetLastVisibleIndexPath()) ?? -1;

		/// <summary>
		/// The placement of group headers with respect to the scroll direction of the panel.
		/// </summary>
		protected RelativeHeaderPlacement RelativeGroupHeaderPlacement
		{
			get
			{
				if (ScrollOrientation == Orientation.Vertical && GroupHeaderPlacement == GroupHeaderPlacement.Top)
				{
					return RelativeHeaderPlacement.Inline;
				}

				if (ScrollOrientation == Orientation.Horizontal && GroupHeaderPlacement == GroupHeaderPlacement.Left)
				{
					return RelativeHeaderPlacement.Inline;
				}

				//The remaining two possibilities equate to Adjacent
				return RelativeHeaderPlacement.Adjacent;
			}
		}

		public bool AreHorizontalSnapPointsRegular => false;

		public bool AreVerticalSnapPointsRegular => false;

		internal SnapPointsType SnapPointsType
		{
			get
			{
				if (ScrollOrientation == Orientation.Vertical)
				{
					return XamlParent.ScrollViewer.VerticalSnapPointsType;
				}

				else
				{
					return XamlParent.ScrollViewer.HorizontalSnapPointsType;
				}
			}
		}

		internal SnapPointsAlignment SnapPointsAlignment
		{
			get
			{
				if (ScrollOrientation == Orientation.Vertical)
				{
					return XamlParent.ScrollViewer.VerticalSnapPointsAlignment;
				}

				else
				{
					return XamlParent.ScrollViewer.HorizontalSnapPointsAlignment;
				}
			}
		}

		/// <summary>
		/// Bound to <see cref="ItemsStackPanel.Orientation"/> or <see cref="ItemsWrapGrid.Orientation"/>.
		/// </summary>
		public Orientation Orientation
		{
			get { return (Orientation)GetValue(OrientationProperty); }
			set { SetValue(OrientationProperty, value); }
		}

		public static readonly DependencyProperty OrientationProperty =
			DependencyProperty.Register("Orientation", typeof(Orientation), typeof(VirtualizingPanelLayout), new PropertyMetadata(Orientation.Vertical, (o, e) => ((VirtualizingPanelLayout)o).OnOrientationChanged((Orientation)e.NewValue)));

		/// <summary>
		/// Whether the content should be stretched in breadth (ie perpendicular to the direction of scroll).
		/// </summary>
		public bool ShouldBreadthStretch
		{
			get
			{
				if (XamlParent == null)
				{
					return true;
				}

				if (ScrollOrientation == Orientation.Vertical)
				{
					return XamlParent.HorizontalAlignment == HorizontalAlignment.Stretch;
				}
				else
				{
					return XamlParent.VerticalAlignment == VerticalAlignment.Stretch;
				}
			}
		}

		public IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)
		{
			if (orientation != ScrollOrientation)
			{
				return null;
			}
			return GetSnapPointsInner(alignment).Distinct().OrderBy(f => f).ToList().AsReadOnly();
		}

		public float GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset) => throw new NotSupportedException("Regular snap points are not supported.");

		/// <summary>
		/// Get target snap point, based on scroll velocity sign, current scroll offset, and snap settings.
		/// </summary>
		/// <returns>The snap point to snap to, or null if no snapping should occur.</returns>
		internal float? GetSnapTo(float scrollVelocity, float currentScrollOffset)
		{
			if (SnapPointsType == SnapPointsType.MandatorySingle)
			{
				var snapPoints = GetIrregularSnapPoints(ScrollOrientation, SnapPointsAlignment);

				if (snapPoints.Count == 0)
				{
					return null;
				}

				var adjustedOffset = AdjustOffsetForSnapPointsAlignment(currentScrollOffset);

				// If velocity > 0, stop at the next snap point in the direction of inertia. If velocity == 0, snap to the nearest snap point.
				if (scrollVelocity == 0)
				{
					var closest = MinWithSelector(snapPoints, sp => Math.Abs(adjustedOffset - sp)).Item1;

					return closest;
				}
				else
				{
					float snapTo = 0;
					var increment = scrollVelocity > 0 ? 1 : -1;
					var start = scrollVelocity > 0 ? 0 : snapPoints.Count - 1;
					for (int i = start; i >= 0 && i < snapPoints.Count; i += increment)
					{
						//Ensure we always return a snap point to handle, eg, 'rubber banding' past the first/last snap point.
						snapTo = snapPoints[i];
						if (scrollVelocity > 0 ?
								snapTo > adjustedOffset :
								snapTo < adjustedOffset
						)
						{
							break;
						}
					}

					return snapTo;
				}
			}

			return null;
		}

		/// <summary>
		/// Get the index of the next item that has not yet been materialized in the nominated fill direction. Returns null if there are no more available items in the source.
		/// </summary>
		protected IndexPath? GetNextUnmaterializedItem(GeneratorDirection fillDirection, IndexPath? currentMaterializedItem)
		{
			return XamlParent?.GetNextItemIndex(currentMaterializedItem, fillDirection == GeneratorDirection.Forward ? 1 : -1);
		}

		// Note that Item1 is used instead of Item to work around an issue
		// in VS15.2 and its associated Roslyn issue: 
		// Uno\Uno.UI.Shared.Xamarin\UI\Xaml\Controls\ListViewBase\VirtualizingPanelLayout.cs(122,31): Error CS0570: 'EnumerableExtensions.MinBy<TSource, TComparable>(IEnumerable<TSource>, Func<TSource, TComparable>)' is not supported by the language
		//
		// This is an extract from Uno.Core d9bb6750a164f9d8a32ccf1c4527b02678595ab5 to change the method name.
		// To be removed when moving to VS15.4 and later only.
		public static (TSource Item, TComparable Value) MinWithSelector<TSource, TComparable>(IEnumerable<TSource> source, Func<TSource, TComparable> selector)
		{
			var comparer = Comparer<TComparable>.Default;

			var enumerator = source.GetEnumerator();

			if (!enumerator.MoveNext())
			{
				throw new InvalidOperationException("Source must contain at least one element.");
			}

			var minItem = enumerator.Current;
			var min = selector(minItem);

			while (enumerator.MoveNext())
			{
				var item = enumerator.Current;
				var value = selector(item);
				if (comparer.Compare(value, min) < 0)
				{
					minItem = item;
					min = value;
				}
			}

			return (minItem, min);
		}

#if !__WASM__
		private class VirtualizingPanelLayouter : Layouter
		{

			public VirtualizingPanelLayouter() : base(null)
			{

			}
			protected override string Name => "VirtualizingPanelLayout";

			protected override Size ArrangeOverride(Size finalSize)
			{
				throw new NotSupportedException($"{nameof(VirtualizingPanelLayouter)} is only used for measuring and arranging child views.");
			}

#if XAMARIN_ANDROID
			protected override void MeasureChild(Android.Views.View view, int widthSpec, int heightSpec)
			{
				view.Measure(widthSpec, heightSpec);
			}
#endif

			protected override Size MeasureOverride(Size availableSize)
			{
				throw new NotSupportedException($"{nameof(VirtualizingPanelLayouter)} is only used for measuring and arranging child views.");
			}
		}
#endif
	}
}

#endif
