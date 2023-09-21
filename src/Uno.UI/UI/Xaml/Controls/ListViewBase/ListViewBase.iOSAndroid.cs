#if __IOS__ || __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Uno.UI.Controls;
#if __ANDROID__
using _ViewGroup = Android.Views.ViewGroup;
#elif __IOS__
using _ViewGroup = UIKit.UIView;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ListViewBase
	{
		/// <summary>
		/// The native ListView type which is providing the implementation. This will be null if a panel other than <see cref="ItemsStackPanel"/>
		/// or <see cref="ItemsWrapGrid"/> is being used.
		/// </summary>
		internal NativeListViewBase NativePanel => InternalItemsPanelRoot as NativeListViewBase;

		/// <summary>
		/// The managed implementation of ItemsStackPanel, if it is set as ItemsPanel (normally only in a debugging scenario).
		/// </summary>
		private ManagedItemsStackPanel ManagedVirtualizingPanel => ItemsPanelRoot as ManagedItemsStackPanel;

		private protected override bool ShouldItemsControlManageChildren => ItemsPanelRoot == InternalItemsPanelRoot && ManagedVirtualizingPanel == null;

		// TODO: This is a temporary workaround for TabView items stretching vertically
		// Can be removed when #1133 is fixed.
		internal bool ShouldApplyChildStretch { get; set; } = true;

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

				if (ScrollViewer?.Style == null)
				{
					throw new InvalidOperationException($"Performance hit: {this} is using a ScrollViewer in its template with a default style, which would break virtualization. A Style containing {nameof(ListViewBaseScrollContentPresenter)} must be used.");
				}

				if (ScrollViewer != null)
				{
					NativePanel.HorizontalScrollBarVisibility = ScrollViewer.HorizontalScrollBarVisibility;
					NativePanel.VerticalScrollBarVisibility = ScrollViewer.VerticalScrollBarVisibility;
				}
			}
			else
			{
				if (ScrollViewer?.Style != null && ScrollViewer.Style == ResourceResolver.GetSystemResource<Style>("ListViewBaseScrollViewerStyle")) //TODO: this, too, properly
				{
					// We're not using NativeListViewBase so we need a 'real' ScrollViewer, remove the internal custom style
					ScrollViewer.Style = null;
				}
			}
		}

		protected override _ViewGroup ResolveInternalItemsPanel(_ViewGroup itemsPanel)
		{
			// If the items panel is a virtualizing panel, we substitute it with NativeListViewBase
			var virtualizingPanel = itemsPanel as IVirtualizingPanel;
			if (virtualizingPanel != null)
			{
				var layouter = virtualizingPanel.GetLayouter();
				layouter.ShouldApplyChildStretch = ShouldApplyChildStretch;
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

		protected internal override void CleanUpInternalItemsPanel(_ViewGroup panel)
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
