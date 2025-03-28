using System;
using System.Collections.Generic;
using System.Drawing;
using Windows.UI.Xaml;
using Uno.Extensions;
using Uno.Disposables;
using Windows.UI.Xaml.Controls;

using Foundation;
using UIKit;
using CoreGraphics;

namespace Uno.UI.Controls.Legacy
{
	public partial class GridView : ListViewBase
	{
		public GridView()
			: base(new RectangleF(), new GridViewWrapGridLayout())
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

			Source = new GridViewSource(this);

			Source.SelectionChanged += (sender, e) =>
			{
				if (!e.AddedItems.Equals(e.RemovedItems))
				{
					SetBindingValue((e.AddedItems.Count > 0) ? e.AddedItems[0] : null, "SelectedItem");
				}
			};
		}

		protected GridView(RectangleF frame)
			: base(frame, new GridViewWrapGridLayout())
		{
		}

		public void RegisterItemType(Type itemType)
		{
			RegisterClassForCell(itemType, new NSString(itemType.Name));
		}

		new public GridViewSource Source
		{
			get
			{
				return base.Source as GridViewSource;
			}
			set
			{
				base.Source = value;
			}
		}

		protected override void OnSourceChanged(ListViewBaseSource oldSource, ListViewBaseSource newSource)
		{
			base.OnSourceChanged(oldSource, newSource);

			var asGridViewSource = newSource as GridViewSource;
			if (asGridViewSource != null)
			{
				Layout.Source = new WeakReference<ListViewBaseSource>(asGridViewSource);
			}
		}

		public void OnSelectionHasChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!e.AddedItems.Equals(e.RemovedItems))
			{
				SetBindingValue((e.AddedItems.Count > 0) ? e.AddedItems[0] : null, "SelectedItem");
			}
		}

	}
}
