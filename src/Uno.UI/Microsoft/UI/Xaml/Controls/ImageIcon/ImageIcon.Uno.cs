using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ImageIcon
	{
		private bool _initialized = false;

		private void InitializeVisualTree()
		{
			if (!_initialized)
			{
				var image = new Image
				{
					Stretch = Stretch.Uniform
				};

				var grid = new Grid();
				grid.Children.Add(image);

				AddIconElementView(grid);
			}
		}
	}
}
