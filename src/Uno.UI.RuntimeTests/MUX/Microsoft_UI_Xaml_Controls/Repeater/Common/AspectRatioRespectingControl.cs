using System;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace MUXControlsTestApp.Utilities
{
	[Microsoft.UI.Xaml.Data.Bindable]
	public partial class AspectRatioRespectingControl : ContentPresenter
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			return TwoByOneifySize(availableSize);
		}

		/// <inheritdoc/>
		protected override Size ArrangeOverride(Size finalSize)
		{
			return TwoByOneifySize(finalSize);
		}

		private Size TwoByOneifySize(Size initialSize)
		{
			if (double.IsNaN(initialSize.Height) || double.IsInfinity(initialSize.Height))
			{
				return new Size(initialSize.Width / 2, initialSize.Width);
			}
			if (double.IsNaN(initialSize.Width) || double.IsInfinity(initialSize.Width))
			{
				return new Size(initialSize.Height / 2, initialSize.Height);
			}
			return new Size(Math.Min(100, initialSize.Height / 2), Math.Min(100, initialSize.Height));
		}
	}
}
