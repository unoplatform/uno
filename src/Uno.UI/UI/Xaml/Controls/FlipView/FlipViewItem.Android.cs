using System;
using System.Linq;
using Android.Views;
using Uno.Extensions;
using Uno.UI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class FlipViewItem : SelectorItem
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			//If a dimension is set, we stretch to that dimension. Otherwise, measure the child.
			var baseMeasure = base.MeasureOverride(availableSize);
			return new Size(
				!double.IsPositiveInfinity(availableSize.Width) ? availableSize.Width : baseMeasure.Width,
				!double.IsPositiveInfinity(availableSize.Height) ? availableSize.Height : baseMeasure.Height
			);
		}
	}
}

