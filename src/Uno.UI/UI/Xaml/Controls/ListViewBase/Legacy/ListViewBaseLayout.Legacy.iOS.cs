using System;
using System.Collections.Generic;
using System.Drawing;
using Windows.UI.Xaml;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.Disposables;
using Foundation;
using UIKit;
using CoreGraphics;
using LayoutInfo = System.Collections.Generic.Dictionary<Foundation.NSIndexPath, UIKit.UICollectionViewLayoutAttributes>;
using Uno.Diagnostics.Eventing;
using Uno;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using ObjCRuntime;

namespace Uno.UI.Controls.Legacy
{
	public abstract partial class ListViewBaseLayout : UICollectionViewLayout, DependencyObject
	{
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{17071417-C62E-4469-BD1D-981734C46E3C}");

			public const int ListViewBaseLayout_PrepareLayoutStart = 1;
			public const int ListViewBaseLayout_PrepareLayoutStop = 2;
		}

		private enum DirtyState { None, NeedsRelayout, NeedsHeaderRelayout }

		#region Members
		protected Dictionary<string, LayoutInfo> _layoutInfos;
		private Thickness _margin;
		private float _lineSpacing;
		private ListViewBaseScrollDirection _scrollDirection;
		private float _intersectionSpacing;
		private Dictionary<CachedTuple<int, int>, NSIndexPath> _indexPaths = new Dictionary<CachedTuple<int, int>, NSIndexPath>(CachedTuple<int, int>.Comparer);
		private Dictionary<CachedTuple<int, int>, UICollectionViewLayoutAttributes> _layoutAttributesForIndexPaths = new Dictionary<CachedTuple<int, int>, UICollectionViewLayoutAttributes>(CachedTuple<int, int>.Comparer);
		private DirtyState _dirtyState;
		private CGSize _lastReportedSize;
		private CGSize _lastAvailableSize;
		private bool _invalidatingHeadersOnBoundsChange;
		#endregion

		#region Properties
		public ListViewBaseScrollDirection ScrollDirection
		{
			get { return _scrollDirection; }
			set
			{
				if (_scrollDirection != value)
				{
					_scrollDirection = value;
					InvalidateLayout();
				}
			}
		}

		public Thickness Margin
		{
			get { return _margin; }
			set
			{
				if (!_margin.Equals(value))
				{
					_margin = value;
					InvalidateLayout();
				}
			}
		}

		public float LineSpacing
		{
			get { return _lineSpacing; }
			set
			{
				_lineSpacing = value;
				InvalidateLayout();
			}
		}
		public float IntersectionSpacing
		{
			get { return _intersectionSpacing; }
			set
			{
				_intersectionSpacing = value;
				InvalidateLayout();
			}
		}

		protected CGSize _itemSize;

		public CGSize ItemSize
		{
			get
			{
				return _itemSize;
			}
			set
			{
				if (_itemSize != value)
				{
					_itemSize = value;
					InvalidateLayout();
				}
			}
		}


		public bool AreStickyGroupHeadersEnabled
		{
			get { return (bool)this.GetValue(AreStickyGroupHeadersEnabledProperty); }
			set { this.SetValue(AreStickyGroupHeadersEnabledProperty, value); }
		}

		public static DependencyProperty AreStickyGroupHeadersEnabledProperty { get; } =
			DependencyProperty.Register("AreStickyGroupHeadersEnabled", typeof(bool), typeof(ListViewBaseLayout), new FrameworkPropertyMetadata(false, OnAreStickyGroupHeadersEnabledChanged));

