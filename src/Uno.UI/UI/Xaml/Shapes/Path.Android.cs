#nullable enable
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Shapes
{
	public partial class Path : Shape
	{
		protected override Size MeasureOverride(Size availableSize)
			=> MeasureAbsoluteShape(availableSize, GetPath());

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
			=> ArrangeAbsoluteShape(finalSize, GetPath());

		private APath? GetPath()
		{
			return Data?.ToStreamGeometry()?.ToPath();
		}
	}
}
