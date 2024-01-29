using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class Image : FrameworkElement
{
	protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
	protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
}
