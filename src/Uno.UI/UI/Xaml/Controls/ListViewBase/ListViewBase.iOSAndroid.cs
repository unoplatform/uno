#if XAMARIN_IOS || XAMARIN_ANDROID
using System;
using System.Collections.Generic;
using System.Text;
#if XAMARIN_ANDROID
using _View = Android.Views.View;
#elif XAMARIN_IOS
using _View = UIKit.UIView;
#else
using View = Windows.UI.Xaml.FrameworkElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ListViewBase
	{
		internal NativeListViewBase NativePanel { get { return InternalItemsPanelRoot as NativeListViewBase; } }

		/// <summary>
		/// The number of currently visible items, ie a 'page' from the point of view of incremental data loading.
		/// </summary>
		private int PageSize
		{
			get
			{
				if (NativePanel == null)
				{
					// Not supported
					return 0;
				}
				var lastVisibleIndex = NativePanel.NativeLayout.LastVisibleIndex;
				var firstVisibleIndex = NativePanel.NativeLayout.FirstVisibleIndex;
				if (lastVisibleIndex == -1)
				{
					return 0;
				}
				return lastVisibleIndex + 1 - firstVisibleIndex;
			}
		}

		partial void OnApplyTemplatePartial()
		{
			// NativePanel may not exist if we're using a non-virtualizing ItemsPanel.
			if (NativePanel != null)
			{
				// Propagate the DataContext manually, since ItemsPanelRoot isn't really part of the visual tree
				ItemsPanelRoot.SetValue(DataContextProperty, DataContext, DependencyPropertyValuePrecedences.Inheritance);

				//if (ScrollViewer?.Style?.Precedence == DependencyPropertyValuePrecedences.ImplicitStyle) //TODO: this, properly
				//{
				//	throw new InvalidOperationException($"Performance hit: {this} is using a ScrollViewer in its template with a default style, which would break virtualization. A Style containing {nameof(ListViewBaseScrollContentPresenter)} must be used.");
				//}

				if (ScrollViewer != null)
				{
					NativePanel.HorizontalScrollBarVisibility = ScrollViewer.HorizontalScrollBarVisibility;
					NativePanel.VerticalScrollBarVisibility = ScrollViewer.VerticalScrollBarVisibility;
				}
			}
			else
			{
				//if (ScrollViewer?.Style == Uno.UI.GlobalStaticResources.ListViewBaseScrollViewerStyle) //TODO: this, too, properly
				//{
				//	// We're not using NativeListViewBase so we need a 'real' ScrollViewer
				//	ScrollViewer.Style = Uno.UI.GlobalStaticResources.DefaultScrollViewerStyle;
				//}
			}
		}

		protected override _View ResolveInternalItemsPanel(_View itemsPanel)
		{
			// If the items panel is a virtualizing panel, we substitute it with NativeListViewBase
			var virtualizingPanel = itemsPanel as IVirtualizingPanel;
			if (virtualizingPanel != null)
			{
				var layouter = virtualizingPanel.GetLayouter();
				PrepareNativeLayout(layouter);

				var panel = new NativeListViewBase();
				panel.XamlParent = this;
				panel.NativeLayout = layouter;
				panel.BindToEquivalentProperty(virtualizingPanel, "Background");
				InitializeNativePanel(panel);

				return panel;
			}
			else
			{
				// Otherwise act as a normal ItemsControl
				return base.ResolveInternalItemsPanel(itemsPanel);
			}
		}

		protected internal override void CleanUpInternalItemsPanel(_View panel)
		{
			if (panel is NativeListViewBase nativePanel)
			{
				CleanUpNativePanel(nativePanel);
			}
		}

		private void TryLoadMoreItems() => TryLoadMoreItems(NativePanel.NativeLayout.LastVisibleIndex);

		partial void CleanUpNativePanel(NativeListViewBase panel);
	}
}
#endif
