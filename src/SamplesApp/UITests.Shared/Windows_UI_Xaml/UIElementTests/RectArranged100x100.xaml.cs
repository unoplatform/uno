using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml.UIElementTests
{
	public sealed partial class RectArranged100x100 : UserControl
	{
		public RectArranged100x100()
		{
			this.InitializeComponent();
		}

		protected override Size MeasureOverride(Size availableSize) => new Size(75, 75);

		protected override Size ArrangeOverride(Size finalSize)
		{
			var controlSize = new Size(100, 100);
			base.ArrangeOverride(controlSize);

			return controlSize;
		}
	}
}
