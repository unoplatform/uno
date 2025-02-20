#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Windows.Foundation;
using Windows.UI.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBlock : FrameworkElement
	{
		private int GetCharacterIndexAtPoint(Point point) => -1;

		protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(availableSize);
		protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize);
	}
}
