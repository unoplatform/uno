using System.IO;
using SkiaSharp;
using SkiaSharp.Views.UWP;
using Svg.Skia;
using Uno.UI.Xaml.Media.Imaging.Svg;
using Windows.UI.Xaml;

namespace Uno.UI.Svg;

[Preserve]
internal partial class SvgProvider : ISvgProvider
{
	public UIElement GetImage(Stream stream)
	{
		var svg = new SKSvg();

		svg.Load(stream);

		SKXamlCanvas canvas = new SKXamlCanvas();
		canvas.DrawPicture(svg.Picture);
	}
}
