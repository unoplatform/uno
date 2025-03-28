#nullable enable

using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ContentPresenter = Windows.UI.Xaml.Controls.ContentPresenter;
using WpfCanvas = System.Windows.Controls.Canvas;

namespace Uno.UI.Runtime.Skia.Wpf;

internal partial class WpfNativeElementHostingExtension : ContentPresenter.INativeElementHostingExtension
{
	public object CreateSampleComponent(string text)
	{
		return new Button
		{
			Width = 100,
			Height = 100,
			Content = new Viewbox
			{
				Child = new StackPanel
				{
					Children =
					{
						new TextBlock
						{
							Text = text
						},
						new Path
						{
							// A star
							Data = Geometry.Parse("M 17.416,32.25L 32.910,32.25L 38,18L 43.089,32.25L 58.583,32.25L 45.679,41.494L 51.458,56L 38,48.083L 26.125,56L 30.597,41.710L 17.416,32.25 Z"),
							Stretch = Stretch.Uniform,
							Stroke = Brushes.Red
						}
					}
				}
			}
		};
	}
}
