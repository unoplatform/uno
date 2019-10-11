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
		}

		protected override void OnChildrenChanged()
		{
			base.OnChildrenChanged();
		}


		protected override Size MeasureOverride(Size availableSize)
		{
			Size size = default;
			foreach (var child in Children)
			{
				if (!(child is PopupPanel))
				{
					continue;
				}
				// Note that we should always be arranged with the full size of the window, so we don't care too much about the return value here.
				size = MeasureElement(child, availableSize);
			}
			return size;
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
