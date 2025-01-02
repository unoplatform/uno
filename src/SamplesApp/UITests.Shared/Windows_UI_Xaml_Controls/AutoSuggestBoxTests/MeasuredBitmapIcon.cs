using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml_Controls.AutoSuggestBoxTests
{
	public partial class MeasuredBitmapIcon : BitmapIcon
	{
		public Size LastMeasureAvailableSize { get; private set; }

		public Size LastMeasureResultSize { get; private set; }

		public Size LastArrangeOverrideFinalSize { get; private set; }

		public Size LastArrangeOverrideResultSize { get; private set; }

		protected override Size ArrangeOverride(Size finalSize)
		{
			LastArrangeOverrideFinalSize = finalSize;
			LastArrangeOverrideResultSize = base.ArrangeOverride(finalSize);
			return LastArrangeOverrideResultSize;
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			LastMeasureAvailableSize = availableSize;
			LastMeasureResultSize = base.MeasureOverride(availableSize);
			return LastMeasureResultSize;
		}
	}
}
