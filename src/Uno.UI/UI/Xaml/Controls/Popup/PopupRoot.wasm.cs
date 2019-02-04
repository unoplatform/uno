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
		/// <summary>
		/// This value is an arbitrary value for the placement of
		/// a popup below its anchor.
		/// </summary>
		private const int PopupPlacementTargetMargin = 5;

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
				var popupLocation = popup.TransformToVisual(popup.Anchor ?? this) as MatrixTransform;

				Point getLocation()
				{
					if (popup.Anchor != null)
					{
						var anchorHeight = ((popup.Anchor as FrameworkElement)?.ActualHeight + PopupPlacementTargetMargin) ?? 0;

						return new Point(
							popup.HorizontalOffset - popupLocation.Matrix.OffsetX,
							(popup.VerticalOffset + anchorHeight) - popupLocation.Matrix.OffsetY
						);
					}
					else
					{
						return new Point(
							popupLocation.Matrix.OffsetX + popup.HorizontalOffset,
							popupLocation.Matrix.OffsetY + popup.VerticalOffset
						);
					}
				}

				var location = getLocation();

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
