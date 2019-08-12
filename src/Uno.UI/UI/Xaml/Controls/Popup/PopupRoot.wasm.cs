using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal partial class PopupRoot : Panel
	{
		public PopupRoot()
		{
			Background = new SolidColorBrush(Colors.Transparent);
			UpdateIsHitTestVisible();
		}

		protected override void OnChildrenChanged()
		{
			base.OnChildrenChanged();
			UpdateIsHitTestVisible();
		}

		private bool _pointerHandlerRegistered = false;

		private void UpdateIsHitTestVisible()
		{
			var anyChildren = Children.Any();
			IsHitTestVisible = anyChildren;
			if (anyChildren)
			{
				if (!_pointerHandlerRegistered)
				{
					PointerReleased += PopupRoot_PointerReleased;
					_pointerHandlerRegistered = true;
				}
			}
			else
			{
				if (_pointerHandlerRegistered)
				{
					PointerReleased -= PopupRoot_PointerReleased;
					_pointerHandlerRegistered = false;
				}
			}
		}

		private void PopupRoot_PointerReleased(object sender, Input.PointerRoutedEventArgs e)
		{
			var x = Children.ToArray().FirstOrDefault();

			var lastDismissablePopupPanel = Children
				.OfType<PopupPanel>()
				.LastOrDefault(p => p.Popup.IsLightDismissEnabled);

			if(lastDismissablePopupPanel != null)
			{
				lastDismissablePopupPanel.Popup.IsOpen = false;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			foreach (var child in Children)
			{
				if (!(child is PopupPanel panel))
				{
					continue;
				}

				// Note: The popup alignment is ensure by the PopupPanel itself
				child.Arrange(new Rect(new Point(), finalSize));
			}

			return finalSize;
		}
	}
}
