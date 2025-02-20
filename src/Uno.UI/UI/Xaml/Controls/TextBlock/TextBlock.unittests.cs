#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using Windows.Foundation;
using Windows.UI.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class TextBlock : FrameworkElement
	{
		private void InitializePartial() { }

		private int GetCharacterIndexAtPoint(Point point) => -1;
	}
}
