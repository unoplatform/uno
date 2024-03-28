//#define USE_CUSTOM_LAYOUT_ATTRIBUTES // Use this to debug the frame of the attributes (This also has to be set in the ListViewBaseSource.iOS.cs and ItemsStackPanaleLayout.iOS.cs)
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.Disposables;
using Foundation;
using UIKit;
using CoreGraphics;
using Uno.Diagnostics.Eventing;
using Uno;
using Uno.UI.DataBinding;
using System.Linq;
using Uno.Foundation.Logging;

using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI;
using Windows.Foundation;
using ObjCRuntime;

#if USE_CUSTOM_LAYOUT_ATTRIBUTES
using _LayoutAttributes = Microsoft/* UWP don't rename */.UI.Xaml.Controls.UnoUICollectionViewLayoutAttributes;
using LayoutInfoDictionary = System.Collections.Generic.Dictionary<Foundation.NSIndexPath, Microsoft/* UWP don't rename */.UI.Xaml.Controls.UnoUICollectionViewLayoutAttributes>;
#else
using _LayoutAttributes = UIKit.UICollectionViewLayoutAttributes;
using LayoutInfoDictionary = System.Collections.Generic.Dictionary<Foundation.NSIndexPath, UIKit.UICollectionViewLayoutAttributes>;
#endif


namespace Windows.UI.Xaml.Controls
{
#if USE_CUSTOM_LAYOUT_ATTRIBUTES
	public class UnoUICollectionViewLayoutAttributes : UICollectionViewLayoutAttributes
	{
		/// <inheritdoc />
		public override CGRect Frame
		{
			get => base.Frame;
			set
			{
				
				var was = base.Frame;
				base.Frame = value;
				var index = IndexPath;

				this.Log().Debug($"////////////// ALTERING FRAME OF ATTRIBUTES: {GetHashCode():X8} {index.Section}-{index.Row} from {ToString(was)} TO {ToString(value)}");
				
				static string ToString(CGRect frame)
					=> $" {((int)frame.Width)}x{((int)frame.Height)}@{((int)frame.X)},{((int)frame.Y)}";
			}
		}
	}
#endif

	public abstract partial class VirtualizingPanelLayout : UICollectionViewLayout, DependencyObject
	{
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{17071417-C62E-4469-BD1D-981734C46E3C}");

			public const int ListViewBaseLayout_PrepareLayoutStart = 1;
			public const int ListViewBaseLayout_PrepareLayoutStop = 2;
		}

		private NativeListViewBase Owner => CollectionView as NativeListViewBase;

		private enum DirtyState
		{
			// The state of the layout hasn't changed
			None,
			// The state of the layout has been completely invalidated
			NeedsRelayout,
			// The state of the layout has been invalidated because of an INotifyCollectionChanged operation
			CollectionChanged,
			// The state of the layout has been invalidated because the scroll has changed and sticky header positions need updating
			NeedsHeaderRelayout,
			// The state of the layout has been invalidated to show provisional item positions during drag-to-reorder
			Reordering
		}

		private enum UnusedSpaceState
		{
			/// <summary>
			/// Initial state
			/// </summary>
			Unset,
			/// <summary>
			/// The panel left some available extent unused on a previous measure pass, and hasn't attempted to use it.
			/// </summary>
			HasUnusedSpace,
			/// <summary>
			/// The panel has called a relayout to try to claim unused space that was available on a previous measure.
			/// </summary>
			ConsumeUnusedSpacePending
		}

		#region Members
		/// <summary>
		/// All cached item <see cref="UICollectionViewLayoutAttributes"/>s, grouped by collection group.
		/// </summary>
		private readonly Dictionary<int, LayoutInfoDictionary> _itemLayoutInfos = new Dictionary<int, LayoutInfoDictionary>();
		/// <summary>
		/// All cached <see cref="UICollectionViewLayoutAttributes"/> for supplementary elements (Header, Footer, group headers), grouped by element type.
		/// </summary>
		private readonly Dictionary<string, LayoutInfoDictionary> _supplementaryLayoutInfos = new Dictionary<string, LayoutInfoDictionary>();
		/// <summary>
		/// The last element in the list. This is set when the layout is created (and will point to the databound size and position of 
		/// the element, once it has been materialized).
		/// </summary>
		private _LayoutAttributes _lastElement;

		/// <summary>
		/// Locations of group header frames if they were to appear inline with items (ie not 'sticky').
		/// </summary>
		private readonly Dictionary<int, CGRect> _inlineHeaderFrames = new Dictionary<int, CGRect>();
		protected readonly Dictionary<int, nfloat> _sectionEnd = new Dictionary<int, nfloat>();

		private Thickness _padding;
		private Dictionary<CachedTuple<int, int>, NSIndexPath> _indexPaths = new Dictionary<CachedTuple<int, int>, NSIndexPath>(CachedTuple<int, int>.Comparer);
		private Dictionary<CachedTuple<int, int>, _LayoutAttributes> _layoutAttributesForIndexPaths = new Dictionary<CachedTuple<int, int>, _LayoutAttributes>(CachedTuple<int, int>.Comparer);
		private DirtyState _dirtyState;
		/// <summary>
		/// The most recently returned desired size.
		/// </summary>
		private CGSize _lastReportedSize;
		/// <summary>
		/// The most recent available size given by measure.
		/// </summary>
		private CGSize _lastAvailableSize;
		/// <summary>
		/// The available size when layout was most recently recreated.
		/// </summary>
		private CGSize _lastArrangeSize;
		private bool _invalidatingHeadersOnBoundsChange;
		private bool _invalidatingOnCollectionChanged;
		private bool _invalidatingWhileReordering;
		private UnusedSpaceState _unusedSpaceState;
		private readonly Queue<CollectionChangedOperation> _pendingCollectionChanges = new Queue<CollectionChangedOperation>();
		/// <summary>
		/// Updates being applied in response to in-place collection modifications.
		/// </summary>
		private UICollectionViewUpdateItem[] _updateItems;

		private (Point Location, object Item, _LayoutAttributes LayoutAttributes)? _reorderingState;
		private NSIndexPath _reorderingDropTarget;
		/// <summary>
		/// Pre-reorder item positions, stored while applying provisional positions during drag-to-reorder
		/// </summary>
		private readonly Dictionary<NSIndexPath, CGRect> _preReorderFrames = new Dictionary<NSIndexPath, CGRect>();
		#endregion

		#region Properties

		public Thickness Padding
		{
			get { return _padding; }
			set
			{
				if (!_padding.Equals(value))
				{
					_padding = value;
					InvalidateLayout();
				}
			}
		}

		private double _itemsPresenterMinWidth;
		internal double ItemsPresenterMinWidth
		{
			get => _itemsPresenterMinWidth;
			set
			{
				_itemsPresenterMinWidth = value;
				InvalidateLayout();
			}
		}

		private double itemsPresenterMinHeight;
		internal double ItemsPresenterMinHeight
		{
			get => itemsPresenterMinHeight;
			set
			{
				itemsPresenterMinHeight = value;
				InvalidateLayout();
			}
		}

		private Size ItemsPresenterMinSize => new Size(ItemsPresenterMinWidth, ItemsPresenterMinHeight);

		private double InitialExtentPadding => ScrollOrientation == Orientation.Vertical ? Padding.Top : Padding.Left;
		private double FinalExtentPadding => ScrollOrientation == Orientation.Vertical ? Padding.Bottom : Padding.Right;
		private double InitialBreadthPadding => ScrollOrientation == Orientation.Vertical ? Padding.Left : Padding.Top;
		private double FinalBreadthPadding => ScrollOrientation == Orientation.Vertical ? Padding.Right : Padding.Bottom;


		private bool _hasDynamicItemSizes;
		/// <summary>
		/// True if at least one of the materialized elements in the collection had a size that varied from
		/// its non-databound size.
		/// </summary>
		public bool HasDynamicElementSizes
		{
			get { return _hasDynamicItemSizes; }
			set
			{
				if (value && !_hasDynamicItemSizes)
				{
					//Force a (non-dirty) layout, otherwise items may not have correct bounds when list is initially loaded.
					RefreshLayout();
				}
				_hasDynamicItemSizes = value;
			}
		}

		partial void OnAreStickyGroupHeadersEnabledChangedPartialNative(bool oldAreStickyGroupHeadersEnabled, bool newAreStickyGroupHeadersEnabled)
		{
			if (newAreStickyGroupHeadersEnabled != oldAreStickyGroupHeadersEnabled)
			{
				InvalidateLayout();
			}
		}

		internal global::System.WeakReference<ListViewBaseSource> Source
		{
			get;
			set;
		}

