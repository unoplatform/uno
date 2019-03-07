#if XAMARIN
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	internal partial class FlyoutPopupPanel : PopupPanel
	{
		private readonly Flyout _flyout;

		public FlyoutPopupPanel(Flyout flyout) : base(flyout._popup)
		{
			_flyout = flyout;
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (Popup.Child is FrameworkElement child && _flyout.Target is FrameworkElement target)
			{
				var windowRect = Windows.UI.Xaml.Window.Current.Bounds;

				var targetTransform = (MatrixTransform)target.TransformToVisual(this);
				var targetRect = new Rect(targetTransform.Matrix.OffsetX, targetTransform.Matrix.OffsetY, target.ActualWidth, target.ActualHeight);

				child.Measure(windowRect.Size);
				var childRect = new Rect(new Point(), child.DesiredSize);

				switch (_flyout.Placement)
				{
					case FlyoutPlacementMode.Left:
						childRect.Location = targetRect.Location;
						childRect.X -= childRect.Width;
						childRect.Y += (targetRect.Height - childRect.Height) / 2.0;
						break;
					case FlyoutPlacementMode.Right:
						childRect.Location = targetRect.Location;
						childRect.X += targetRect.Width;
						childRect.Y += (targetRect.Height - childRect.Height) / 2.0;
						break;
					case FlyoutPlacementMode.Top:
						childRect.Location = targetRect.Location;
						childRect.X += (targetRect.Width - childRect.Width) / 2.0;
						childRect.Y -= childRect.Height;
						break;
					case FlyoutPlacementMode.Bottom:
						childRect.Location = targetRect.Location;
						childRect.X += (targetRect.Width - childRect.Width) / 2.0;

						// Adjust the horizontal position of the child element to be within the window.
						if (childRect.X < windowRect.Left)
						{
							childRect.X = windowRect.Left;
						}
						else if (childRect.X + childRect.Width > windowRect.Right)
						{
							childRect.X = windowRect.Right - childRect.Width;
						}

						childRect.Y += targetRect.Height;
						break;
					case FlyoutPlacementMode.Full:
					default:
						childRect = new Rect(new Point(), finalSize);
						break;
				}

				ArrangeElement(child, childRect);
			}

			return finalSize;
		}
	}
}
#endif
