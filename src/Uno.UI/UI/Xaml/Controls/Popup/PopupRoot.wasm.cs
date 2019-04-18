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
			var lastDismissablePopup = Children.Select(GetPopup)
				.LastOrDefault(p => p.IsLightDismissEnabled);

			if(lastDismissablePopup != null)
			{
				lastDismissablePopup.IsOpen = false;
			}
		}

		private static MatrixTransform _identityTransform = new MatrixTransform();

		protected override Size ArrangeOverride(Size finalSize)
		{
			foreach (var child in Children)
			{
				if (!(child is PopupPanel panel))
				{
					continue;
				}

				var desiredSize = child.DesiredSize;
				var popup = panel.Popup;

				var locationTransform1 = popup.TransformToVisual(this) ?? _identityTransform;
				var r1 = new Rect(new Point(popup.HorizontalOffset, popup.VerticalOffset), desiredSize);
				var rect = locationTransform1.TransformBounds(r1);
				child.Arrange(rect);
			}

			return finalSize;
		}

		#region Popup

		public static Popup GetPopup(DependencyObject obj)
		{
			return (Popup)obj.GetValue(PopupProperty);
		}

		public static void SetPopup(DependencyObject obj, Popup value)
		{
			obj.SetValue(PopupProperty, value);
		}

		public static readonly DependencyProperty PopupProperty =
			DependencyProperty.RegisterAttached("Popup", typeof(Popup), typeof(PopupRoot), new PropertyMetadata(null));

		#endregion
	}
}