		private ListViewBase XamlParent => Source.GetTarget()?.Owner?.XamlParent;

		/// <summary>
		/// Whether this panel supports item view sizes which vary depending on the bound data. (Note that this doesn't 
		/// include variable databound sizes for headers, footers, and group headers, which all panels are assumed to support.)
		/// </summary>
		internal abstract bool SupportsDynamicItemSizes { get; }

		protected CGSize AvailableSize => CollectionView?.Bounds.Size ?? CGSize.Empty;
		#endregion

		public VirtualizingPanelLayout()
		{
			InitializeBinder();

			OnOrientationChanged(Orientation);
		}

		#region Overrides
		public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
		{
			var allAttributes = new List<_LayoutAttributes>();

			foreach (var cellLayoutInfo in _itemLayoutInfos.Values.Concat(_supplementaryLayoutInfos.Values))
			{
				var areItems = cellLayoutInfo.Values.FirstOrDefault()?.RepresentedElementKind == null;
				foreach (var layoutAttributes in cellLayoutInfo.Values)
				{
					// Alias to avoid paying the price of interop twice.
					var frame = layoutAttributes.Frame;

					if (SupportsDynamicItemSizes && HasDynamicElementSizes && areItems && _reorderingState is null)
					{
						//Propagate layout changes for materialized items that may have a different size to the non-databound template.
						// Note: If there is pending re-ordering (_reorderingState is not null), we don't want to update the layout attributes,
						//		 as this will cause the item to jump to the wrong position.
						//		 Anyway in that case item should already have the correct size.

						UpdateLayoutAttributesForItem(layoutAttributes, shouldRecurse: false);
					}

					if (rect.Contains(frame) ||
						rect.IntersectsWith(frame))
					{
						allAttributes.Add(layoutAttributes);
					}
				}
			}

			if (SupportsDynamicItemSizes && HasDynamicElementSizes && AreStickyGroupHeadersEnabled)
			{
				//Reapply sticky group header positions, which may have been overwritten by updating for dynamic item sizes
				UpdateHeaderPositions();
			}

			return allAttributes.ToArray();
		}

