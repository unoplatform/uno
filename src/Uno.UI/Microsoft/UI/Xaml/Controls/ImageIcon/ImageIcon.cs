#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ImageIcon : IconElement
	{
		private Image? m_rootImage = null;

		public ImageIcon()
		{
		}

		protected override void OnApplyTemplate()
		{
#if HAS_UNO
			// Uno specific: WinUI shares the visual tree initialziation with BitmapIcon
			InitializeVisualTree();
#endif

			if (VisualTreeHelper.GetChild(this, 0) is Grid grid)
			{
				var image = (Image)VisualTreeHelper.GetChild(grid, 0);
				image.Source = Source;
				m_rootImage = image;
			}
			else
			{
				m_rootImage = null;
			}
		}

		private void OnSourcePropertyChanged(DependencyPropertyChangedEventArgs agrs)
		{
			if (m_rootImage is { } image)
			{
				image.Source = Source;
			}
		}
	}
}
