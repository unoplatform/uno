using System;
using Windows.System;
using Uno.UI;
#if HAS_UNO_WINUI
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
#else
using Windows.UI.Xaml.Controls.Primitives;
#endif

#if XAMARIN_IOS_UNIFIED
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	public  partial class NavigationViewList : ListView
	{
		// Workaround for https://github.com/unoplatform/uno/issues/2477
		//
		// In case a container is hosted in another container (if
		// materialized through a DataTemplate), forward some of the properties
		// to the original container.

		private bool _isTemplateOwnContainer;

		protected override void OnItemTemplateChanged(DataTemplate oldItemTemplate, DataTemplate newItemTemplate)
		{
			base.OnItemTemplateChanged(oldItemTemplate, newItemTemplate);

			_isTemplateOwnContainer = ItemTemplate?.LoadContent() is NavigationViewItemBase;
		}

		private bool IsTemplateOwnContainer => _isTemplateOwnContainer;

		private DependencyObject CreateOwnContainer()
		{
			return new ListViewItem()
			{
				Padding = Thickness.Empty,
				VerticalContentAlignment = VerticalAlignment.Stretch,
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
			};
		}

		internal override int IndexFromContainerInner(DependencyObject container)
		{
			var index = base.IndexFromContainerInner(container);

			if (index == -1
				&& container is FrameworkElement fe
				&& fe.FindFirstParent<SelectorItem>() is SelectorItem parentItem)
			{
				index = base.IndexFromContainerInner(parentItem);
			}

			return index;
		}
	}
}
