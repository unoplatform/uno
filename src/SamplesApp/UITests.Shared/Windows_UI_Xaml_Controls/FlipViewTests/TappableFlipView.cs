using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UITests.Shared.Windows_UI_Xaml_Controls.FlipViewTests
{
	public partial class TappableFlipView : FlipView
	{
		public TappableFlipView()
		{
#if NETFX_CORE
			DefaultStyleKey = typeof(FlipView);
#endif
		}

		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);
			var flipViewItem = (FlipViewItem)element;

			flipViewItem.Tapped -= OnFlipViewItemTapped;
			flipViewItem.Tapped += OnFlipViewItemTapped;
		}

		private void OnFlipViewItemTapped(object sender, TappedRoutedEventArgs e)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("FlipViewItem was tapped.");
			}
		}
	}
}
