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
using Windows.UI.Core;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Controls
{
	[Windows.UI.Xaml.Data.Bindable]
	public class BindableListAdapter : ItemContainerHolderAdapter
	{
		private Android.Content.Context _context;
		private IEnumerable _itemsSource;
		private SerialDisposable _notifyCollectionChanged = new SerialDisposable();

		private object _header;
		private object _footer;
		private DataTemplate _footerTemplate;
		private DataTemplate _itemTemplate;
		private DataTemplateSelector _itemTemplateSelector;
		private Style _itemContainerStyle;
		private DataTemplate _headerTemplate;
		private HashSet<object> _selectedItems = new HashSet<object>();

		/// <summary>
		/// All unique templates used; used to return ViewTypeCount
		/// </summary>
		private List<DataTemplate> _usedTemplates = new List<DataTemplate>();

		public IEnumerable<object> SelectedItems
		{
			get { return _selectedItems; }
		}

		private readonly List<View> _allocatedViews = new List<View>();

		public BindableListAdapter(Android.Content.Context context, object header = null, object footer = null)
		{
			_context = context;
			_header = header;
			_footer = footer;
		}

		public ICommand ItemClickCommand { get; set; }

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

		/// <summary>
		/// Sets the default item template for this adapter.
		/// </summary>
		public DataTemplate ItemTemplate
		{
			get { return _itemTemplate; }
			set { _itemTemplate = value; SetNeedsRefresh(); }
		}

		/// <summary>
		/// Provides an ItemTemplateSelector that will alter the item template based on the databound item.
		/// </summary>
		public DataTemplateSelector ItemTemplateSelector
		{
			get { return _itemTemplateSelector; }
			set { _itemTemplateSelector = value; SetNeedsRefresh(); }
		}

		/// <summary>
		/// Defines an item container for all items of the adapter. The default is a 
		/// <see cref="BasicVerticalItemContainer"/> that allows for items to use the full space.
		/// </summary>
		public Func<SelectorItem> ItemContainerTemplate { get; set; }

		/// <summary>
		/// Sets the style to be applied to displayed containers
		/// </summary>
		public Style ItemContainerStyle
		{
			get { return _itemContainerStyle; }
			set { _itemContainerStyle = value; SetNeedsRefresh(); }
		}


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
				_allocatedViews
					.OfType<SelectorItem>()
					.Where(p => p.DataContext == item)
					.ForEach(s => s.IsSelected = selected);
			}
			else
			{
				view.IsSelected = selected;
			}
		}

		public override int Count
		{
			get
			{
				var list = _itemsSource as IList;

				var count = list != null ? list.Count : _itemsSource.Count();

				if (HasHeader())
				{
					count += 1;
				}

				if (HasFooter())
				{
					count += 1;
				}

				return count;
			}
		}

		public int GetPosition(object item)
		{
			if (item == Header)
			{
				return 0;
			}
			else if (item == Footer)
			{
				return Count - 1;
			}
			else
			{
				var position = _itemsSource.IndexOf(item);

				if (HasHeader())
				{
					position += 1;
				}

				return position;
			}
		}

		/// <summary>
		/// Get item corresponding to <paramref name="position"/> in list, including header and footer if present
		/// </summary>
		/// <param name="position">Position in list</param>
		/// <returns>Corresponding item from items source, header, or footer</returns>
		public System.Object GetRawItem(int position)
		{
			if (IsHeader(position))
			{
				return Header;
			}
			else if (IsFooter(position))
			{
				return Footer;
			}
			else
			{
				return _itemsSource.ElementAt(AdjustPosition(position));
			}
		}

		public override bool IsEnabled(int position)
		{
			if ((HasHeader() && position == 0) || (HasFooter() && position == Count - 1))
			{
				return false;
			}
			else
			{
				if (ItemClickCommand != null)
				{
					return ItemClickCommand.CanExecute(GetRawItem(position));
				}
				else
				{
					return base.IsEnabled(position);
				}
			}
		}

		protected Android.Content.Context Context
		{
			get { return _context; }
		}

		public override Java.Lang.Object GetItem(int position)
		{
			// we return null to Java here
			// - see @JonPryor's answer in http://stackoverflow.com/questions/13842864/why-does-the-gref-go-too-high-when-i-put-a-mvxbindablespinner-in-a-mvxbindableli/13995199#comment19319057_13995199
			return null;
			//return new MvxJavaContainer<object>(GetRawItem(position));
		}

		public override long GetItemId(int position)
		{
			return position;
		}

		protected override View GetContainerView(int position, View convertView, ViewGroup parent)
		{
			if (IsHeader(position))
			{
				return GetSectionView(Header, HeaderTemplate, convertView);
			}
			else if (IsFooter(position))
			{
				return GetSectionView(Footer, FooterTemplate, convertView);
			}
			else
			{
				return GetItemView(position, convertView, parent, ItemTemplateId);
			}
		}

		protected virtual View GetItemView(int position, View convertView, ViewGroup parent, int templateId)
		{
			if (_itemsSource == null)
			{
				this.Log().Debug("GetView called when ItemsSource is null");
				return null;
			}

			var source = GetRawItem(position);

			var view = GetBindableView(convertView, source, parent, templateId);

			var selectorItem = view as SelectorItem;

			if (selectorItem != null)
			{
				selectorItem.IsSelected = _selectedItems.Contains(source);
			}

			return view;
		}

		private View GetSectionView(object source, DataTemplate template, View convertView)
		{
			var view = convertView;

			if (view == null)
			{
				view = template != null ? ((Func<View>)template)() : new TextBlock()
				{
					Text = source?.ToString()
				};
			}

			var provider = view as IDataContextProvider;

			if (provider != null)
			{
				provider.DataContext = source;
			}

			return view;
		}

		protected virtual View GetBindableView(View convertView, object source, ViewGroup parent, int templateId)
		{
			if (templateId == 0)
			{
				// no template seen - so use a standard string view from Android and use ToString()
				var view = GetSimpleView(convertView, source, parent);

				var bindable = view as IDataContextProvider;

				var viewGroup = view as ViewGroup;
				if (viewGroup != null)
				{
					// This is here to avoid disabling the ItemClick event when an Item Template has a button 
					// as any of its children.
					viewGroup.DescendantFocusability = Android.Views.DescendantFocusability.BlockDescendants;
				}

				if (bindable != null)
				{
					bindable.DataContext = source;
				}

				return view;
			}

			var bindableView = convertView as BindableView;

			if (convertView == null || bindableView.LayoutId != templateId)
			{
				bindableView = new BindableView(_context, templateId);

				_allocatedViews.Add(bindableView);
			}

			bindableView.DataContext = source;

			return bindableView;
		}

		private View GetSimpleView(View convertView, object source, ViewGroup parent)
		{
			if (ItemTemplate != null || ItemTemplateSelector != null)
			{
				if (convertView == null)
				{
					if (ItemContainerTemplate != null)
					{
						var container = ItemContainerTemplate() as SelectorItem;

						if (container != null)
						{
							container.Style = ItemContainerStyle;
							container.ContentTemplateSelector = ItemTemplateSelector;
							container.ContentTemplate = ItemTemplate;
							container.Binding("Content", "");
						}
						else
						{
							throw new InvalidOperationException("The ItemContainerTemplate must be a SelectorItem");
						}

						_allocatedViews.Add(container);
						return container;
					}
					else
					{
						var template = DataTemplateHelper.ResolveTemplate(
							this.ItemTemplate,
							this.ItemTemplateSelector,
							source,
							null
						);

						var templateView = template?.LoadContentCached();

						_allocatedViews.Add(templateView);
						return templateView;
					}
				}
				else
				{
					return convertView;
				}
			}
			else
			{
				var container = ItemContainerTemplate?.Invoke() ?? new SelectorItem() { ShouldHandlePressed = false };

				container.Style = ItemContainerStyle;
				container.ContentTemplate = GetDataTemplateFromItem(source);
				container.Binding("Content", "");

				return container;
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

		public int ItemTemplateId { get; set; }

		public System.Collections.IEnumerable ItemsSource
		{
			get { return _itemsSource; }
			set
			{
				if (_itemsSource != value)
				{
					_itemsSource = value;

					SetNeedsRefresh();
				}
			}
		}

		private bool _needsRefresh;

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
			ObserveCollectionChanged();
			NotifyDataSetChanged();
		}

		private void ObserveCollectionChanged()
		{
			var existingObservable = _itemsSource as INotifyCollectionChanged;

			if (existingObservable != null)
			{
				NotifyCollectionChangedEventHandler handler = (s, e) => OnItemsSourceCollectionChanged(e);

				existingObservable.CollectionChanged += handler;

				_notifyCollectionChanged.Disposable = Disposable.Create(() => existingObservable.CollectionChanged -= handler);
			}
		}

		private void OnItemsSourceCollectionChanged(NotifyCollectionChangedEventArgs c)
		{
			NotifyDataSetChanged();
		}

		protected bool HasHeader()
		{
			return Header != null;
		}

		protected bool HasFooter()
		{
			return Footer != null;
		}

		protected bool IsHeader(int position)
		{
			return HasHeader() && position == 0;
		}

		protected bool IsFooter(int position)
		{
			return HasFooter() && position == Count - 1;
		}

		protected int AdjustPosition(int position)
		{
			if (HasHeader())
			{
				position -= 1;
			}

			return position;
		}

		public override int ViewTypeCount
		{
			get
			{
				var count = 1;

				if (HasHeader())
				{
					count += 1;
				}

				if (HasFooter())
				{
					count += 1;
				}

				count += CustomViewTypeCount;

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"Returning ViewTypeCount: {count}");
				}
				return count;
			}
		}

		/// <summary>
		/// Should be equal or greater than number of unique DataTemplates used. Set by ListView.
		/// </summary>
		public int CustomViewTypeCount { get; set; }

		public override int GetItemViewType(int position)
		{
			if (IsHeader(position))
			{
				return 1;
			}
			else if (IsFooter(position))
			{
				return HasHeader() ? 2 : 1;
			}

			return GetItemViewTypeFromTemplate(GetRawItem(position));
		}

		private int GetItemViewTypeFromTemplate(object item)
		{
			var itemTemplate = DataTemplateHelper.ResolveTemplate(
				this.ItemTemplate,
				this.ItemTemplateSelector,
				item,
				null
			);

			if (itemTemplate == null)
			{
				return 0;
			}

			if (!_usedTemplates.Contains(itemTemplate))
			{
				_usedTemplates.Add(itemTemplate);
			}

			var toReturn = _usedTemplates.IndexOf(itemTemplate)
				//Add 1 because 0 represents null (no template)
				+ 1
				+ (HasHeader() ? 1 : 0)
				+ (HasFooter() ? 1 : 0);
			return toReturn;
		}
	}
}
