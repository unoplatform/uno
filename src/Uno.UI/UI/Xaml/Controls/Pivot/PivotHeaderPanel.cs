using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class PivotHeaderPanel : Canvas
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			availableSize.Width -= GetHorizontalOffset();
			availableSize.Height -= GetVerticalOffset();

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

			desiredSize.Width += GetHorizontalOffset();
			desiredSize.Height += GetVerticalOffset();

			return desiredSize;
		}

		protected override Size ArrangeOverride(Size arrangeSize)
		{
			arrangeSize.Width -= GetHorizontalOffset();
			arrangeSize.Height -= GetVerticalOffset();

			var childRectangle = new Windows.Foundation.Rect(BorderThickness.Left + Padding.Left, BorderThickness.Top + Padding.Top, arrangeSize.Width, arrangeSize.Height);


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

			arrangeSize.Width += GetHorizontalOffset();
			arrangeSize.Height += GetVerticalOffset();

			return arrangeSize;
		}
	}
}
