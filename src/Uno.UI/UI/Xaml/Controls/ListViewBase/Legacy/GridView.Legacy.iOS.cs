using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.UI.Xaml;
using Uno.Extensions;
using Uno.Disposables;
using Microsoft.UI.Xaml.Controls;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using CGPoint = System.Drawing.PointF;
using nint = System.Int32;
using CGSize = System.Drawing.SizeF;
#endif

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
