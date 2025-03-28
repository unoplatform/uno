using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Uno.UI;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class PivotHeaderPanel : Canvas
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			var desiredSize = default(Size);
			var slotSize = availableSize;

			slotSize.Height = float.PositiveInfinity;

			// Shadow variables for evaluation performance
			var count = Children.Count;

			for (int i = 0; i < count; i++)
			{
				var view = Children[i];

				var measuredSize = MeasureElement(view, slotSize);

				desiredSize.Width += measuredSize.Width;
				desiredSize.Height = Math.Max(desiredSize.Height, measuredSize.Height);
			}

			return desiredSize;
		}

		protected override Size ArrangeOverride(Size arrangeSize)
		{
			var childRectangle = new Rect(0d, 0d, arrangeSize.Width, arrangeSize.Height);

			var previousChildSize = 0.0;

			// Shadow variables for evaluation performance
			var count = Children.Count;

			for (int i = 0; i < count; i++)
			{
				var view = Children[i];
				var desiredChildSize = GetElementDesiredSize(view);

				childRectangle.X += previousChildSize;

				previousChildSize = desiredChildSize.Width;
				childRectangle.Width = desiredChildSize.Width;
				childRectangle.Height = Math.Max(arrangeSize.Height, desiredChildSize.Height);

				ArrangeElement(view, childRectangle);
			}

			return arrangeSize;
		}
	}
}
