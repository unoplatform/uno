#if __WASM__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			PointerReleased += PopupRoot_PointerReleased;
		}

		protected override void OnChildrenChanged()
		{
			base.OnChildrenChanged();
			UpdateIsHitTestVisible();
		}

		// This is a workaround because PopupRoot otherwise blocks touches.
		private void UpdateIsHitTestVisible()
		{
			IsHitTestVisible = Children.Any();
		}

		private void PopupRoot_PointerReleased(object sender, Input.PointerRoutedEventArgs e)
		{
			Children.Select(GetPopup)
				.Where(p => p.IsLightDismissEnabled)
				.ToList()
				.ForEach(p => p.IsOpen = false);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			foreach (var child in Children)
			{
				var desiredSize = child.DesiredSize;
				var popup = GetPopup(child);
				var popupLocation = popup.TransformToVisual(this) as TranslateTransform;
				var location = new Point(
					popupLocation.X + popup.HorizontalOffset,
					popupLocation.Y + popup.VerticalOffset
				);

				child.Arrange(new Rect(location, desiredSize));
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
#endif
