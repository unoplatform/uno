using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Collections;
using System.Collections.Specialized;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using Windows.UI.Xaml;
using Android.Util;
using Java.Interop;
using Windows.UI.Xaml.Data;
using Android.Views.Accessibility;
using System.Collections.ObjectModel;
using Windows.UI.Core;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Controls.Legacy
{
	public class ListViewAdapter : ItemContainerHolderAdapter
	{
		#region Constants

		// ViewTypes that are materialized no matter what 'Items' contains
		private const int NumberOfStaticViewTypes = 4;

		// Make sure these IDs match the order in which view types are added in 'InitializeTemplates'
		//private const int DefaultViewType = 0; // no template
		private const int HeaderViewType = 1;
		private const int FooterViewType = 2;
		private const int GroupHeaderViewType = 3;

		#endregion

		#region Fields

		private SerialDisposable _notifyCollectionChanged = new SerialDisposable();
		private readonly SerialDisposable _notifyCollectionGroupsChanged = new SerialDisposable();

		private readonly List<object> _selectedItems = new List<object>();
		private readonly List<System.Tuple<ContainerType, DataTemplate>> _usedTemplates = new List<System.Tuple<ContainerType, DataTemplate>>();

		private object _header;
		private object _footer;
		private object _itemsSource;
		private DataTemplate _footerTemplate;
		private DataTemplate _itemTemplate;
		private DataTemplateSelector _itemTemplateSelector;
		private Style _itemContainerStyle;
		private GroupStyle _groupStyle;
		private DataTemplate _headerTemplate;
		private bool _needsRefresh;
		private IEnumerable _items;
		private HeaderWrapper _headerWrapper;
		private FooterWrapper _footerWrapper;

		#endregion

		#region Properties

		/// <summary>
		/// Should be equal or greater than number of unique DataTemplates used. Set by ListView.
		/// </summary>
		public int CustomViewTypeCount { get; set; }

		public Func<SelectorItem> ItemContainerFactory { get; set; }


		/// <summary>
		/// Gets the item from the source at the specified position.
		/// </summary>
		public object GetItemAt(int position)
		{
			var headerOffset = _headerWrapper != null ? 1 : 0;
			var footerOffset = _footerWrapper != null ? 1 : 0;

			if (position == 0 && headerOffset == 1)
			{
				return _headerWrapper;
			}

			if (footerOffset == 1 && position == GetItemsCount() - 1)
			{
				return _footerWrapper;
			}

			return _items.ElementAt(position - headerOffset);
		}

		/// <summary>
		/// Gets the number of items in the source.
		/// </summary>
		public int GetItemsCount()
		{
			var headerOffset = _headerWrapper != null ? 1 : 0;
			var footerOffset = _footerWrapper != null ? 1 : 0;

			return (_items?.Count() ?? 0) + headerOffset + footerOffset;
		}

		public List<object> SelectedItems => _selectedItems;

		public ICommand ItemClickCommand { get; set; }

		public DataTemplate HeaderTemplate
		{
			get { return _headerTemplate; }
			set { _headerTemplate = value; SetNeedsRefresh(); }
		}

		public DataTemplate FooterTemplate
		{
			get { return _footerTemplate; }
			set { _footerTemplate = value; SetNeedsRefresh(); }
		}

		public object ItemsSource
		{
			get { return _itemsSource; }
			set { _itemsSource = value; SetNeedsRefresh(); }
		}

		public object Header
		{
			get { return _header; }
			set { _header = value; SetNeedsRefresh(); }
		}

		public object Footer
		{
			get { return _footer; }
			set { _footer = value; SetNeedsRefresh(); }
		}

		public DataTemplate ItemTemplate
		{
			get { return _itemTemplate; }
			set { _itemTemplate = value; SetNeedsRefresh(); }
		}

		public DataTemplateSelector ItemTemplateSelector
		{
			get { return _itemTemplateSelector; }
			set { _itemTemplateSelector = value; SetNeedsRefresh(); }
		}

		public Style ItemContainerStyle
		{
			get { return _itemContainerStyle; }
			set { _itemContainerStyle = value; SetNeedsRefresh(); }
		}

		public GroupStyle GroupStyle
		{
			get { return _groupStyle; }
			set { _groupStyle = value; SetNeedsRefresh(); }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sets the selected state for the provided item.
		/// </summary>
		/// <param name="item">The item to alter selection</param>
		/// <param name="view">An optional view associated with the item being selected</param>
		/// <param name="selected">The new selection state</param>
		public void SetItemSelection(object item, SelectorItem view, bool selected)
		{
			if (selected)
			{
				_selectedItems.Add(item);
			}
			else
			{
				_selectedItems.Remove(item);
			}

			if (view == null)
			{
				// A single item may be mapped multiple times because of the recycling, and
				// we may not be able to know which one is actually displayed.
				SecondaryPool?.GetAllViews()
					.OfType<ItemContainerHolder>()
					.Select(i => i.Child as SelectorItem)
					.Trim()
					.Where(p => p.DataContext.SafeEquals(item))
					.ForEach(s => s.IsSelected = selected);
			}
			else
			{
				view.IsSelected = selected;
			}
		}

		private DataTemplate GetDataTemplateFromItem(object item)
		{
			return DataTemplateHelper.ResolveTemplate(
				ItemTemplate,
				ItemTemplateSelector,
				item,
				null
			);
		}

		private int GetItemViewTypeFromItem(object item)
		{
			var itemTemplate = GetDataTemplateFromItem(item);

			var viewType = Tuple.Create(ContainerType.Item, itemTemplate);

			if (!_usedTemplates.Contains(viewType))
			{
				_usedTemplates.Add(viewType);
			}

			return _usedTemplates.IndexOf(viewType);
		}

		protected void SetNeedsRefresh()
		{
			if (_needsRefresh)
			{
				return;
			}

			_needsRefresh = true;
			_ = CoreDispatcher.Main.RunAsync(
				CoreDispatcherPriority.Normal,
				() =>
			{
				Refresh();
				_needsRefresh = false;
			});
		}

		protected virtual void Refresh()
		{
			InitializeTemplates();
			InitializeItems();

			ObserveCollectionChanged();
			NotifyDataSetChanged();
		}

		private void InitializeTemplates()
		{
			// The order in which templates are added is important (the index is used as the view type id, see view type IDs in Constants #region)
			// The order is so important we do not clear on Refresh().
			_usedTemplates.AddDistinct(Tuple.Create(ContainerType.Item, default(DataTemplate))); // default, no template
			_usedTemplates.AddDistinct(Tuple.Create(ContainerType.Header, HeaderTemplate));
			_usedTemplates.AddDistinct(Tuple.Create(ContainerType.Footer, FooterTemplate));
			_usedTemplates.AddDistinct(Tuple.Create(ContainerType.GroupHeader, GroupStyle?.HeaderTemplate));
		}

		private void InitializeItems()
		{
			_items = ProcessItemsSource(_itemsSource);

			if (Header != null)
			{
				_headerWrapper = new HeaderWrapper(Header);
			}

			if (Footer != null)
			{
				_footerWrapper = new FooterWrapper(Footer);
			}
		}

		/// <summary>
		/// Returns a IEnumerable of the current ItemsSource (adds support for CollectionViewSource, groups, etc)
		/// </summary>
		/// <param name="itemsSource"></param>
		/// <returns></returns>
		private IEnumerable ProcessItemsSource(object itemsSource)
		{
			// extracts Source from CollectionViewSource (if applicable)
			var collectionViewSource = itemsSource as CollectionViewSource;
			if (collectionViewSource != null)
			{
				itemsSource = collectionViewSource.Source;
			}
			var collectionView = itemsSource as CollectionView;
			if (collectionView != null)
			{
				itemsSource = collectionView.CollectionGroups.Cast<ICollectionViewGroup>().Select(g => g.GroupItems);
			}

			// flattens GroupedEnumerable and insert group headers (wrapped inside GroupHeaderWrapper class)
			var groups = itemsSource as IEnumerable<IEnumerable<object>>; // won't work for Enumerable of value type
			if (groups != null)
			{
				if (GroupStyle?.HidesIfEmpty ?? false)
				{
					// removes empty groups
					groups = groups.Where(group => group.Any());
				}

				return groups.SelectMany(group => new object[] { new GroupHeaderWrapper(group) }.Concat(group)).ToList();
			}

			return itemsSource as IEnumerable ?? Enumerable.Empty<object>();
		}

		private void ObserveCollectionChanged()
		{
			NotifyCollectionChangedEventHandler handler = (s, e) => SetNeedsRefresh();

			var itemsSource = (_itemsSource as CollectionViewSource)?.Source ?? _itemsSource;

			var existingObservable = itemsSource as INotifyCollectionChanged;

			if (existingObservable != null)
			{

				existingObservable.CollectionChanged += handler;

				_notifyCollectionChanged.Disposable =
					Disposable.Create(() => existingObservable.CollectionChanged -= handler);
			}

			//Subscribe to changes of observable groups
			if (itemsSource is IEnumerable<IEnumerable> groups)
			{
				var disposables = new CompositeDisposable();
				foreach (var group in groups)
				{
					if (group is INotifyCollectionChanged observableGroup)
					{
						observableGroup.CollectionChanged += handler;
						Disposable.Create(() => observableGroup.CollectionChanged -= handler)
							.DisposeWith(disposables);
					}
				}
				_notifyCollectionGroupsChanged.Disposable = disposables;
			}
			else
			{
				_notifyCollectionGroupsChanged.Disposable = null;
			}
		}

		private ContentControl GenerateContainer(object item)
		{
			ContentControl container = null;

			if (item is HeaderWrapper)
			{
				container = new ListViewHeader
				{
					ContentTemplate = HeaderTemplate
				};
			}
			else if (item is FooterWrapper)
			{
				container = new ListViewFooter
				{
					ContentTemplate = FooterTemplate
				};
			}
			else if (item is GroupHeaderWrapper)
			{
				container = new Uno.UI.Controls.Legacy.ListViewHeaderItem
				{
					Style = GroupStyle?.HeaderContainerStyle,
					ContentTemplate = GroupStyle?.HeaderTemplate,
				};
			}
			else // item
			{
				container = ItemContainerFactory?.Invoke() ?? new SelectorItem() { ShouldHandlePressed = false };

				container.Style = ItemContainerStyle;
				container.ContentTemplate = GetDataTemplateFromItem(item);
			}

			// This is here to avoid disabling the ItemClick event when an Item Template has a button as any of its children.
			container.DescendantFocusability = DescendantFocusability.BlockDescendants;

			container.Binding("Content", "");

			FrameworkElement.InitializePhaseBinding(container);

			return container;
		}

		#endregion

		#region Adapter Implementation

		public override int GetItemViewType(int position)
		{
			var item = GetItemAt(position);

			var section = item as ItemWrapperBase;
			if (section != null)
			{
				return section.ViewType;
			}
			else
			{
				return GetItemViewTypeFromItem(item);
			}
		}

		public override long GetItemId(int position) => position;

		// we return null to Java here
		// - see @JonPryor's answer in http://stackoverflow.com/questions/13842864/why-does-the-gref-go-too-high-when-i-put-a-mvxbindablespinner-in-a-mvxbindableli/13995199#comment19319057_13995199
		public override Java.Lang.Object GetItem(int position) => null;

		public override int Count => GetItemsCount();

		// This value can't change after the adapter is set. Therefore, we assume there's a Header, a Footer and a GroupHeader template (NumberOfNonSelectableItemTypes)
		public override int ViewTypeCount => CustomViewTypeCount + NumberOfStaticViewTypes;

		public override bool IsEnabled(int position)
		{
			var item = GetItemAt(position);

			if (item is ItemWrapperBase)
			{
				return false;
			}
			else
			{
				if (ItemClickCommand != null)
				{
					return ItemClickCommand.CanExecute(item);
				}
				else
				{
					return base.IsEnabled(position);
				}
			}
		}

		protected override View GetContainerView(int position, View convertView, ViewGroup parent)
		{
			var item = GetItemAt(position);

			var itemContainer = convertView as ContentControl;
			if (itemContainer == null)
			{
				itemContainer = GenerateContainer(item);
			}

			// unwraps the actual value from the wrapper (used to identify the type of item)
			// needs to be done after the GenerateContainer step, as it relies on the presence of this wrapper
			var section = item as ItemWrapperBase;
			if (section != null)
			{
				item = section.Content;
			}

			var selectorItem = itemContainer as SelectorItem;
			if (selectorItem != null)
			{
				selectorItem.IsSelected = _selectedItems.Contains(item);
			}

			itemContainer.DataContext = item;

			FrameworkElement.RegisterPhaseBinding(itemContainer, a => SecondaryPool.RegisterForRecycled(itemContainer, a));

			return itemContainer;
		}

		#endregion

		#region Classes

		private abstract class ItemWrapperBase
		{
			public object Content { get; protected set; }
			public int ViewType { get; protected set; }
		}

		private class GroupHeaderWrapper : ItemWrapperBase
		{
			public GroupHeaderWrapper(object content)
			{
				Content = content;
				ViewType = GroupHeaderViewType;
			}
		}

		private class HeaderWrapper : ItemWrapperBase
		{
			public HeaderWrapper(object content)
			{
				Content = content;
				ViewType = HeaderViewType;
			}
		}

		private class FooterWrapper : ItemWrapperBase
		{
			public FooterWrapper(object content)
			{
				Content = content;
				ViewType = FooterViewType;
			}
		}

		private enum ContainerType
		{
			Header,
			Footer,
			GroupHeader,
			Item,
		}

		#endregion
	}
}
