using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Uno.Disposables;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;

using Foundation;
using UIKit;
using CoreGraphics;

namespace Uno.UI.Controls.Legacy
{
	// FIXME Merge with ListView and remove the ListViewTypeSelector in favor of using a generic "ListViewItem" and ItemTemplate for its content
	public partial class ListView : ListViewBase, IListView
	{

		#region Properties
		public override ListViewBaseLayout Layout
		{
			get
			{
				return base.Layout;
			}

			set
			{
				if (!(value is ListViewLayout))
				{
					throw new InvalidOperationException("ListView only supports ListViewLayout");
				}

				base.Layout = value;
			}
		}

		new internal ListViewSource Source
		{
			get { return base.Source as ListViewSource; }
			set
			{
				var oldSource = Source;
				base.Source = value;
			}
		}

		public override DataTemplate ItemTemplate
		{
			get
			{
				return base.ItemTemplate;
			}
			set
			{
				base.ItemTemplate = value;

				if (this.Superview != null)
				{
					// When the template changes, and the list has already
					// been displayed once, we need to refresh the default item size.
					SetDefaultItemSize();
				}
			}
		}

		public ItemsPanelTemplate ItemsPanel { get; set; }
		#endregion

		public ListView()
			: base(new RectangleF(), new ListViewLayout())
		{
			Initialize();
		}

		private void Initialize()
		{
			var internalContainerType = typeof(ListViewBaseSource.InternalContainer);
			RegisterClassForCell(internalContainerType, ListViewItemReuseIdentifier);
			RegisterClassForSupplementaryView(internalContainerType, ListViewHeaderElementKindNS, ListViewHeaderReuseIdentifier);
			RegisterClassForSupplementaryView(internalContainerType, ListViewFooterElementKindNS, ListViewFooterReuseIdentifier);
			RegisterClassForSupplementaryView(internalContainerType, ListViewSectionHeaderElementKindNS, ListViewSectionHeaderReuseIdentifier);

			Source = new ListViewSource(this);

			Source.SelectionChanged += (sender, e) =>
			{
				if (!e.AddedItems.Equals(e.RemovedItems))
				{
					SetBindingValue((e.AddedItems.Count > 0) ? e.AddedItems[0] : null, "SelectedItem");
				}
			};
		}

		#region Overrides
		public override CGSize SizeThatFits(CGSize size)
		{
			var result = Layout.SizeThatFits(size);
			return result;
		}

		public override void MovedToSuperview()
		{
			base.MovedToSuperview();

			SetDefaultItemSize();
		}
		#endregion

		#region Properties Changed
		protected override void OnSourceChanged(ListViewBaseSource oldSource, ListViewBaseSource newSource)
		{
			base.OnSourceChanged(oldSource, newSource);

			var newListViewSource = newSource as ListViewSource;
			if (newListViewSource != null)
			{
				Layout.Source = new WeakReference<ListViewBaseSource>(newListViewSource);
			}
		}

		public void OnSelectionHasChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!e.AddedItems.Equals(e.RemovedItems))
			{
				SetBindingValue((e.AddedItems.Count > 0) ? e.AddedItems[0] : null, "SelectedItem");
			}
		}
		#endregion

		private void SetDefaultItemSize()
		{
			if (ItemTemplate != null)
			{
				using (var template = ItemTemplate.LoadContent())
				{
					var listviewLayout = Layout as ListViewLayout;

					var frameworkElement = template as IFrameworkElement;

					if (frameworkElement != null && !double.IsNaN(frameworkElement.Height))
					{
						listviewLayout.ItemHeight = frameworkElement.Height;
					}
					else
					{
						var measuredSize = template.SizeThatFits(new CGSize(double.MaxValue, double.MaxValue));
						listviewLayout.ItemHeight = (double)measuredSize.Height;
					}

				}
			}
		}
	}
}
