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
				var desiredSize = child.DesiredSize;
				var popup = GetPopup(child);
				var locationTransform = popup.TransformToVisual(popup.Anchor ?? this) ?? _identityTransform;

				Point getLocation()
				{
					Point l;

					if (popup.Anchor != null)
					{
						var anchorHeight = ((popup.Anchor as FrameworkElement)?.ActualHeight + PopupPlacementTargetMargin) ?? 0;

						l = new Point(popup.HorizontalOffset, popup.VerticalOffset + anchorHeight);
					}
					else
					{
						l = new Point(popup.HorizontalOffset, popup.VerticalOffset);
					}

					return locationTransform.TryTransform(l, out var transformedLocation) ? transformedLocation : l;
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
