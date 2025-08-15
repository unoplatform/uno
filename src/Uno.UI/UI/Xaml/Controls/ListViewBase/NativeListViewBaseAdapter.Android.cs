using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AndroidX.RecyclerView.Widget;
using Android.Views;
using Uno.Client;
using Uno.Extensions;
using Uno.UI.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Linq;
using Uno.Foundation.Logging;
using Uno.UI;
using Java.Lang;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.UI.Core;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls
{
	public class NativeListViewBaseAdapter : RecyclerView.Adapter
	{
		private const int NoTemplateItemType = 0;
		private const int NoTemplateGroupHeaderType = -1;
		private const int HeaderItemType = -2;
		private const int FooterItemType = -3;
		private const int IsOwnContainerType = -4;

		private const int MaxRecycledViewsPerViewType = 10;

		private readonly HashSet<int> _groupHeaderItemTypes = new HashSet<int>();

		/// <summary>
		/// Known templates cached for each view type.
		/// </summary>
		private readonly Dictionary<int, DataTemplate> _itemTemplatesByViewType = new Dictionary<int, DataTemplate>();

		private ManagedWeakReference _ownerWeakReference;

		internal NativeListViewBase Owner
		{
			get => _ownerWeakReference?.Target as NativeListViewBase;
			set
			{
				WeakReferencePool.ReturnWeakReference(this, _ownerWeakReference);
				_ownerWeakReference = WeakReferencePool.RentWeakReference(this, value);
			}
		}

		private ListViewBase XamlParent { get { return Owner?.XamlParent; } }

		public override int ItemCount
		{
			get
			{
				var count = XamlParent?.GetDisplayItemCount();
				return count ?? 0;
			}
		}

		public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
		{
			var parent = XamlParent;

			var index = parent?.ConvertDisplayPositionToIndex(position) ?? -1;
			var container = holder.ItemView as ContentControl;

			var viewType = GetItemViewType(position);
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Binding view with view type {viewType} at position {position}.");
			}

			var isGroupHeader = parent.GetIsGroupHeader(position);
			var isHeader = parent.GetIsHeader(position);
			var isFooter = parent.GetIsFooter(position);
			if (isGroupHeader || isHeader || isFooter)
			{
				var item = parent.GetElementFromDisplayPosition(position);

				var style = parent.GetIsGroupHeader(position)
					? parent.GetGroupStyle()?.HeaderContainerStyle
					: null;

				if (style != null)
				{
					container.Style = style;
				}

				var dataTemplate = GetDataTemplateFromItem(parent, item, viewType, isGroupHeader);
				container.ContentTemplate = dataTemplate;

				if (!isHeader && !isFooter)
				{
					container.DataContext = item;
					if (container.GetBindingExpression(ContentControl.ContentProperty) == null)
					{
						container.SetBinding(ContentControl.ContentProperty, new Binding());
					}
				}
				else
				{
					// When showing the header/footer, the datacontext must be the listview's
					// datacontext. We only need to set the content of the container, not its
					// datacontext.

					container.Content = item;
				}
			}
			else if (viewType == IsOwnContainerType)
			{
				var itemContainer = parent?.GetContainerForIndex(index);
				if (itemContainer is not null)
				{
					parent?.PrepareContainerForIndex(itemContainer, index);
				}

				holder.ItemView = itemContainer as View;
			}
			else
			{
				parent.PrepareContainerForIndex(container, index);
			}

			RegisterPhaseBinding(holder);
		}

		/// <summary>
		/// Register provided list item for Phase binding.
		/// </summary>
		internal void RegisterPhaseBinding(RecyclerView.ViewHolder holder)
		{
			FrameworkElement.RegisterPhaseBinding(holder.ItemView as ContentControl, a => Owner.ViewCache.OnRecycled(holder as UnoViewHolder, a));
		}

		public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
		{
			if (viewType != HeaderItemType && viewType != FooterItemType)
			{
				var pool = Owner.GetRecycledViewPool();
				pool.SetMaxRecycledViews(viewType, MaxRecycledViewsPerViewType);
			}

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Creating view with view type {viewType}.");
			}
			var container = GenerateContainer(viewType);
			var holder = new UnoViewHolder(container);

			FrameworkElement.InitializePhaseBinding(holder.ItemView as ContentControl);

			return holder;
		}

		private ContentControl GenerateContainer(int viewType)
		{
			if (viewType == HeaderItemType || viewType == FooterItemType || viewType == IsOwnContainerType)
			{
				return ContentControl.CreateItemContainer();
			}
			if (_groupHeaderItemTypes.Contains(viewType))
			{
				return XamlParent?.GetGroupHeaderContainer(null);
			}

			//return XamlParent?.GetContainerForIndex(-1) as ContentControl;
			var template = _itemTemplatesByViewType.UnoGetValueOrDefault(viewType);
			// 
			return XamlParent?.GetContainerForTemplate(template) as ContentControl;
		}

		// Get the 'view type' for the element at the given position. Elements with the same 'view type' have the same reuse
		// characteristics - eg, they can share containers via recycling, or in some cases they should not be recycled at all.
		public override int GetItemViewType(int position)
		{
			if (XamlParent?.GetIsHeader(position) ?? false)
			{
				return HeaderItemType;
			}
			if (XamlParent?.GetIsFooter(position) ?? false)
			{
				return FooterItemType;
			}

			var item = XamlParent?.GetElementFromDisplayPosition(position);
			var isGroupHeader = XamlParent?.GetIsGroupHeader(position) ?? false;
			var isItemsItsOwnContainer = XamlParent?.IsItemItsOwnContainer(item) ?? false;
			var template = GetDataTemplateFromItem(XamlParent, item, null, isGroupHeader);

			var viewType = (isItemsItsOwnContainer, isGroupHeader, template) switch
			{
				(true, _, _) => IsOwnContainerType, // IsOwnContainer takes precedence
				(false, _, DataTemplate _) => GetViewTypeAndRegister(template), // Use template for identifier if possible
				(false, true, null) => NoTemplateGroupHeaderType, // Constant viewTypes when template is null
				(false, false, null) => NoTemplateItemType,
			};

			if (isGroupHeader)
			{
				_groupHeaderItemTypes.Add(viewType);
			}

			return viewType;

			int GetViewTypeAndRegister(DataTemplate dataTemplate)
			{
				var id = dataTemplate.GetHashCode();
				_itemTemplatesByViewType[id] = dataTemplate;
				return id;
			}
		}

		private static DataTemplate GetDataTemplateFromItem(ListViewBase parent, object item, int? itemViewType, bool isGroupHeader)
		{
			if (isGroupHeader)
			{
				var groupStyle = parent.GetGroupStyle();

				return DataTemplateHelper.ResolveTemplate(
					groupStyle?.HeaderTemplate,
					groupStyle?.HeaderTemplateSelector,
					item,
					parent
				);
			}
			if (itemViewType == HeaderItemType)
			{
				return parent.HeaderTemplate;
			}
			if (itemViewType == FooterItemType)
			{
				return parent.FooterTemplate;
			}
			return DataTemplateHelper.ResolveTemplate(
				 parent?.ItemTemplate,
				 parent?.ItemTemplateSelector,
				 item,
				 parent
			 );
		}

		internal void Refresh()
		{
			_groupHeaderItemTypes.Clear();
			_itemTemplatesByViewType.Clear();
			NotifyDataSetChanged();
		}
	}
}
