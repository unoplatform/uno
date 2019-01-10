using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class ComboBoxItem : SelectorItem
	{
		protected override void OnLoaded()
		{
			var popup = GetPopupControl();
			popup.Opened += OnPopupOpened;

			base.OnLoaded();
		}

		private void OnPopupOpened(object sender, object e)
		{
			if (Content is DependencyObject content)
			{
				// Reconnect content with current element
				Content = null;
				Content = content;
			}
		}

		public Popup GetPopupControl()
		{
			DependencyObject GetParent(DependencyObject e) => (e as FrameworkElement)?.Parent ?? VisualTreeHelper.GetParent(e);

			var parent = GetParent(this);

			while (parent != null)
			{
				if (parent is PopupPanel pnl)
				{
					return pnl.Popup;
				}

				parent = GetParent(parent);
			}

			return null;
		}
	}
}