		public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath indexPath)
		{
			return _itemLayoutInfos.UnoGetValueOrDefault(indexPath.Section)?.UnoGetValueOrDefault(indexPath);
		}

		public override UICollectionViewLayoutAttributes LayoutAttributesForSupplementaryView(NSString kind, NSIndexPath indexPath)
		{
			var isHeaderOrFooter = kind == NativeListViewBase.ListViewFooterElementKindNS || kind == NativeListViewBase.ListViewHeaderElementKindNS;
			if (isHeaderOrFooter && indexPath?.Section != 0)
			{
				// We always give the Header and Footer indexPath (0,0) by convention, but in the case that a group is inserted at 0, 
				// UICollectionView can momentarily believe that the section of the header/footer has also shifted. 
				indexPath = NSIndexPath.FromRowSection(0, 0);
			}

			if (indexPath == null)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Received null index path for kind {kind}");
				}

				return null;
			}

			var layoutAttributes = _supplementaryLayoutInfos.UnoGetValueOrDefault(kind)?.UnoGetValueOrDefault(indexPath);
			if (layoutAttributes == null && this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Returning null layout attributes for {kind} at {indexPath}");
			}

			return layoutAttributes;
		}

		public override CGSize CollectionViewContentSize
		{
			get
			{
				var measured = PrepareLayoutIfNeeded(false);
				if (_lastElement != null && HasDynamicElementSizes)
				{
					if (ScrollOrientation == Orientation.Vertical)
					{
						measured = new CGSize(measured.Width, DynamicContentExtent);
					}
					else
					{
						measured = new CGSize(DynamicContentExtent, measured.Height);
					}
				}

				measured = LayoutHelper.Max(measured, ItemsPresenterMinSize);
				return measured;
			}
		}

		private nfloat DynamicContentExtent
		{
			get
			{
				if (_lastElement == null)
				{
					return 0;
				}

				return GetExtentEnd(_lastElement.Frame) + (nfloat)FinalExtentPadding;
			}
		}

		public override void PrepareLayout()
		{
			//If data reload is scheduled, call it immediately to avoid NSInternalInconsistencyException caused by supplying layoutAttributes for index paths that the list doesn't 'know about'
			if (Owner?.NeedsReloadData ?? false)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("LVBL: Calling immediate data reload");
				}

				ClearCaches();
				Owner.ReloadDataIfNeeded();
			}

			PrepareLayoutIfNeeded(true);
		}

		public override void PrepareForCollectionViewUpdates(UICollectionViewUpdateItem[] updateItems)
		{
			_updateItems = updateItems;
			base.PrepareForCollectionViewUpdates(updateItems);
		}

		public override void FinalizeCollectionViewUpdates()
		{
			var updatingVisibleItem = _updateItems?.Any(up =>
				CollectionView.IndexPathsForVisibleItems.Any(p => p.Equals(up.IndexPathBeforeUpdate) || p.Equals(up.IndexPathAfterUpdate))
			) ?? false;
			if (!updatingVisibleItem)
			{
				// If no updates are happening in view, we remove any animations. This prevents ugly 'jumps' related to changes in item size out of view.
				foreach (var cell in CollectionView.VisibleCells)
				{
					cell.Layer.RemoveAllAnimations();
				}
				foreach (var cell in Owner.VisibleSupplementaryViews)
				{
					cell.Layer.RemoveAllAnimations();
				}
			}

			_updateItems = null;
			base.FinalizeCollectionViewUpdates();
		}

		public override UICollectionViewLayoutAttributes InitialLayoutAttributesForAppearingItem(NSIndexPath itemIndexPath)
		{
			var attributes = base.InitialLayoutAttributesForAppearingItem(itemIndexPath);
			//For some reason (dynamic item sizing?) this can be called not only for appearing items but any visible item. In this case 
			// returning a value from our cache prevents glitchy animations.
			var isUpdatingItem = _updateItems?.Any(item => item.IndexPathAfterUpdate?.Equals(itemIndexPath) ?? false) ?? false;
			if (!isUpdatingItem)
			{
				var unoCachedAttributes = LayoutAttributesForItem(itemIndexPath);
				if (unoCachedAttributes != null)
				{
					attributes.Frame = unoCachedAttributes.Frame;
				}
			}

			return attributes;
		}

		public override UICollectionViewLayoutAttributes FinalLayoutAttributesForDisappearingItem(NSIndexPath itemIndexPath)
		{
			var attributes = base.FinalLayoutAttributesForDisappearingItem(itemIndexPath);
			//For some reason (dynamic item sizing?) this can be called not only for disappearing items but any visible item. In this case 
			// returning a value from our cache prevents glitchy animations.
			var isUpdatingItem = _updateItems?.Any(item => item.IndexPathBeforeUpdate?.Equals(itemIndexPath) ?? false) ?? false;
			if (!isUpdatingItem)
			{
				var unoCachedAttributes = LayoutAttributesForItem(itemIndexPath);
				if (unoCachedAttributes != null && attributes != null)
				{
					attributes.Frame = unoCachedAttributes.Frame;
				}
			}

			return attributes;
		}
		#endregion

		public CGSize SizeThatFits(CGSize size)
		{
			TrySetHasConsumedUnusedSpace();
			return PrepareLayoutIfNeeded(false, size);
		}

		/// <summary>
		/// Prepare layout if necessary and return its size.
		/// </summary>
		/// <param name="createLayoutInfo">Should we create <see cref="UICollectionViewLayoutAttributes"/>?</param>
		/// <param name="size">The available size</param>
		/// <returns>The total collection size</returns>
		/// <remarks>This is called by overridden methods which need to know the total dimensions of the panel content. If a full relayout is required,
		/// it calls <see cref="PrepareLayoutInternal(bool, bool, CGSize)"/>; otherwise it returns a cached value.</remarks>
		private CGSize PrepareLayoutIfNeeded(bool createLayoutInfo, CGSize? size = null)
		{
			using (
			   _trace.WriteEventActivity(
				   TraceProvider.ListViewBaseLayout_PrepareLayoutStart,
				   TraceProvider.ListViewBaseLayout_PrepareLayoutStop
			   )
			)
			{
				if (size.HasValue && _lastAvailableSize != size)
				{
					if (IsLayoutRequiredOnSizeChange(_lastAvailableSize, size.Value))
					{
						_dirtyState = DirtyState.NeedsRelayout;
					}

					if (!size.Value.IsEmpty)
					{
						_lastAvailableSize = size.Value;
					}
				}

				if (_dirtyState == DirtyState.NeedsRelayout || _dirtyState == DirtyState.CollectionChanged)
				{
					var availableSize = size ?? AvailableSize;
					if (createLayoutInfo)
					{
						_lastElement = null;
					}
					_lastReportedSize = PrepareLayoutInternal(createLayoutInfo, _dirtyState == DirtyState.CollectionChanged, availableSize);

					if (createLayoutInfo)
					{
						if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
						{
							this.Log().Debug("Created new layout info.");
						}

						// Clear temporary layout state
						if (_dirtyState == DirtyState.NeedsRelayout)
						{
							_hasDynamicItemSizes = false;
							_lastArrangeSize = availableSize;
						}
						_pendingCollectionChanges.Clear();
						_dirtyState = DirtyState.None;
						Owner?.SetLayoutCreated();

						if (AreStickyGroupHeadersEnabled)
						{
							UpdateHeaderPositions();
						}
					}
				}
				else if (_dirtyState == DirtyState.NeedsHeaderRelayout)
				{
					UpdateHeaderPositions();
					_dirtyState = DirtyState.None;
				}
				else if (_dirtyState == DirtyState.Reordering)
				{
					UpdateLayoutForReordering();
					_dirtyState = DirtyState.None;
				}

				if (GetExtent(_lastReportedSize) < GetExtent(_lastAvailableSize))
				{
					SetHasUnusedSpace();
				}

				return _lastReportedSize;
			}
		}

		private static readonly nfloat epsilon = (nfloat)1e-6;
		private bool IsLayoutRequiredOnSizeChange(CGSize oldAvailableSize, CGSize newAvailableSize)
		{
			var oldBreadth = GetBreadth(oldAvailableSize);
			var newBreadth = GetBreadth(newAvailableSize);
			var oldArrangeBreadth = GetBreadth(_lastArrangeSize);

			// Recalculate layout only when the breadth changes. Extent changes don't affect the desired extent reported by layouting (since 
			// items always get measured with infinite extent).
			return NMath.Abs(oldBreadth - newBreadth) > epsilon
				// If the new measure size happens to have been used for the most recent arrange, we don't need to relayout
				&& NMath.Abs(oldArrangeBreadth - newBreadth) > epsilon
				// ShouldApplyChildStretch is currently set false only for TabView - we avoid triggering a layout on size change in this case
				// because it gives the items messed-up frame offsets.
				&& ShouldApplyChildStretch
				// Skip recalculating layout for 0 size.
				&& !newAvailableSize.IsEmpty;
		}

		/// <summary>
		/// Recalculate the layout for all elements (items, header, footer, and group headers).
		/// </summary>
		/// <param name="createLayoutInfo">Should we create <see cref="UICollectionViewLayoutAttributes"/>?</param>
		/// <param name="isCollectionChanged">Is this a partial layout in response to an INotifyCollectionChanged operation</param>
		/// <param name="size">The available size</param>
		/// <returns>The total collection size</returns>
		private CGSize PrepareLayoutInternal(bool createLayoutInfo, bool isCollectionChanged, CGSize size)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"PrepareLayoutInternal() - recalculating layout, createLayoutInfo={createLayoutInfo}, dirtyState={_dirtyState}, size={size} ");
			}
			Dictionary<NSIndexPath, CGSize?> oldItemSizes = null;
			Dictionary<int, CGSize?> oldGroupHeaderSizes = null;

			// Always cache header+footer sizes; they'll be remeasured once databound.
			var oldHeaderSize = _supplementaryLayoutInfos
				.UnoGetValueOrDefault(NativeListViewBase.ListViewHeaderElementKind)?
				.FirstOrDefault()
				.Value?.Frame.Size;
			var oldFooterSize = _supplementaryLayoutInfos
				.UnoGetValueOrDefault(NativeListViewBase.ListViewFooterElementKind)?
				.FirstOrDefault()
				.Value?.Frame.Size;

			// Header/Footer can be measured by ListViewBaseInternalContainer as zero if, eg, Header is null and HeaderTemplate is set. In 
			// this case we don't want to start with zero dimension because UICollectionView seems to ignore it.
			if (oldHeaderSize?.HasZeroArea() ?? false)
			{
				oldHeaderSize = null;
			}
			if (oldFooterSize?.HasZeroArea() ?? false)
			{
				oldFooterSize = null;
			}

			(NSIndexPath Path, string Kind, nfloat Offset)? anchorItem = null;
			if (isCollectionChanged)
			{
				// We are layouting after an INotifyCollectionChanged operation(s). Cache the previous element sizes, under their new index 
				// paths, so we can reuse them in order not to have to lay out elements with different databound sizes with their static size.
				oldItemSizes = _itemLayoutInfos.SelectMany(kvp => kvp.Value)
					.ToDictionaryKeepLast(
						kvp => OffsetIndexForPendingChanges(kvp.Key, NativeListViewBase.ListViewItemElementKind),
						kvp => (CGSize?)kvp.Value.Size
					);
				oldGroupHeaderSizes = _supplementaryLayoutInfos
					.UnoGetValueOrDefault(NativeListViewBase.ListViewSectionHeaderElementKind)?
					.ToDictionaryKeepLast(
						kvp => OffsetIndexForPendingChanges(kvp.Key, NativeListViewBase.ListViewSectionHeaderElementKind).Section,
						kvp => (CGSize?)kvp.Value.Size
					);

				anchorItem = FindAnchorItem();
			}

			if (createLayoutInfo)
			{
				//0. Clear previous layout attributes
				_itemLayoutInfos.Clear();
				_supplementaryLayoutInfos.Clear();
			}

			var xamlParent = XamlParent;
			if (xamlParent == null)
			{
				return CGSize.Empty;
			}

			nfloat measuredBreadth = 0;
			var availableBreadth = GetBreadth(size) - (nfloat)InitialBreadthPadding - (nfloat)FinalBreadthPadding;
			var isGrouping = xamlParent.IsGrouping;
			var frame = CGRect.Empty;
			IncrementExtentBy(ref frame, (nfloat)InitialExtentPadding);
			IncrementBreadthBy(ref frame, (nfloat)InitialBreadthPadding);

			if (xamlParent.ShouldShowHeader)
			{
				//1. Layout header at start
				frame.Size = oldHeaderSize ?? GetHeaderSize(size);
				//Give the maximum breadth available, since for now we don't adjust the measured width of the list based on the databound item
				SetBreadth(ref frame, availableBreadth);
				if (createLayoutInfo)
				{
					CreateSupplementaryElementLayoutInfo(0, 0, NativeListViewBase.ListViewHeaderElementKindNS, frame);
				}
				IncrementExtent(ref frame);
				measuredBreadth = NMath.Max(measuredBreadth, GetBreadthEnd(frame));
			}

			var sections = CollectionView.NumberOfSections() - ListViewBaseSource.SupplementarySections;
			for (int section = 0; section < sections; section++)
			{
				var availableGroupBreadth = availableBreadth;
				var groupBreadthStart = GetBreadthStart(frame);
				var groupExtentStart = GetExtentStart(frame);
				nfloat groupHeaderExtent = 0;
				nfloat groupHeaderBreadth = 0;
				//2. Layout each group
				if (isGrouping)
				{
					IncrementExtentBy(ref frame, (nfloat)GroupPaddingExtentStart);
					IncrementBreadthBy(ref frame, (nfloat)GroupPaddingBreadthStart);
					availableGroupBreadth -= (nfloat)GroupPaddingBreadthStart;
					availableGroupBreadth -= (nfloat)GroupPaddingBreadthEnd;

					//a. Layout group header, if present
					frame.Size = oldGroupHeaderSizes?.UnoGetValueOrDefault(section) ?? GetSectionHeaderSize(section, size);
					if (RelativeGroupHeaderPlacement != RelativeHeaderPlacement.Adjacent)
					{
						//Give the maximum breadth available, since for now we don't adjust the measured width of the list based on the databound item
						SetBreadth(ref frame, availableBreadth);
					}

					groupHeaderExtent = GetExtent(frame.Size);
					groupHeaderBreadth = GetBreadth(frame.Size);
					if (createLayoutInfo)
					{
						CreateSupplementaryElementLayoutInfo(0, section, NativeListViewBase.ListViewSectionHeaderElementKindNS, frame);
						_inlineHeaderFrames[section] = frame;
					}
					if (RelativeGroupHeaderPlacement == RelativeHeaderPlacement.Inline)
					{
						IncrementExtent(ref frame);
					}
					else
					{
						IncrementBreadth(ref frame);
						availableGroupBreadth -= GetBreadth(frame.Size);
					}
				}

				//Ensure container for group exists even if group contains no items, to simplify subsequent logic
				if (createLayoutInfo)
				{
					_itemLayoutInfos[section] = new Dictionary<NSIndexPath, _LayoutAttributes>();
				}
				//b. Layout items in group
				var itemsBreadth = LayoutItemsInGroup(section, availableGroupBreadth, ref frame, createLayoutInfo, oldItemSizes);
				var groupBreadth = itemsBreadth;
				if (RelativeGroupHeaderPlacement == RelativeHeaderPlacement.Adjacent)
				{
					groupBreadth += groupHeaderBreadth;
				}
				else
				{
					groupBreadth = NMath.Max(groupBreadth, groupHeaderBreadth);
				}

				measuredBreadth = NMath.Max(measuredBreadth, groupBreadth);

				if (isGrouping)
				{
					IncrementExtentBy(ref frame, (nfloat)GroupPaddingExtentEnd);
					//Reset breadth in case it has been incremented by an adjacent group header
					SetBreadthStart(ref frame, groupBreadthStart);
					if (RelativeGroupHeaderPlacement == RelativeHeaderPlacement.Adjacent && GetExtentStart(frame) - groupExtentStart < groupHeaderExtent)
					{
						//Ensure we account for the extent of the group header if it is adjacent and it takes up more extent than the items in the group
						IncrementExtentBy(ref frame, groupExtentStart + groupHeaderExtent - GetExtentStart(frame));
					}
				}
			}

			if (xamlParent.ShouldShowFooter)
			{
				//3. Layout footer 
				frame.Size = oldFooterSize ?? GetFooterSize(size);
				//Give the maximum breadth available, since for now we don't adjust the measured width of the list based on the databound item
				SetBreadth(ref frame, availableBreadth);
				if (createLayoutInfo)
				{
					CreateSupplementaryElementLayoutInfo(0, 0, NativeListViewBase.ListViewFooterElementKindNS, frame);
				}
				IncrementExtent(ref frame);
				measuredBreadth = NMath.Max(measuredBreadth, GetBreadthEnd(frame));
			}

			//Apply anchor if doing a partial relayout
			if (anchorItem?.Path != null && CollectionView != null)
			{
				var newAnchor = _itemLayoutInfos.UnoGetValueOrDefault(anchorItem.Value.Path.Section)?.UnoGetValueOrDefault(anchorItem.Value.Path);
				if (newAnchor == null)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"No anchor found after layout at {anchorItem.Value.Path}");
					}
				}
				else
				{
					var newOffset = GetExtentStart(newAnchor.Frame) + anchorItem.Value.Offset;
					CollectionView.ContentOffset = SetExtentOffset(CollectionView.ContentOffset, newOffset);
				}
			}

			IncrementExtentBy(ref frame, (nfloat)FinalExtentPadding);

			//Measured size: the current start of the frame is the extent, + measured breadth
			return ScrollOrientation == Orientation.Vertical ?
				new CGSize(measuredBreadth, frame.Top) :
				new CGSize(frame.Left, measuredBreadth);
		}

		/// <summary>
		/// Get item which should act as an 'anchor' during a partial relayout of the list. This is the item closest to the top of the 
		/// viewport that isn't being removed or replaced.
		/// </summary>
		/// <returns>The index path of the item, its element Kind, and its offset relative to the current scroll position.</returns>
		private (NSIndexPath, string, nfloat) FindAnchorItem()
		{
			if (CollectionView == null)
			{
				return default((NSIndexPath, string, nfloat));
			}

			var scrollOffset = GetExtent(CollectionView.ContentOffset);

			if (scrollOffset == 0)
			{
				// UWP behavior is to keep the scroll offset 0 if it's 0.
				return default((NSIndexPath, string, nfloat));
			}
			var anchor = _itemLayoutInfos
				.SelectMany(kvp => kvp.Value.Values)
				.FirstOrDefault(layout =>
				{
					// RepresentedElementKind is null for items
					var kind = layout.RepresentedElementKind ?? NativeListViewBase.ListViewItemElementKind;
					var indexPath = OffsetIndexForPendingChanges(layout.IndexPath, kind);
					return GetExtentEnd(layout.Frame) > scrollOffset &&
					// Exclude items that are being removed or replaced. (These items are set to row or section of int.MaxValue / 2, but 
					// subsequent insertions/deletions mean they might no longer be exactly equal.)
					indexPath.Row < int.MaxValue / 4 &&
					indexPath.Section < int.MaxValue / 4;
				});

			if (anchor == null)
			{
				return default((NSIndexPath, string, nfloat));
			}

			var offset = scrollOffset - GetExtentStart(anchor.Frame);

			var offsetPath = OffsetIndexForPendingChanges(anchor.IndexPath, anchor.RepresentedElementKind ?? NativeListViewBase.ListViewItemElementKind);

			return (offsetPath, anchor.RepresentedElementKind, offset);
		}

		/// <summary>
		/// Lay out all items in a single group, according to the layouting of the panel.
		/// </summary>
		/// <param name="group">The group to lay out.</param>
		/// <param name="availableBreadth">The breadth available for items.</param>
		/// <param name="frame">The item frame. The initial position is the position the first item should use, and the final position should
		/// be the position for the next element after this group to use.</param>
		/// <param name="createLayoutInfo">Should we create <see cref="UICollectionViewLayoutAttributes"/>?</param>
		/// <param name="oldItemSizes">If this is a layout in response to an INotifyCollectionChanged operation, this will contain the old item sizes for reuse; otherwise it will be null.</param>
		/// <returns>The measured breadth of this group.</returns>
		protected abstract nfloat LayoutItemsInGroup(int group, nfloat availableBreadth, ref CGRect frame, bool createLayoutInfo, Dictionary<NSIndexPath, CGSize?> oldItemSizes);

		private void CreateSupplementaryElementLayoutInfo(int row, int section, NSString kind, CGRect frame)
		{
			var container = _supplementaryLayoutInfos.FindOrCreate(kind, () => new LayoutInfoDictionary());
			var indexPath = GetNSIndexPathFromRowSection(row, section);
			var layout = UICollectionViewLayoutAttributes.CreateForSupplementaryView<_LayoutAttributes>(kind, indexPath);
			layout.Frame = frame;
			container[indexPath] = layout;
			_lastElement = layout;
		}

		protected void CreateItemLayoutInfo(int row, int section, CGRect frame)
		{
			var layout = (_LayoutAttributes)GetLayoutAttributesForIndexPath(row, section);
			layout.Frame = frame;
			var container = _itemLayoutInfos.FindOrCreate(section, () => new LayoutInfoDictionary());
			container[GetNSIndexPathFromRowSection(row, section)] = layout;
			_lastElement = layout;
		}

		/// <summary>
		/// Adjust the index of cached layouts to account for INotifyCollectionChanged operations.
		/// </summary>
		/// <param name="oldIndex">The index prior to pending operations.</param>
		/// <param name="kind">The type of element (can be either item or group header).</param>
		/// <returns>The new index of the element after pending operations are applied.</returns>
		private NSIndexPath OffsetIndexForPendingChanges(NSIndexPath oldIndex, string kind)
		{
			var section = oldIndex.Section;
			var row = oldIndex.Row;
			foreach (var change in _pendingCollectionChanges)
			{
				switch (change)
				{
					case var groupAdd when groupAdd.ElementType == CollectionChangedOperation.Element.Group &&
							groupAdd.Action == NotifyCollectionChangedAction.Add &&
							groupAdd.EndIndex.Section <= section:
						section += groupAdd.Range;
						break;

					case var groupRemove when groupRemove.ElementType == CollectionChangedOperation.Element.Group &&
							groupRemove.Action == NotifyCollectionChangedAction.Remove &&
							groupRemove.EndIndex.Section < section:
						section -= groupRemove.Range;
						break;

					case var thisGroupRemoved when thisGroupRemoved.ElementType == CollectionChangedOperation.Element.Group &&
							(thisGroupRemoved.Action == NotifyCollectionChangedAction.Remove || thisGroupRemoved.Action == NotifyCollectionChangedAction.Replace) &&
							thisGroupRemoved.StartingIndex.Section <= section && thisGroupRemoved.EndIndex.Section >= section:
						// This element's group has been removed or replaced, this ensures its size won't be reused.
						section = PutOutOfRange(section);
						break;

					case var itemAdd when itemAdd.ElementType == CollectionChangedOperation.Element.Item &&
							// Group header indices are unaffected by item-level operations
							kind == NativeListViewBase.ListViewItemElementKind &&
							itemAdd.Action == NotifyCollectionChangedAction.Add &&
							itemAdd.StartingIndex.Section == section &&
							itemAdd.EndIndex.Row <= row:
						row += itemAdd.Range;
						break;

					case var itemRemove when itemRemove.ElementType == CollectionChangedOperation.Element.Item &&
							// Group header indices are unaffected by item-level operations
							kind == NativeListViewBase.ListViewItemElementKind &&
							itemRemove.Action == NotifyCollectionChangedAction.Remove &&
							itemRemove.StartingIndex.Section == section &&
							itemRemove.EndIndex.Row < row:
						row -= itemRemove.Range;
						break;

					case var thisItemRemoved when thisItemRemoved.ElementType == CollectionChangedOperation.Element.Item &&
							// Group header indices are unaffected by item-level operations
							kind == NativeListViewBase.ListViewItemElementKind &&
							(thisItemRemoved.Action == NotifyCollectionChangedAction.Remove || thisItemRemoved.Action == NotifyCollectionChangedAction.Replace) &&
							thisItemRemoved.StartingIndex.Section == section &&
							thisItemRemoved.StartingIndex.Row <= row && thisItemRemoved.EndIndex.Row >= row:
						// This item has been removed or replaced, this ensures its size won't be reused.
						section = PutOutOfRange(section);
						break;
				}
			}

			return GetNSIndexPathFromRowSection(row, section);
		}

		/// <summary>
		/// Set value to a very large number to ensure it isn't eligible for reuse.
		/// </summary>
		private static int PutOutOfRange(int value)
		{
			// We don't use 
			const int veryLarge = int.MaxValue / 2;

			if (value < veryLarge / 2)
			{
				value += veryLarge;
			}

			return value;
		}

		/// <summary>
		/// Apply 'stickiness' to group header positions.
		/// </summary>
		private void UpdateHeaderPositions()
		{
			// Get coordinate index to modify
			var axisIndex = ScrollOrientation == Orientation.Horizontal ?
				0 :
				1;
			var offset = CollectionView.ContentOffset.GetXOrY(axisIndex);
			Dictionary<NSIndexPath, _LayoutAttributes> headerAttributes;
			if (_supplementaryLayoutInfos.TryGetValue(NativeListViewBase.ListViewSectionHeaderElementKind, out headerAttributes))
			{
				foreach (var kvp in headerAttributes)
				{
					var layoutAttributes = kvp.Value;

					var section = kvp.Key.Section;

					//1. Start with frame if header were inline
					var frame = _inlineHeaderFrames[section];

					//2. If frame would be out of bounds, bring it just in bounds
					nfloat frameOffset = frame.GetXOrY(axisIndex);
					if (frameOffset < offset)
					{
						frameOffset = offset;
					}

					//3. If frame base would be below base of lowest element in section, bring it just above lowest element in section
					var sectionMin = _sectionEnd[section] - frame.GetWidthOrHeight(axisIndex);
					if (frameOffset > sectionMin)
					{
						frameOffset = sectionMin;
					}

					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug) && frameOffset != (axisIndex == 0 ? frame.X : frame.Y))
					{
						this.Log().Debug($"Sticky group header adjustment: offsetting header for group {section} by {frameOffset - (axisIndex == 0 ? frame.X : frame.Y)}");
					}

					layoutAttributes.Frame = frame.SetXOrY(axisIndex, frameOffset);
					//Ensure headers appear above elements
					layoutAttributes.ZIndex = 1;
				}
			}
		}

		private protected CGSize GetItemSizeForIndexPath(NSIndexPath indexPath, nfloat availableBreadth)
		{
			return Source.GetTarget()?.GetItemSize(CollectionView, indexPath, GetAvailableChildSize(availableBreadth)) ?? CGSize.Empty;
		}

		private CGSize GetHeaderSize(CGSize availableViewportSize)
		{
			return Source.GetTarget()?.GetHeaderSize(GetAvailableChildSize(availableViewportSize)) ?? CGSize.Empty;
		}

		private CGSize GetFooterSize(CGSize availableViewportSize)
		{
			return Source.GetTarget()?.GetFooterSize(GetAvailableChildSize(availableViewportSize)) ?? CGSize.Empty;
		}

		private CGSize GetSectionHeaderSize(int section, CGSize availableViewportSize)
		{
			return Source.GetTarget()?.GetSectionHeaderSize(section, GetAvailableChildSize(availableViewportSize)) ?? CGSize.Empty;
		}

		/// <summary>
		/// Gets the CollectionView that owns this layout.
		/// </summary>
		/// <remarks>
		/// This property is present to avoid the interop cost, even at
		/// the expense of WeakReference dereference.
		/// </remarks>
		public new UICollectionView CollectionView => Source?.GetTarget()?.Owner ?? base.CollectionView;

		public override bool ShouldInvalidateLayoutForBoundsChange(CGRect newBounds)
		{
			//Invalidate if collection bounds have changed in breadth
			if (IsLayoutRequiredOnSizeChange(CollectionView.Bounds.Size, newBounds.Size))
			{
				return true;
			}

			//Set flag and invalidate if sticky headers are enabled
			if (AreStickyGroupHeadersEnabled)
			{
				_invalidatingHeadersOnBoundsChange = true;
				return true;
			}

			//Don't invalidate
			return false;
		}

		/// <summary>
		/// Triggers an update of the sticky group header positions.
		/// </summary>
		public void UpdateStickyHeaderPositions()
		{
			if (AreStickyGroupHeadersEnabled)
			{
				_invalidatingHeadersOnBoundsChange = true;
				InvalidateLayout();
			}
		}

		public override void InvalidateLayout()
		{
			//Called in response to INotifyCollectionChanged operation, update layout reusing already-calculated databound element sizes
			if (_invalidatingOnCollectionChanged)
			{
				_invalidatingOnCollectionChanged = false;
				if (_dirtyState != DirtyState.NeedsRelayout)
				{
					_dirtyState = DirtyState.CollectionChanged;
				}
			}
			//Called from scrolling, update sticky headers
			else if (_invalidatingHeadersOnBoundsChange)
			{
				_invalidatingHeadersOnBoundsChange = false;
				if (_dirtyState == DirtyState.None)
				{
					_dirtyState = DirtyState.NeedsHeaderRelayout;
				}
			}
			// Called while dragging to reorder, apply provisional item positions
			else if (_invalidatingWhileReordering)
			{
				_invalidatingWhileReordering = false;
				if (_dirtyState == DirtyState.None)
				{
					_dirtyState = DirtyState.Reordering;
				}
			}
			//Called for some other reason, update everything
			else
			{
				_dirtyState = DirtyState.NeedsRelayout;
			}
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Invalidating layout with dirty state={_dirtyState}");
			}
			base.InvalidateLayout();
		}

		/// <summary>
		/// Notify the layout that an INotifyCollectionChanged operation is pending. (Note: the UICollectionView already implements 
		/// PrepareForCollectionViewUpdates() which is essentially the same thing; unfortunately it is called *after* 
		/// InvalidateLayout+PrepareLayout, and is therefore useless for us.)
		/// </summary>
		internal void NotifyCollectionChange(CollectionChangedOperation change)
		{
			_invalidatingOnCollectionChanged = true;
			_pendingCollectionChanges.Enqueue(change);

			if (change.Action == NotifyCollectionChangedAction.Add && DidLeaveUnusedSpace())
			{
				// If we're adding an item and the previous layout didn't use all available space, we probably need more space.
				PropagateUnusedSpaceRelayout();
			}
			else if (change.Action == NotifyCollectionChangedAction.Remove)
			{
				NeedsRelayout();
			}
		}

		/// <summary>
		/// Request a new layout without completely recreating layout attributes for each item.
		/// </summary>
		public void RefreshLayout()
		{
			if (_dirtyState == DirtyState.NeedsRelayout && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("Trying to refresh layout when a full recreate is required - this will cause unexpected behaviour.");
			}

			//Calling base avoids setting the _dirtyState, avoiding internal layout recalculations.
			base.InvalidateLayout();
		}

		/// <summary>
		/// Provides a NSIndexPath for the current ListViewSource.
		/// </summary>
		/// <remarks>
		/// Use this method instead of NSIndexPath.FromRowSection, as the interop call
		/// is quite costly.
		/// </remarks>
		protected NSIndexPath GetNSIndexPathFromRowSection(int row, int section)
		{
			NSIndexPath indexPath;
			var key = CachedTuple.Create(row, section);

			if (!_indexPaths.TryGetValue(key, out indexPath))
			{
				_indexPaths.Add(key, indexPath = NSIndexPath.FromRowSection(row, section));
			}

			return indexPath;
		}

		/// <summary>
		/// Provides a UICollectionViewLayoutAttributes for the current ListViewSource.
		/// </summary>
		/// <remarks>
		/// Use this method instead of UICollectionViewLayoutAttributes.CreateForCell, as the interop call
		/// is quite costly.
		/// </remarks>
		protected UICollectionViewLayoutAttributes GetLayoutAttributesForIndexPath(int row, int section)
		{
			var key = CachedTuple.Create(row, section);
			_LayoutAttributes attributes;

			if (!_layoutAttributesForIndexPaths.TryGetValue(key, out attributes))
			{
				var indexPath = GetNSIndexPathFromRowSection(row, section);
				_layoutAttributesForIndexPaths.Add(key, attributes = UICollectionViewLayoutAttributes.CreateForCell<_LayoutAttributes>(indexPath));
			}

			return attributes;
		}

		private void ClearCaches()
		{
			_indexPaths.Clear();
			_layoutAttributesForIndexPaths.Clear();
		}

		internal void ReloadData()
		{
			// Calling ClearCaches is required because UIKit
			// seems to release the UICollectionViewLayoutAttributes
			// if the data is reloaded completely. This can lead to having 
			// InternalContainer.ApplyLayoutAttributes being called with an
			// instance that is not of a type UICollectionViewLayoutAttributes, 
			// and raise an exception like this one: 

			//		Foundation.MonoTouchException: Objective-C exception thrown. 
			//		Name: NSInvalidArgumentException Reason: 
			//		-[UICollectionViewLayoutAttributes nextResponder]: unrecognized selector sent to instance 0x7de0a6e0

			ClearCaches();
		}

		private void OnOrientationChanged(Orientation newValue)
		{
			InvalidateLayout();
		}

		/// <summary>
		/// Update cached layout attributes for an element after the materialized, databound element has been measured.
		/// </summary>
		public void UpdateLayoutAttributesForElement(_LayoutAttributes layoutAttributes)
		{
			//Update frame of target layoutAttributes
			var identifier = layoutAttributes.RepresentedElementKind;
			var indexPath = layoutAttributes.IndexPath;
			var cachedAttributes = identifier == null ? LayoutAttributesForItem(indexPath) : _supplementaryLayoutInfos[identifier][indexPath];
			var extentDifference = GetExtent(layoutAttributes.Frame.Size - cachedAttributes.Frame.Size);

			AdjustScrollOffset(extentDifference, GetExtentEnd(cachedAttributes.Frame));

			cachedAttributes.Frame = layoutAttributes.Frame;

			if ((identifier == NativeListViewBase.ListViewHeaderElementKind || identifier == NativeListViewBase.ListViewSectionHeaderElementKind))
			{
				var sectionHeaderLayouts = _supplementaryLayoutInfos.UnoGetValueOrDefault(NativeListViewBase.ListViewSectionHeaderElementKind);
				if (sectionHeaderLayouts != null)
				{
					//Update position for this and subsequent groups
					foreach (var kvp in sectionHeaderLayouts)
					{
						var groupHeaderLayout = kvp.Value;
						if (groupHeaderLayout.IndexPath.Section >= layoutAttributes.IndexPath.Section)
						{
							UpdateLayoutAttributesForGroupHeader(groupHeaderLayout, extentDifference, groupHeaderLayout != cachedAttributes);
						}
					}
				}
				else //No group headers, ie this is an update for the list header
				{
					//Offset all items
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Applying offset of {extentDifference} to all items");
					}
					foreach (var kvp in _itemLayoutInfos.SelectMany(kvp2 => kvp2.Value))
					{
						var itemLayout = kvp.Value;
						itemLayout.Frame = AdjustExtentOffset(itemLayout.Frame, extentDifference);
					}
				}
				var footerLayout = _supplementaryLayoutInfos.UnoGetValueOrDefault(NativeListViewBase.ListViewFooterElementKind)?.UnoGetValueOrDefault(GetNSIndexPathFromRowSection(0, 0));
				if (footerLayout != null)
				{
					footerLayout.Frame = AdjustExtentOffset(footerLayout.Frame, extentDifference);
				}
			}

			if (layoutAttributes.RepresentedElementKind == NativeListViewBase.ListViewSectionHeaderElementKind)
			{
				var inlineFrame = _inlineHeaderFrames[layoutAttributes.IndexPath.Section];
				inlineFrame.Size = layoutAttributes.Frame.Size;
				_inlineHeaderFrames[layoutAttributes.IndexPath.Section] = inlineFrame;
			}

			if (SupportsDynamicItemSizes && layoutAttributes.RepresentedElementKind == null)
			{
				UpdateLayoutAttributesForItem(layoutAttributes, shouldRecurse: true);
			}

			// If this pushes content size bigger than viewport, and we 'left money on the table' when we requested desired size, send up a layout request
			CheckDesiredSizeChanged();
		}

		private void CheckDesiredSizeChanged()
		{
			var lastReported = GetExtent(_lastReportedSize);
			if (DidLeaveUnusedSpace() && DynamicContentExtent > lastReported)
			{
				SetExtent(ref _lastReportedSize, DynamicContentExtent);

				PropagateUnusedSpaceRelayout();
			}
		}

		/// <summary>
		/// We left space 'on the table' in a previous measure+arrange and now we need more, propagate a layout request.
		/// </summary>
		private void PropagateUnusedSpaceRelayout()
		{
			SetWillConsumeUnusedSpace();
			NeedsRelayout();
		}

		internal void NeedsRelayout()
		{
			var nativePanel = CollectionView as NativeListViewBase;

			nativePanel.SetNeedsLayout();
			// NativeListViewBase swallows layout requests by design
			nativePanel.SetSuperviewNeedsLayout();
		}

		/// <summary>
		/// A previous measure pass left unused space, which hasn't been consumed yet.
		/// </summary>
		private bool DidLeaveUnusedSpace() => _unusedSpaceState != UnusedSpaceState.Unset;

		/// <summary>
		/// The panel is returning a smaller desired extent than the available size (ie the current items at their currently-known dimensions don't fill the entire available size).
		/// </summary>
		private void SetHasUnusedSpace() => _unusedSpaceState = UnusedSpaceState.HasUnusedSpace;

		/// <summary>
		/// An update to the dynamic (ie databound) size of an item is attempting to consume unused space. The result will transpire on a subsequent measure pass.
		/// </summary>
		private void SetWillConsumeUnusedSpace() => _unusedSpaceState = UnusedSpaceState.ConsumeUnusedSpacePending;

		/// <summary>
		/// Called on measure pass to indicate that a request to consume unused space has been processed.
		/// </summary>
		private void TrySetHasConsumedUnusedSpace()
		{
			if (_unusedSpaceState == UnusedSpaceState.ConsumeUnusedSpacePending)
			{
				_unusedSpaceState = UnusedSpaceState.Unset;
			}
		}

		/// <summary>
		/// Adjust the scroll offset of the list so that layout updates out of view don't affect the position of visible items. 
		/// </summary>
		private void AdjustScrollOffset(nfloat extentDifference, nfloat frameEnd)
		{
			if (CollectionView == null)
			{
				return;
			}

			var offset = CollectionView.ContentOffset;

			if (frameEnd < GetExtent(offset))
			{
				CollectionView.ContentOffset = AdjustExtentOffset(offset, extentDifference);
			}
		}

		protected void UpdateLayoutAttributesForGroupHeader(_LayoutAttributes groupHeaderLayout, nfloat extentDifference, bool applyOffsetToThis)
		{
			if (applyOffsetToThis)
			{
				//Update group header, if it's not the one that triggered the update
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Applying offset of {extentDifference} to group header for section {groupHeaderLayout.IndexPath.Section}");
				}
				groupHeaderLayout.Frame = AdjustExtentOffset(groupHeaderLayout.Frame, extentDifference);

				_inlineHeaderFrames[groupHeaderLayout.IndexPath.Section] = AdjustExtentOffset(_inlineHeaderFrames[groupHeaderLayout.IndexPath.Section], extentDifference);
			}
			//Update all items in this group
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Applying offset of {extentDifference} to each item in section {groupHeaderLayout.IndexPath.Section}");
			}
			foreach (var kvpItem in _itemLayoutInfos[groupHeaderLayout.IndexPath.Section])
			{
				var itemLayout = kvpItem.Value;
				itemLayout.Frame = AdjustExtentOffset(itemLayout.Frame, extentDifference);
			}

			//Update dimensions for sticky header layouting
			_sectionEnd[groupHeaderLayout.IndexPath.Section] += extentDifference;
		}

		private protected virtual void UpdateLayoutAttributesForItem(_LayoutAttributes layoutAttributes, bool shouldRecurse)
		{
			throw new NotSupportedException($"This should be overridden by types which set {nameof(SupportsDynamicItemSizes)} to true.");
		}

		private IEnumerable<float> GetSnapPointsInner(SnapPointsAlignment alignment)
		{
			return GetAllElementLayouts().Select(l => GetSnapPoint(l.Frame, alignment));
		}

		/// <summary>
		/// Get the snap point corresponding to a given layout frame and alignment, in the current scroll orientation.
		/// </summary>
		private float GetSnapPoint(CGRect frame, SnapPointsAlignment alignment)
		{
			nfloat snapPoint;
			switch (alignment)
			{
				case SnapPointsAlignment.Near:
					snapPoint = GetExtentStart(frame);
					break;
				case SnapPointsAlignment.Center:
					snapPoint = (GetExtentEnd(frame) + GetExtentStart(frame)) / 2;
					break;
				case SnapPointsAlignment.Far:
					snapPoint = GetExtentEnd(frame);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(alignment));
			}

			return (float)snapPoint;
		}

		/// <summary>
		/// Get the snap point to apply, if any, when a dragging gesture ends.
		/// </summary>
		internal CGPoint? GetSnapTo(CGPoint velocity, CGPoint targetContentOffset)
		{
			var scrollVelocity = (float)GetExtent(velocity);
			var currentOffset = (float)GetExtent(Owner.ContentOffset);

			var snapTo = GetSnapTo(scrollVelocity, currentOffset);

			return snapTo.HasValue ? GetSnapToAsOffset(snapTo.Value) : (CGPoint?)null;
		}

		/// <summary>
		/// Convert target snap point to ContentOffset (which always corresponds to the top of the visible viewport).
		/// </summary>
		private CGPoint GetSnapToAsOffset(float closest)
		{
			nfloat contentOffset;
			switch (SnapPointsAlignment)
			{
				case SnapPointsAlignment.Near:
					contentOffset = closest;
					break;
				case SnapPointsAlignment.Center:
					contentOffset = closest - GetExtent(CollectionView.Bounds.Size) / 2;
					break;
				case SnapPointsAlignment.Far:
					contentOffset = closest - GetExtent(CollectionView.Bounds.Size);
					break;
				default:
					throw new InvalidOperationException($"{SnapPointsAlignment} out of range.");
			}

			return SetExtentOffset(CGPoint.Empty, contentOffset);
		}

		/// <summary>
		/// Apply alignment to ContentOffset.
		/// </summary>
		private float AdjustOffsetForSnapPointsAlignment(nfloat contentOffset)
		{
			switch (SnapPointsAlignment)
			{
				case SnapPointsAlignment.Near:
					return (float)contentOffset;
				case SnapPointsAlignment.Center:
					return (float)(contentOffset + GetExtent(CollectionView.Bounds.Size) / 2);
				case SnapPointsAlignment.Far:
					return (float)(contentOffset + GetExtent(CollectionView.Bounds.Size));
				default:
					throw new InvalidOperationException($"{SnapPointsAlignment} out of range.");
			}
		}

		/// <summary>
		/// Get offset to apply to scroll item into view
		/// </summary>
		internal CGPoint GetTargetScrollOffset(NSIndexPath item, ScrollIntoViewAlignment alignment)
		{
			var baseOffsetExtent = GetBaseOffset();
			var snapTo = GetSnapTo(scrollVelocity: 0, (float)baseOffsetExtent);
			if (snapTo.HasValue)
			{
				return GetSnapToAsOffset(snapTo.Value);
			}
			else
			{
				return SetExtentOffset(CGPoint.Empty, baseOffsetExtent);
			}

			nfloat GetBaseOffset()
			{
				var frame = LayoutAttributesForItem(item).Frame;
				var headerCorrection = GetStickyHeaderExtent(item.Section);
				var frameExtentStart = GetExtentStart(frame) - headerCorrection;
				if (alignment == ScrollIntoViewAlignment.Leading)
				{
					// Alignment=Leading snaps item to same position no matter where it currently is
					return frameExtentStart;
				}
				else // Alignment=Default
				{
					var currentOffset = GetExtent(Owner.ContentOffset);
					var targetOffset = currentOffset;
					var frameExtentEnd = GetExtentEnd(frame);
					var viewportHeight = GetExtent(CollectionView.Bounds.Size);

					if (frameExtentStart < currentOffset)
					{
						// Start of item is above viewport, it should snap to start of viewport
						targetOffset = frameExtentStart;
					}
					if (frameExtentEnd > currentOffset + viewportHeight)
					{
						//End of item is below viewport, it should snap to end of viewport
						targetOffset = frameExtentEnd - viewportHeight;
					}
					// (If neither of the conditions apply, item is already fully in view, return current offset unaltered)

					return targetOffset;
				}
			}
		}

		/// <summary>
		/// Get extent added by sticky group header for given section, if any.
		/// </summary>
		private nfloat GetStickyHeaderExtent(int section) => XamlParent.IsGrouping && AreStickyGroupHeadersEnabled && RelativeGroupHeaderPlacement == RelativeHeaderPlacement.Inline ?
			GetExtent(_inlineHeaderFrames[section].Size) :
			0;

		/// <summary>
		/// Get all LayoutAttributes for every display element.
		/// </summary>
		private IEnumerable<_LayoutAttributes> GetAllElementLayouts()
		{
			foreach (var kvp in _itemLayoutInfos)
			{
				foreach (var kvp2 in kvp.Value)
				{
					yield return kvp2.Value;
				}
			}

			foreach (var kvp in _supplementaryLayoutInfos)
			{
				foreach (var kvp2 in kvp.Value)
				{
					yield return kvp2.Value;
				}
			}
		}

		private Uno.UI.IndexPath GetFirstVisibleIndexPath()
		{
			return CollectionView.IndexPathsForVisibleItems
				.OrderBy(p => p.ToIndexPath())
				.FirstOrDefault()?
				.ToIndexPath()
					?? Uno.UI.IndexPath.FromRowSection(-1, 0);
		}

		private Uno.UI.IndexPath GetLastVisibleIndexPath()
		{
			return CollectionView.IndexPathsForVisibleItems
				.OrderByDescending(p => p.ToIndexPath())
				.FirstOrDefault()?
				.ToIndexPath()
					?? Uno.UI.IndexPath.FromRowSection(-1, 0);
		}

		internal void UpdateReorderingItem(Point location, FrameworkElement element, object item)
		{
			var indexPath = XamlParent.GetIndexPathFromItem(item);
			var layoutAttributes = (_LayoutAttributes)LayoutAttributesForItem(indexPath.ToNSIndexPath());

			_reorderingState = (location, item, layoutAttributes);
			if (layoutAttributes != null && !DoesLayoutAttributesContainDraggedPoint(location, layoutAttributes))
			{
				// If the drag position is outside of the bounds of the item, recalculate the layout
				_invalidatingWhileReordering = true;
				InvalidateLayout();
			}
		}

		internal Uno.UI.IndexPath? CompleteReorderingItem(FrameworkElement element, object item)
		{
			var dropTarget = _reorderingDropTarget?.ToIndexPath();
			CleanupReordering();
			return dropTarget;
		}

		internal void CleanupReordering()
		{
			_reorderingState = null;
			ResetReorderedLayoutAttributes();
			_reorderingDropTarget = null;
			InvalidateLayout();
		}

		/// <summary>
		/// Update layout with provisional item positions during drag-to-reorder
		/// </summary>
		private void UpdateLayoutForReordering()
		{
			ResetReorderedLayoutAttributes();

			if (_reorderingState is { } reorderingState && reorderingState.LayoutAttributes is { } draggedAttributes)
			{
				var dropTargetAttributes = FindLayoutAttributesClosestOfPoint(reorderingState.Location);
				if (dropTargetAttributes == draggedAttributes)
				{
					// The item being dragged is currently under the point, no need to shift any items
					dropTargetAttributes = null;
				}
				_reorderingDropTarget = dropTargetAttributes?.IndexPath;
				if (dropTargetAttributes is not null)
				{
					var preDragDraggedFrame = draggedAttributes.Frame;

					var preDragDraggedExtentStart = GetExtentStart(preDragDraggedFrame);
					var dropTargetExtentStart = GetExtentStart(dropTargetAttributes.Frame);

					// If the dragged frame starts before the drop target frame, move its end to the target's end. If it starts after it,
					// move its start to the target's start. (Moving the start and moving the end will only be different when items have
					// unequal sizes.)
					var draggedFrame = preDragDraggedExtentStart < dropTargetExtentStart ?
						ApplyTemporaryFrame(draggedAttributes, GetExtentEnd(dropTargetAttributes.Frame), SetExtentEnd) :
						ApplyTemporaryFrame(draggedAttributes, GetExtentStart(dropTargetAttributes.Frame), SetExtentStart);
					var draggedFrameExtent = GetExtent(draggedFrame.Size);

					foreach (var dict in _itemLayoutInfos.Values)
					{
						foreach (var layoutAttributes in dict.Values)
						{
							if (layoutAttributes == draggedAttributes)
							{
								continue;
							}
							var extentStart = GetExtentStart(layoutAttributes.Frame);

							if (extentStart >= dropTargetExtentStart && extentStart < preDragDraggedExtentStart)
							{
								// Shift elements at or after target, and before pre-drag position, down
								ApplyTemporaryFrame(layoutAttributes, draggedFrameExtent, AdjustExtentOffset);
							}
							else if (extentStart <= dropTargetExtentStart && extentStart > preDragDraggedExtentStart)
							{
								// Shift elements at or before target, and after pre-drag position, up
								ApplyTemporaryFrame(layoutAttributes, -draggedFrameExtent, AdjustExtentOffset);
							}
						}
					}
				}
			}

			CGRect ApplyTemporaryFrame(_LayoutAttributes layoutAttributes, nfloat adjustValue, AdjustFrame adjustFrame)
			{
				var frame = layoutAttributes.Frame;
				_preReorderFrames[layoutAttributes.IndexPath] = frame;
				adjustFrame(ref frame, adjustValue);
				layoutAttributes.Frame = frame;
				return frame;
			}
		}
		private delegate void AdjustFrame(ref CGRect frame, nfloat adjustValue);

		/// <summary>
		/// Reset provisional item positions to their pre-drag positions.
		/// </summary>
		private void ResetReorderedLayoutAttributes()
		{
			foreach (var kvp in _preReorderFrames)
			{
				if (LayoutAttributesForItem(kvp.Key) is { } layoutAttributes)
				{
					layoutAttributes.Frame = kvp.Value;
				}
			}
			_preReorderFrames.Clear();
		}

		private _LayoutAttributes FindLayoutAttributesClosestOfPoint(Point point)
		{
			var adjustedPoint = AdjustExtentOffset(point, GetExtent(Owner.ContentOffset));

			var closestDistance = double.MaxValue;
			var closestElement = default(_LayoutAttributes);

			foreach (var dict in _itemLayoutInfos.Values)
			{
				foreach (var layoutAttributes in dict.Values)
				{
					var distance = ((Rect)layoutAttributes.Frame).GetDistance(adjustedPoint);
					if (distance == 0)
					{
						// Fast path: we found the element that is under the element
						return layoutAttributes;
					}

					if (distance < closestDistance)
					{
						closestDistance = distance;
						closestElement = layoutAttributes;
					}
				}
			}

			return closestElement;
		}

		private bool DoesLayoutAttributesContainDraggedPoint(Point point, _LayoutAttributes layoutAttributes)
		{
			var adjustedPoint = AdjustExtentOffset(point, GetExtent(Owner.ContentOffset));
			return layoutAttributes.Frame.Contains(adjustedPoint);
		}

		private protected Uno.UI.IndexPath? GetAndUpdateReorderingIndex() => throw new NotSupportedException("Not used on iOS");

		protected CGRect AdjustExtentOffset(CGRect frame, nfloat adjustment)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				frame.Y += adjustment;
			}
			else
			{
				frame.X += adjustment;
			}
			return frame;
		}

		protected CGPoint AdjustExtentOffset(CGPoint point, nfloat adjustment)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				point.Y += adjustment;
			}
			else
			{
				point.X += adjustment;
			}

			return point;
		}

		protected nfloat GetExtentStart(CGRect frame)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				return frame.Top;
			}
			else
			{
				return frame.Left;
			}
		}

		protected nfloat GetExtentEnd(CGRect frame)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				return frame.Bottom;
			}
			else
			{
				return frame.Right;
			}
		}

		private nfloat GetExtent(CGSize size)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				return size.Height;
			}
			else
			{
				return size.Width;
			}
		}

		private nfloat GetExtent(CGPoint point)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				return point.Y;
			}
			else
			{
				return point.X;
			}
		}

		protected nfloat GetBreadth(CGSize size)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				return size.Width;
			}
			else
			{
				return size.Height;
			}
		}

		protected nfloat GetBreadthStart(CGRect frame)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				return frame.Left;
			}
			else
			{
				return frame.Top;
			}
		}

		protected nfloat GetBreadthEnd(CGRect frame)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				return frame.Right;
			}
			else
			{
				return frame.Bottom;
			}
		}

		protected void SetBreadthStart(ref CGRect frame, nfloat breadthStart)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				frame.X = breadthStart;
			}
			else
			{
				frame.Y = breadthStart;
			}
		}

		protected void SetBreadth(ref CGRect frame, nfloat breadth)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				frame.Width = breadth;
			}
			else
			{
				frame.Height = breadth;
			}
		}

		protected void SetExtentStart(ref CGRect frame, nfloat extentStart)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				frame.Y = extentStart;
			}
			else
			{
				frame.X = extentStart;
			}
		}

		private void SetExtentEnd(ref CGRect frame, nfloat extentEnd)
		{
			var delta = extentEnd - GetExtentEnd(frame);
			AdjustExtentOffset(ref frame, delta);
		}

		private void AdjustExtentOffset(ref CGRect frame, nfloat adjustment)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				frame.Y += adjustment;
			}
			else
			{
				frame.X += adjustment;
			}
		}

		protected void SetExtent(ref CGSize size, nfloat extent)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				size.Height = extent;
			}
			else
			{
				size.Width = extent;
			}
		}

		protected CGPoint SetExtentOffset(CGPoint point, nfloat newOffset)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				point.Y = newOffset;
			}
			else
			{
				point.X = newOffset;
			}

			return point;
		}

		protected void IncrementExtent(ref CGRect frame)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				frame.Y += frame.Height;
			}
			else
			{
				frame.X += frame.Width;
			}
		}

		private void IncrementExtentBy(ref CGRect frame, nfloat increment)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				frame.Y += increment;
			}
			else
			{
				frame.X += increment;
			}
		}

		protected void IncrementBreadth(ref CGRect frame)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				frame.X += frame.Width;
			}
			else
			{
				frame.Y += frame.Height;
			}
		}

		private void IncrementBreadthBy(ref CGRect frame, nfloat increment)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				frame.X += increment;
			}
			else
			{
				frame.Y += increment;
			}
		}

		protected CGRect GetInlineHeaderFrame(int section)
		{
			return _inlineHeaderFrames[section];
		}

		/// <summary>
		/// Get size available to children, given an available breadth.
		/// </summary>
		private Size GetAvailableChildSize(nfloat availableBreadth)
		{
			return ScrollOrientation == Orientation.Vertical ?
				new Size(availableBreadth, double.PositiveInfinity) :
				new Size(double.PositiveInfinity, availableBreadth);
		}

		/// <summary>
		/// Get size available to children, given an available size for the viewport.
		/// </summary>
		private Size GetAvailableChildSize(CGSize availableViewportSize)
		{
			return ScrollOrientation == Orientation.Vertical ?
				new Size(availableViewportSize.Width, double.PositiveInfinity) :
				new Size(double.PositiveInfinity, availableViewportSize.Height);
		}

