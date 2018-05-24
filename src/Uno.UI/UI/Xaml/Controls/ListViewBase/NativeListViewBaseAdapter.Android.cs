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

namespace Windows.UI.Xaml.Controls
{
	public class NativeListViewBaseAdapter : RecyclerView.Adapter
	{
		private const int NoTemplateItemType = 0;
		private const int GroupHeaderItemType = -1;
		private const int HeaderItemType = -2;
		private const int FooterItemType = -3;

		private const int MaxRecycledViewsPerViewType = 10;
		
		internal NativeListViewBase Owner { get; set; }

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

			if (parent.GetIsGroupHeader(position) || parent.GetIsHeader(position) || parent.GetIsFooter(position))
			{
				var item = parent.GetElementFromDisplayPosition(position);

				var style = parent.GetIsGroupHeader(position)
					? parent.GetGroupStyle()?.HeaderContainerStyle
					: null;

				if (style != null)
				{
					container.Style = style;
				}

				var dataTemplate = GetDataTemplateFromItem(parent, item, viewType);

				container.DataContext = item;
				container.ContentTemplate = dataTemplate;
				container.Binding("Content", "");
			}
			else
			{
				parent.PrepareContainerForIndex(container, index);
			}

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
			if (viewType == GroupHeaderItemType)
			{
				return XamlParent?.GetGroupHeaderContainer(null);
			}
			if (viewType == HeaderItemType || viewType == FooterItemType)
			{
				return ContentControl.CreateItemContainer();
			}

			return XamlParent?.GetContainerForIndex(-1) as ContentControl;
		}

		public override int GetItemViewType(int position)
		{
			if (XamlParent?.GetIsGroupHeader(position) ?? false)
			{
				return GroupHeaderItemType;
			}
			if (XamlParent?.GetIsHeader(position) ?? false)
			{
				return HeaderItemType;
			}
			if (XamlParent?.GetIsFooter(position) ?? false)
			{
				return FooterItemType;
			}

			var item = XamlParent?.GetElementFromDisplayPosition(position);
			var template = GetDataTemplateFromItem(XamlParent, item, null);

			return template?.GetHashCode() ?? NoTemplateItemType;
		}

		private static DataTemplate GetDataTemplateFromItem(ListViewBase parent, object item, int? itemViewType)
		{
			if (itemViewType == GroupHeaderItemType)
			{
				return parent.GetGroupStyle()?.HeaderTemplate;
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
				 item
			 );
		}
	}
}