		private static void OnAreStickyGroupHeadersEnabledChanged(object o, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue != (bool)e.OldValue)
			{
				((ListViewBaseLayout)o).InvalidateLayout();
			}
		}

		internal WeakReference<ListViewBaseSource> Source
		{
			get;
			set;
		}
		#endregion

		public ListViewBaseLayout()
		{
			InitializeBinder();
		}

		#region Overrides
		public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
		{
			var allAttributes = new List<UICollectionViewLayoutAttributes>();

			foreach (var cellLayoutInfo in (_layoutInfos?.Values).Safe())
			{
				foreach (var layoutAttributes in cellLayoutInfo.Values)
				{
					// Alias to avoid paying the price of interop twice.
					var frame = layoutAttributes.Frame;

					if (rect.Contains(frame) ||
						rect.IntersectsWith(frame))
					{
						allAttributes.Add(layoutAttributes);
					}
				}
			}

			return allAttributes.ToArray();
		}

		public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath indexPath)
		{
			return _layoutInfos[ListViewBase.ListViewItemElementKind][indexPath];
		}

		public override UICollectionViewLayoutAttributes LayoutAttributesForSupplementaryView(NSString kind, NSIndexPath indexPath)
		{
			return _layoutInfos[kind][indexPath];
		}

		public override CGSize CollectionViewContentSize => PrepareLayout(false);

		public override void PrepareLayout()
		{
			//If data reload is scheduled, call it immediately to avoid NSInternalInconsistencyException caused by supplying layoutAttributes for index paths that the list doesn't 'know about'
			var listViewBase = CollectionView as ListViewBase;
			if (listViewBase?.NeedsReloadData ?? false)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug("LVBL: Calling immediate data reload");
				}

				ClearCaches();

				listViewBase.ReloadDataIfNeeded();
			}

			PrepareLayout(true);
		}

		#endregion

		public CGSize SizeThatFits(CGSize size)
		{
			return PrepareLayout(false, size);
		}

		private CGSize PrepareLayout(bool createLayoutInfo, CGSize? size = null)
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
					_dirtyState = DirtyState.NeedsRelayout;
				}

				if (_dirtyState == DirtyState.NeedsRelayout)
				{
					_lastReportedSize = PrepareLayoutInternal(createLayoutInfo, size);
					_lastAvailableSize = size ?? CGSize.Empty;

					if (createLayoutInfo)
					{
						_dirtyState = DirtyState.None;
						(CollectionView as ListViewBase)?.SetLayoutCreated();
					}
				}
				else if (_dirtyState == DirtyState.NeedsHeaderRelayout)
				{
					UpdateHeaderPositions();
					_dirtyState = DirtyState.None;
				}

				return _lastReportedSize;
			}
		}

		protected abstract CGSize PrepareLayoutInternal(bool createLayoutInfo, CGSize? size = null);

		protected virtual void UpdateHeaderPositions() { }

		protected CGSize GetItemSizeForIndexPath(NSIndexPath indexPath)
		{
			ListViewBaseSource source;
			CGSize result;
			Source.TryGetTarget(out source);

			if (source != null)
			{
				result = source.GetItemSize(CollectionView, indexPath);
				if (nfloat.IsNaN(result.Width) || nfloat.IsNaN(result.Height))
				{
					// This may happen if the Source is not a ListView, but if it is
					// the ItemSize will always be valid.
					result = ItemSize;
				}
			}

			else
			{
				result = ItemSize;
			}

			return result;
		}

		protected CGSize GetHeaderSize()
		{
			return Source.GetTarget()?.GetHeaderSize() ?? CGSize.Empty;
		}

		protected CGSize GetFooterSize()
		{
			return Source.GetTarget()?.GetFooterSize() ?? CGSize.Empty;
		}

		protected CGSize GetSectionHeaderSize()
		{
			return Source.GetTarget()?.GetSectionHeaderSize() ?? CGSize.Empty;
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
			var boundsDimensionsChanged = newBounds.AreSizesDifferent(CollectionView.Bounds);
			var newBoundsHasNonZeroArea = newBounds.Width > nfloat.Epsilon
				&& newBounds.Height > nfloat.Epsilon;

			//Invalidate if collection bounds have changed
			if (boundsDimensionsChanged && newBoundsHasNonZeroArea)
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

		public override void InvalidateLayout()
		{
			//Called from scrolling, update sticky headers
			if (_invalidatingHeadersOnBoundsChange)
			{
				_invalidatingHeadersOnBoundsChange = false;
				if (_dirtyState == DirtyState.None)
				{
					_dirtyState = DirtyState.NeedsHeaderRelayout;
				}
			}
			//Called for some other reason, update everything
			else
			{
				_dirtyState = DirtyState.NeedsRelayout;
			}
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
			UICollectionViewLayoutAttributes attributes;

			if (!_layoutAttributesForIndexPaths.TryGetValue(key, out attributes))
			{
				var indexPath = GetNSIndexPathFromRowSection(row, section);
				_layoutAttributesForIndexPaths.Add(key, attributes = UICollectionViewLayoutAttributes.CreateForCell<UICollectionViewLayoutAttributes>(indexPath));
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
	}
}