#if DEBUG
#pragma warning disable IDE0051 // Remove unused private members
		_LayoutAttributes[] AllItemLayoutAttributes => _itemLayoutInfos?.SelectMany(kvp => kvp.Value.Values).ToArray();

		private void DumpFrames(string header, [CallerMemberName] string caller = "", [CallerLineNumber] int line = -1)
		{
			Console.WriteLine($"********************* {caller}@{line}: {header}");
			foreach (var dict in _itemLayoutInfos.Values)
			{
				var i = 0;
				foreach (var layoutAttributes in dict.Values)
				{
					Console.WriteLine($"*********************   - {i++} {ToString(layoutAttributes)}");
				}
			}
		}

		private string ToString(UICollectionViewLayoutAttributes layoutAttributes)
		{
			var index = layoutAttributes.IndexPath;
			var data = (CollectionView?.CellForItem(index)?.ContentView?.Subviews?.FirstOrDefault() as FrameworkElement)?.DataContext?.ToString() ?? "--undef--";

			return $"{layoutAttributes.GetHashCode():X8} {index.Section}-{index.Row} {((int)layoutAttributes.Frame.Width)}x{((int)layoutAttributes.Frame.Height)}@{((int)layoutAttributes.Frame.X)},{((int)layoutAttributes.Frame.Y)} {data}";
		}

		CGRect[] AllItemFrames => AllItemLayoutAttributes?.Select(l => l.Frame).ToArray();
#pragma warning restore IDE0051 // Remove unused private members
#endif
	}
}
