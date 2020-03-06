using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Shapes
{
	public partial class Path : ArbitraryShapeBase
	{
		protected override Android.Graphics.Path GetPath(Size availableSize)
		{
			var streamGeometry = Data.ToStreamGeometry();
			return streamGeometry?.ToPath();
		}

		partial void OnDataChanged()
		{
			RequestLayout();
		}
	}
}
