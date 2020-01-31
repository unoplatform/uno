using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Foundation;
#if __IOS__
using _ReusableView = UIKit.UICollectionReusableView;
#else
using _ReusableView = AppKit.INSCollectionViewElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class NativeListViewBase
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
#endregion

		public Thickness Padding
		{
			get { return NativeLayout.Padding; }
			set { NativeLayout.Padding = value; }
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
if (NativeLayout != null)
					{
						// Copy previous padding
						value.Padding = Padding;
					}
				}

				CollectionViewLayout = value;
			}
		}

		private bool _needsReloadData = false;
		internal bool NeedsReloadData => _needsReloadData;

		/// <summary>
		/// ReloadData() has been called, but the layout hasn't been updated. During this window, in-place modifications to the
		/// collection (InsertItems, etc) shouldn't be called because they will result in a NSInternalInconsistencyException
		/// </summary>
		private bool _needsLayoutAfterReloadData = false;
		/// <summary>
		/// List was empty last time ReloadData() was called. If inserting items into an empty collection we should do a refresh instead, 
		/// to work around a UICollectionView bug https://stackoverflow.com/questions/12611292/uicollectionview-assertion-failure
		/// </summary>
		private bool _listEmptyLastRefresh = false;

		public Style ItemContainerStyle => XamlParent?.ItemContainerStyle;

		public DataTemplate HeaderTemplate
		{
			get { return XamlParent?.HeaderTemplate; }
		}

		public DataTemplate FooterTemplate
		{
			get { return XamlParent?.FooterTemplate; }
		}

		public DataTemplateSelector ItemTemplateSelector => XamlParent?.ItemTemplateSelector;

		public GroupStyle GroupStyle => XamlParent?.GroupStyle.FirstOrDefault();

		internal object SelectedItem => XamlParent?.SelectedItem;

		internal IList<object> SelectedItems => XamlParent?.SelectedItems;

		public ListViewSelectionMode SelectionMode => XamlParent?.SelectionMode ?? ListViewSelectionMode.None;
		public object Header
		{
			get { return XamlParent?.ResolveHeaderContext(); }
		}

		public object Footer
		{
			get { return XamlParent?.ResolveFooterContext(); }
		}

		/// <summary>
		/// Get all currently visible supplementary views.
		/// </summary>
		internal IEnumerable<_ReusableView> VisibleSupplementaryViews
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
	}
}
