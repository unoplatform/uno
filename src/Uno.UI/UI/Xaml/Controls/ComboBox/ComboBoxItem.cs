using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class ComboBoxItem : SelectorItem
	{
		private Popup _popup;

		protected override void OnLoaded()
		{
			base.OnLoaded();

			var popup = GetPopupControl();
			if (popup != _popup)
			{
				if(_popup != null)
				{
					_popup.Opened -= OnPopupOpened;
				}
				popup.Opened += OnPopupOpened;
				_popup = popup;
			}

			CheckContentKidnapped();
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			if (_popup != null)
			{
				_popup.Opened -= OnPopupOpened;
				_popup = null;
			}
		}

		private void OnPopupOpened(object sender, object e)
		{
			CheckContentKidnapped();
		}

		private void CheckContentKidnapped()
		{
			if (Content is DependencyObject content)
			{
				var currentParent = GetParent(content);

				if (currentParent != Content)
				{
					// Content has been kidnapped: reconnect content with current element
					Content = null;
					Content = content;
				}
			}
		}

		public Popup GetPopupControl()
		{
			var parent = GetParent(this);

			while (parent != null)
			{
				if (parent is PopupPanel pnl)
				{
					return pnl.Popup;
				}

#if __WASM__
				var popup = PopupRoot.GetPopup(parent);
				if (popup != null)
				{
					return popup;
				}
#endif

				parent = GetParent(parent);
			}

			return null;
		}

		private DependencyObject GetParent(DependencyObject e)
			=> (e as FrameworkElement)?.Parent ?? VisualTreeHelper.GetParent(e);
	}
}
