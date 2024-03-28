using Android.Views;
using Uno.Extensions;
using Windows.Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Exists as a top-level container for SelectorItems, allowing their Alignments and Margins to be taken into account
	/// </summary>
	internal partial class ItemContainerHolder : Border
	{
		public ItemContainerHolder()
		{
			// Avoids issue when ItemContainerHolder's parent is a native
			// view (i.e. Spinner/PopupWindow) causing height to be measured as 0.
			StretchAffectsMeasure = false;
		}

		/// <summary>
		/// Defines the orientation in which items should stretch (if any)
		/// </summary>
		public Orientation? StretchOrientation { get; set; }


		protected override Size MeasureOverride(Size size)
		{
			var measuredSize = base.MeasureOverride(size);

			if (StretchOrientation == null)
			{
				return measuredSize;
			}

			switch (StretchOrientation.Value)
			{
				case Orientation.Vertical:
					return new Size(measuredSize.Width, size.Height);
				case Orientation.Horizontal:
					return new Size(size.Width, measuredSize.Height);
				default:
					return measuredSize;
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			if (Child != null)
			{
				base.ArrangeElement(
					Child,
					new Rect(Point.Zero, finalSize)
				);
			}

			return finalSize;
		}
	}
}
