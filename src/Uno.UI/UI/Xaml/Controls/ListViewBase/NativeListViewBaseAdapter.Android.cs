using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Android.Support.V7.Widget;
using Android.Views;
using Uno.Client;
using Uno.Extensions;
using Uno.UI.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls.Primitives;
using System.Linq;
using Uno.Logging;
using Uno.UI;
using Java.Lang;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.UI.Core;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
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
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Binding view with view type {viewType} at position {position}.");
			}

			var isGroupHeader = parent.GetIsGroupHeader(position);
			if (isGroupHeader || parent.GetIsHeader(position) || parent.GetIsFooter(position))
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

				container.DataContext = item;
				container.ContentTemplate = dataTemplate;
				if (container.GetBindingExpression(ContentControl.ContentProperty) == null)
				{
					container.SetBinding(ContentControl.ContentProperty, new Binding());
				}
			}
			else if(viewType == IsOwnContainerType)
			{
				holder.ItemView = parent?.GetContainerForIndex(index) as View;
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

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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

			return XamlParent?.GetContainerForIndex(-1) as ContentControl;
		}

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
			var template = GetDataTemplateFromItem(XamlParent, item, null, isGroupHeader);

			int viewType;
			if (template != null)
			{
				viewType = template.GetHashCode();
			}
			else
			{
				if (XamlParent?.IsItemItsOwnContainer(item) ?? false)
				{
					viewType = IsOwnContainerType;
				}
				else
				{
					viewType = isGroupHeader ? NoTemplateGroupHeaderType : NoTemplateItemType;
				}
			}

			if (isGroupHeader)
			{
				_groupHeaderItemTypes.Add(viewType);
			}

			return viewType;
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
			NotifyDataSetChanged();
		}
	}
}
