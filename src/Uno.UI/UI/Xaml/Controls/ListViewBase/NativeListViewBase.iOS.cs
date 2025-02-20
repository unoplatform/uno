using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Input;
using Uno.Extensions;
using Uno.UI.Views.Controls;
using System.Collections.Specialized;
using Uno.Disposables;
using Windows.UI.Core;
using Uno.UI.Controls;
using System.ComponentModel;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;

using Uno.UI;
using Windows.UI.Xaml.Data;
using Uno.UI.Extensions;

using Foundation;
using UIKit;
using CoreGraphics;
using CoreAnimation;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.UI.Input;
using Windows.Devices.Input;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeListViewBase : UICollectionView
	{
		#region Constants
		public static readonly NSString ListViewItemReuseIdentifierNS = new NSString(nameof(ListViewItemReuseIdentifier));
		public const string ListViewItemReuseIdentifier = nameof(ListViewItemReuseIdentifier);

		public static readonly NSString ListViewItemElementKindNS = new NSString(nameof(ListViewItemElementKind));
		public const string ListViewItemElementKind = nameof(ListViewItemElementKind);

		public static readonly NSString ListViewHeaderReuseIdentifierNS = new NSString(nameof(ListViewHeaderReuseIdentifier));
		public const string ListViewHeaderReuseIdentifier = nameof(ListViewHeaderReuseIdentifier);

		public static readonly NSString ListViewHeaderElementKindNS = new NSString(nameof(ListViewHeaderElementKind));
		public const string ListViewHeaderElementKind = nameof(ListViewHeaderElementKind);

		public static readonly NSString ListViewFooterReuseIdentifierNS = new NSString(nameof(ListViewFooterReuseIdentifier));
		public const string ListViewFooterReuseIdentifier = nameof(ListViewFooterReuseIdentifier);

		public static readonly NSString ListViewFooterElementKindNS = new NSString(nameof(ListViewFooterElementKind));
		public const string ListViewFooterElementKind = nameof(ListViewFooterElementKind);

		public static readonly NSString ListViewSectionHeaderReuseIdentifierNS = new NSString(nameof(ListViewSectionHeaderReuseIdentifier));
		public const string ListViewSectionHeaderReuseIdentifier = nameof(ListViewSectionHeaderReuseIdentifier);

		public static readonly NSString ListViewSectionHeaderElementKindNS = new NSString(nameof(ListViewSectionHeaderElementKind));
		public const string ListViewSectionHeaderElementKind = nameof(ListViewSectionHeaderElementKind);

		/// <summary>
		/// This property enables animations when using the ScrollIntoView Method.
		/// </summary>
		public bool AnimateScrollIntoView { get; set; } = Uno.UI.FeatureConfiguration.ListViewBase.AnimateScrollIntoView;
		#endregion

		#region Members
		private bool _needsReloadData;
		/// <summary>
		/// ReloadData() has been called, but the layout hasn't been updated. During this window, in-place modifications to the
		/// collection (InsertItems, etc) shouldn't be called because they will result in a NSInternalInconsistencyException
		/// </summary>
		private bool _needsLayoutAfterReloadData;
		/// <summary>
		/// List was empty last time ReloadData() was called. If inserting items into an empty collection we should do a refresh instead, 
		/// to work around a UICollectionView bug https://stackoverflow.com/questions/12611292/uicollectionview-assertion-failure
		/// </summary>
		private bool _listEmptyLastRefresh;
		private bool _isReloadDataDispatched;

		private readonly SerialDisposable _scrollIntoViewSubscription = new SerialDisposable();
		#endregion

		#region Properties
		new internal ListViewBaseSource Source
		{
			get => base.Source as ListViewBaseSource;
			set => base.Source = value;
		}

		public Style ItemContainerStyle => XamlParent?.ItemContainerStyle;

		public DataTemplate HeaderTemplate => XamlParent?.HeaderTemplate;

		public DataTemplate FooterTemplate => XamlParent?.FooterTemplate;

		public DataTemplateSelector ItemTemplateSelector => XamlParent?.ItemTemplateSelector;

		internal bool NeedsReloadData => _needsReloadData;

		internal CGPoint UpperScrollLimit { get { return (CGPoint)(ContentSize - Frame.Size); } }
		#endregion

		public GroupStyle GroupStyle => XamlParent?.GroupStyle.FirstOrDefault();

		internal object SelectedItem => XamlParent?.SelectedItem;

		internal IList<object> SelectedItems => XamlParent?.SelectedItems;

		public ListViewSelectionMode SelectionMode => XamlParent?.SelectionMode ?? ListViewSelectionMode.None;

		public object Header => XamlParent?.ResolveHeaderContext();

		public object Footer => XamlParent?.ResolveFooterContext();

		/// <summary>
		/// Get all currently visible supplementary views.
		/// </summary>
		internal IEnumerable<UICollectionReusableView> VisibleSupplementaryViews
		{
			get
			{
				foreach (var view in GetVisibleSupplementaryViews(ListViewHeaderElementKindNS))
				{
					yield return view;
				}
				foreach (var view in GetVisibleSupplementaryViews(ListViewSectionHeaderElementKindNS))
				{
					yield return view;
				}
				foreach (var view in GetVisibleSupplementaryViews(ListViewFooterElementKindNS))
				{
					yield return view;
				}
			}
		}

		internal IEnumerable<SelectorItem> CachedItemViews => Enumerable.Empty<SelectorItem>();

		private bool UseCollectionAnimations => XamlParent?.UseCollectionAnimations ?? true;

		public NativeListViewBase() :
			this(new RectangleF(),
				//Supply a layout, otherwise UICollectionView constructor throws ArgumentNullException. This will later be set to ListViewLayout or GridViewLayout as desired.
				new UnsetLayout()
			)
		{ }

		protected NativeListViewBase(RectangleF frame, UICollectionViewLayout layout)
			: base(frame, layout)
		{
			Initialize();
		}

		private void Initialize()
		{
			BackgroundColor = UIColor.Clear;
			IFrameworkElementHelper.Initialize(this);

			var internalContainerType = typeof(ListViewBaseInternalContainer);
			RegisterClassForCell(internalContainerType, ListViewItemReuseIdentifier);
			RegisterClassForSupplementaryView(internalContainerType, ListViewHeaderElementKindNS, ListViewHeaderReuseIdentifier);
			RegisterClassForSupplementaryView(internalContainerType, ListViewFooterElementKindNS, ListViewFooterReuseIdentifier);
			RegisterClassForSupplementaryView(internalContainerType, ListViewSectionHeaderElementKindNS, ListViewSectionHeaderReuseIdentifier);

			DelaysContentTouches = true; // cf. TouchesManager which can alter this!

			ShowsHorizontalScrollIndicator = true;
			ShowsVerticalScrollIndicator = true;

			if (ScrollViewer.UseContentInsetAdjustmentBehavior)
			{
				ContentInsetAdjustmentBehavior = UIScrollViewContentInsetAdjustmentBehavior.Never;
			}
		}

		public override CGSize SizeThatFits(CGSize size)
		{
			var result = NativeLayout.SizeThatFits(size);
			return result;
		}

		internal void Refresh()
		{
			SetNeedsReloadData();

			// We don't reset a negative offset, to support UIRefreshControl-related scenarios. A negative ContentOffset will be reset 
			// on its own by the UI when the user releases the swipe gesture.
			if (ContentOffset.Y > 0 || ContentOffset.X > 0)
			{
				this.SetContentOffset(new PointF(0, 0), false);
			}


			// This is required to ensure that the list will notify its
			// parents that its size may change because of some of its items.
			SetSuperviewNeedsLayout();
		}

		public override void InsertItems(NSIndexPath[] indexPaths)
		{
			if (TryApplyCollectionChange())
			{
				using (EnableOrDisableAnimations())
				{
					NativeLayout?.NotifyCollectionChange(new CollectionChangedOperation(
									indexPaths.First().ToIndexPath(),
									indexPaths.Length,
									NotifyCollectionChangedAction.Add,
									CollectionChangedOperation.Element.Item
								));
					try
					{
						base.InsertItems(indexPaths);
					}
					catch (Exception e)
					{
						this.Log().Error("Error when updating collection", e);
					}
				}
			}
			else
			{
				NativeLayout?.NeedsRelayout();
			}
		}

		public override void InsertSections(NSIndexSet sections)
		{
			if (TryApplyCollectionChange())
			{
				using (EnableOrDisableAnimations())
				{
					NativeLayout?.NotifyCollectionChange(new CollectionChangedOperation(
									Uno.UI.IndexPath.FromRowSection(0, (int)sections.FirstIndex),
									(int)sections.Count,
									NotifyCollectionChangedAction.Add,
									CollectionChangedOperation.Element.Group
								));
					try
					{
						base.InsertSections(sections);
					}
					catch (Exception e)
					{
						this.Log().Error("Error when updating collection", e);
					}
				}
			}
		}

		public override void DeleteItems(NSIndexPath[] indexPaths)
		{
			if (TryApplyCollectionChange())
			{
				using (EnableOrDisableAnimations())
				{
					NativeLayout?.NotifyCollectionChange(new CollectionChangedOperation(
									indexPaths.First().ToIndexPath(),
									indexPaths.Length,
									NotifyCollectionChangedAction.Remove,
									CollectionChangedOperation.Element.Item
								));
					try
					{
						base.DeleteItems(indexPaths);
					}
					catch (Exception e)
					{
						this.Log().Error("Error when updating collection", e);
					}
				}
			}
		}

		public override void DeleteSections(NSIndexSet sections)
		{
			if (TryApplyCollectionChange())
			{
				using (EnableOrDisableAnimations())
				{
					NativeLayout?.NotifyCollectionChange(new CollectionChangedOperation(
									Uno.UI.IndexPath.FromRowSection(0, (int)sections.FirstIndex),
									(int)sections.Count,
									NotifyCollectionChangedAction.Remove,
									CollectionChangedOperation.Element.Group
								));
					try
					{
						base.DeleteSections(sections);
					}
					catch (Exception e)
					{
						this.Log().Error("Error when updating collection", e);
					}
				}
			}
		}

		public override void ReloadItems(NSIndexPath[] indexPaths)
		{
			if (TryApplyCollectionChange())
			{
				using (EnableOrDisableAnimations())
				{
					NativeLayout?.NotifyCollectionChange(new CollectionChangedOperation(
									indexPaths.First().ToIndexPath(),
									indexPaths.Length,
									NotifyCollectionChangedAction.Replace,
									CollectionChangedOperation.Element.Item
								));
					base.ReloadItems(indexPaths);
				}
			}
		}

		public override void ReloadSections(NSIndexSet sections)
		{
			if (TryApplyCollectionChange())
			{
				using (EnableOrDisableAnimations())
				{
					NativeLayout?.NotifyCollectionChange(new CollectionChangedOperation(
						Uno.UI.IndexPath.FromRowSection(0, (int)sections.FirstIndex),
						(int)sections.Count,
						NotifyCollectionChangedAction.Replace,
						CollectionChangedOperation.Element.Group
					));
					base.ReloadSections(sections);
				}
			}
		}

		/// <summary>
		/// Check if in-place collection modification can be performed. If not, retrigger a refresh instead.
		/// </summary>
		private bool TryApplyCollectionChange()
		{
			if (_needsLayoutAfterReloadData || _listEmptyLastRefresh)
			{
				SetNeedsReloadData();
				if (XamlParent.AreEmptyGroupsHidden)
				{
					//Refresh subscriptions because the group occupancies may have changed.
					XamlParent?.ObserveCollectionChanged();
				}
				return false;
			}

			return true;
		}

		private IDisposable EnableOrDisableAnimations()
		{
			if (UseCollectionAnimations)
			{
				return Disposable.Empty;
			}

			AnimationsEnabled = false;

			return Disposable.Create(() => AnimationsEnabled = true);
		}

		internal SelectorItem ContainerFromIndex(NSIndexPath indexPath)
		{
			var cell = CellForItem(indexPath) as ListViewBaseInternalContainer;
			return cell?.Content as SelectorItem;
		}

		internal ContentControl ContainerFromGroupIndex(NSIndexPath indexPath)
		{
			var container = GetSupplementaryView(ListViewSectionHeaderElementKindNS, indexPath) as ListViewBaseInternalContainer;
			return container?.Content;
		}

		/// <summary>
		/// Reapply data contexts and templates to header and footer.
		/// </summary>
		internal void UpdateHeaderAndFooter()
		{
			var header = (GetSupplementaryView(ListViewHeaderElementKindNS, NSIndexPath.FromRowSection(0, 0)) as ListViewBaseInternalContainer)?.Content;

			if (header != null)
			{
				header.ContentTemplate = HeaderTemplate;
				header.DataContext = Header;
			}

			var footer = (GetSupplementaryView(ListViewFooterElementKindNS, NSIndexPath.FromRowSection(0, 0)) as ListViewBaseInternalContainer)?.Content;

			if (footer != null)
			{
				footer.ContentTemplate = FooterTemplate;
				footer.DataContext = Footer;
			}
		}

		/// <summary>
		/// Defines the layout to be used to display the list items.
		/// </summary>
		internal VirtualizingPanelLayout NativeLayout
		{
			get
			{
				return (VirtualizingPanelLayout)CollectionViewLayout;
			}
			set
			{
				if (value != null)
				{
					value.Source = new WeakReference<ListViewBaseSource>(Source);
					value.Padding = Padding;
				}

				CollectionViewLayout = value;
			}
		}

		public override void SetNeedsLayout()
		{
			base.SetNeedsLayout();

			// This method is present to ensure that it is not overridden by the mixins.
			// SetNeedsLayout is called very often during scrolling, and it must not
			// call SetParentNeeds layout, otherwise the whole visual tree above the listviewbase
			// will be refreshed.
		}

		public override void LayoutSubviews()
		{
			base.LayoutSubviews();

			foreach (var tuple in VisibleCellsAndPaths)
			{
				var cell = tuple.Cell;
				var unoCell = cell as ListViewBaseInternalContainer;

				if (unoCell == null)
				{
					continue;
				}

				var isAnimating = unoCell./* cache */Layer.AnimationKeys?.Any() ?? false;
				if (isAnimating)
				{
					// Don't bother checking for consistency while items are animating
					break;
				}

				var expectedItem = XamlParent?.
					ItemFromIndex(XamlParent.
						GetIndexFromIndexPath(Uno.UI.IndexPath.FromNSIndexPath(tuple.Path)
					)
				);
				var actualItem = unoCell.Content?.DataContext;

				if (!XamlParent.IsItemItsOwnContainer(expectedItem)
					// This check is present for the support of explicit ListViewItem 
					// through the Items property. The DataContext may be set to some
					// user defined object.
					&& XamlParent.ItemsSource != null
					// The view's DataContext generally will differ from the source item when DisplayMemberPath is set
					&& XamlParent.DisplayMemberPath.IsNullOrEmpty())
				{
					var areMatching = Object.Equals(expectedItem, actualItem);
					if (!areMatching)
					{
						// This is a failsafe for in-place collection changes which leave the list in an inconsistent state, for exact reasons known only to UICollectionView.
						if (this.Log().IsEnabled(LogLevel.Warning))
						{
							(this).Log().Warn($"Cell had context {actualItem} instead of {expectedItem}, scheduling a refresh to ensure correct display of list. (Uno.UI.IndexPath={tuple.Path}");
						}
						DispatchReloadData();
					}
				}
			}
		}

		private IEnumerable<(NSIndexPath Path, UICollectionViewCell Cell)> VisibleCellsAndPaths
		{
			get
			{
				(NSIndexPath Path, UICollectionViewCell Cell) selector(NSIndexPath path) => (path, CellForItem(path));
				return IndexPathsForVisibleItems.Select(selector);
			}
		}

		internal Orientation ScrollOrientation => (CollectionViewLayout as VirtualizingPanelLayout)?.ScrollOrientation ?? Orientation.Vertical;

		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get
			{
				return ShowsHorizontalScrollIndicator ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden;
			}

			set
			{
				switch (value)
				{
					case ScrollBarVisibility.Disabled:
					case ScrollBarVisibility.Hidden:
						ShowsHorizontalScrollIndicator = false;
						break;
					case ScrollBarVisibility.Auto:
					case ScrollBarVisibility.Visible:
					default:
						ShowsHorizontalScrollIndicator = true;
						break;
				}
			}
		}

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get
			{
				return ShowsVerticalScrollIndicator ? ScrollBarVisibility.Visible : ScrollBarVisibility.Hidden;
			}

			set
			{
				switch (value)
				{
					case ScrollBarVisibility.Disabled:
					case ScrollBarVisibility.Hidden:
						ShowsVerticalScrollIndicator = false;
						break;
					case ScrollBarVisibility.Auto:
					case ScrollBarVisibility.Visible:
					default:
						ShowsVerticalScrollIndicator = true;
						break;
				}
			}
		}

		public void ScrollIntoView(object item)
		{
			ScrollIntoView(item, ScrollIntoViewAlignment.Default);
		}

		public void ScrollIntoView(object item, ScrollIntoViewAlignment alignment)
		{
			//Check if item is a group
			if (XamlParent.IsGrouping)
			{
				for (int i = 0; i < XamlParent.NumberOfDisplayGroups; i++)
				{
					if (XamlParent.GetGroupAtDisplaySection(i).Group?.Equals(item) ?? false)
					{
						//If item is a group, scroll to first element of that group
						ScrollToItem(NSIndexPath.FromItemSection(0, i), ConvertScrollAlignmentForGroups(alignment), AnimateScrollIntoView);
						return;
					}
				}
			}

			var index = IndexPathForItem(item);
			if (index != null)
			{
				if (IndexPathsForVisibleItems.Length == 0)
				{
					var cd = new CancellationDisposable();
					_scrollIntoViewSubscription.Disposable = cd;
					//Item is present but no items are visible, probably being called on first load. Dispatch so that it actually does something.

					Dispatcher.RunAsync(CoreDispatcherPriority.Normal, DispatchedScrollInner).AsTask(cd.Token);
				}
				else
				{
					_scrollIntoViewSubscription.Disposable = null; //Cancel any pending dispatched ScrollIntoView
					ScrollInner();
				}

				void DispatchedScrollInner()
				{
					index = IndexPathForItem(item);
					// Recheck item because it may no longer be there
					if (index != null)
					{
						ScrollInner();
					}
				}

				void ScrollInner()
				{
					//Scroll to individual item, We set the UICollectionViewScrollPosition to None to have the same behavior as windows.
					//We can potentially customize this By using ConvertScrollAlignment and use different alignments.
					var needsMaterialize = Source.UpdateLastMaterializedItem(index);
					if (needsMaterialize)
					{
						NativeLayout.InvalidateLayout();
						NativeLayout.PrepareLayout();
					}

					var offset = NativeLayout.GetTargetScrollOffset(index, alignment);
					SetContentOffset(offset, AnimateScrollIntoView);
					NativeLayout.UpdateStickyHeaderPositions();
				}
			}
		}

		private UICollectionViewScrollPosition ConvertSnapPointsAlignmentToScrollPosition()
		{
			var snapPointsType = (CollectionViewLayout as VirtualizingPanelLayout)?.SnapPointsType;

			if (snapPointsType != SnapPointsType.MandatorySingle)
			{
				return UICollectionViewScrollPosition.None;
			}

			var scrollDirection = ScrollOrientation;
			var snapPointsAlignment = (CollectionViewLayout as VirtualizingPanelLayout)?.SnapPointsAlignment;

			switch (scrollDirection)
			{
				case Orientation.Horizontal:
					{
						switch (snapPointsAlignment)
						{
							case SnapPointsAlignment.Center:
								return UICollectionViewScrollPosition.CenteredHorizontally;
							case SnapPointsAlignment.Near:
								return UICollectionViewScrollPosition.Left;
							case SnapPointsAlignment.Far:
								return UICollectionViewScrollPosition.Right;
						}

						throw new InvalidOperationException();
					}
				case Orientation.Vertical:
					{
						switch (snapPointsAlignment)
						{
							case SnapPointsAlignment.Center:
								return UICollectionViewScrollPosition.CenteredVertically;
							case SnapPointsAlignment.Near:
								return UICollectionViewScrollPosition.Top;
							case SnapPointsAlignment.Far:
								return UICollectionViewScrollPosition.Bottom;
						}

						throw new InvalidOperationException();
					}
			}

			return UICollectionViewScrollPosition.None;
		}

		private UICollectionViewScrollPosition ConvertScrollAlignmentForGroups(ScrollIntoViewAlignment alignment)
		{
			if (alignment == ScrollIntoViewAlignment.Default && this.Log().IsEnabled(LogLevel.Warning))
			{
				//TODO: UICollectionViewScrollPosition doesn't contain an option corresponding to 'nearest edge.' This might need to be implemented manually using ScrollRectToVisible
				this.Log().Warn("ScrollIntoViewAlignment.Default is not implemented");
			}

			var scrollDirection = ScrollOrientation;
			switch (scrollDirection)
			{
				case Orientation.Horizontal:
					return UICollectionViewScrollPosition.Left;
				case Orientation.Vertical:
					return UICollectionViewScrollPosition.Top;
			}

			throw new InvalidOperationException($"Invalid scroll direction: {scrollDirection}");
		}

		public override void SetContentOffset(CGPoint contentOffset, bool animated)
		{
			base.SetContentOffset(contentOffset, animated);
			if (animated)
			{
				Source?.SetIsAnimatedScrolling();
			}
		}

		/// <summary>
		/// Gets the NSIndexPath for the specified item
		/// </summary>
		/// <remark>Do not use when source is paginated as this iterates the source</remark>
		public NSIndexPath IndexPathForItem(object item)
		{
			var indexPath = XamlParent.GetIndexPathFromItem(item);
			if (indexPath.Row < 0)
			{
				return null;
			}

			return indexPath.ToNSIndexPath();
		}


		public override nint NumberOfItemsInSection(nint section)
		{
			return Source.GetItemsCount(this, section);
		}

		internal void SetNeedsReloadData()
		{
			//We do not want to reload if list is collapsed
			if (_needsReloadData || Visibility == Visibility.Collapsed)
			{
				return;
			}

			_needsReloadData = true;

			ReloadDataIfNeeded();
		}

		/// <summary>
		/// Reloads full list if need be
		/// </summary>
		internal void ReloadDataIfNeeded()
		{
			if (_needsReloadData)
			{
				_needsReloadData = false;
				_needsLayoutAfterReloadData = true;
				ReloadData();

				Source?.ReloadData();
				NativeLayout?.ReloadData();

				_listEmptyLastRefresh = XamlParent?.NumberOfItems == 0;
			}
		}

		internal void SetLayoutCreated()
		{
			_needsLayoutAfterReloadData = false;
		}

		/// <summary>
		/// Schedule <see cref="SetNeedsReloadData"/> to run on the next UI loop.
		/// </summary>
		private void DispatchReloadData()
		{
			if (_isReloadDataDispatched)
			{
				return;
			}
			_isReloadDataDispatched = true;

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.High, setNeedsReloadData);

			void setNeedsReloadData()
			{
				_isReloadDataDispatched = false;
				SetNeedsReloadData();
			}
		}

		public Thickness Padding
		{
			get { return NativeLayout.Padding; }
			set { NativeLayout.Padding = value; }
		}

		#region Touches
		private UIElement _touchTarget;

		/// <inheritdoc />
		public override void TouchesBegan(NSSet touches, UIEvent evt)
		{
			base.TouchesBegan(touches, evt);

			// We wait for the first touches to get the parent so we don't have to track Loaded/UnLoaded
			// Like native dispatch on iOS, we do "implicit captures" of the target.
			if (this.GetParent() is UIElement parent)
			{
				// canBubbleNatively: true => We let native bubbling occur properly as it's never swallowed by the system
				//							  but blocking it would be breaking in a lot of aspects
				//							  (e.g. it would prevent all subsequent events for the given pointer).

				_touchTarget = parent;
				_touchTarget.TouchesBegan(touches, evt, canBubbleNatively: true);
			}
		}

		/// <inheritdoc />
		public override void TouchesMoved(NSSet touches, UIEvent evt)
		{
			base.TouchesMoved(touches, evt);

			// canBubbleNatively: false => The system might silently swallow pointers after a few moves so we prefer to bubble in managed.
			_touchTarget?.TouchesMoved(touches, evt, canBubbleNatively: false);
		}

		/// <inheritdoc />
		public override void TouchesEnded(NSSet touches, UIEvent evt)
		{
			base.TouchesEnded(touches, evt);

			// canBubbleNatively: false => system might silently swallow the pointer after a few moves so we prefer to bubble in managed.
			_touchTarget?.TouchesEnded(touches, evt, canBubbleNatively: false);
			_touchTarget = null;
		}

		/// <inheritdoc />
		public override void TouchesCancelled(NSSet touches, UIEvent evt)
		{
			base.TouchesCancelled(touches, evt);

			// canBubbleNatively: false => system might silently swallow the pointer after a few moves so we prefer to bubble in managed.
			_touchTarget?.TouchesCancelled(touches, evt, canBubbleNatively: false);
			_touchTarget = null;
		}


		private TouchesManager _touchesManager;

		internal TouchesManager TouchesManager => _touchesManager ??= new NativeListViewBaseTouchesManager(this);

		private class NativeListViewBaseTouchesManager : TouchesManager
		{
			private readonly NativeListViewBase _listView;

			public NativeListViewBaseTouchesManager(NativeListViewBase listView)
			{
				_listView = listView;
			}

			/// <inheritdoc />
			protected override bool CanConflict(GestureRecognizer.Manipulation manipulation)
				=> manipulation.IsDragManipulation || _listView.ScrollOrientation switch
				{
					Orientation.Horizontal => manipulation.IsTranslateXEnabled,
					Orientation.Vertical => manipulation.IsTranslateYEnabled,
					_ => manipulation.IsTranslateXEnabled || manipulation.IsTranslateYEnabled
				};

			/// <inheritdoc />
			protected override void SetCanDelay(bool canDelay)
				=> _listView.DelaysContentTouches = canDelay;

			/// <inheritdoc />
			protected override void SetCanCancel(bool canCancel)
				=> _listView.CanCancelContentTouches = canCancel;
		}
		#endregion
	}
}
