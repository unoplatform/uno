using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter
	{
		internal void OnMinZoomFactorChanged(float newValue)
		{
			throw new NotImplementedException();
		}

		internal void OnMaxZoomFactorChanged(float newValue)
		{
			throw new NotImplementedException();
		}

		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
		protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
	}
}
