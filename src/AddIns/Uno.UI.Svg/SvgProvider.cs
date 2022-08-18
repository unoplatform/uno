#nullable enable

using System;
using Uno.UI.Xaml.Media.Imaging.Svg;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace Uno.UI.Svg;

[Preserve]
public partial class SvgProvider : ISvgProvider
{
	private SvgImageSource _owner;

	public SvgProvider(object owner)
	{
		if (owner is not SvgImageSource svgImageSource)
		{
			throw new InvalidOperationException("Owner must be a SvgImageSource instance.");
		}

		_owner = svgImageSource;
	}

	public UIElement GetCanvas()
	{
		return new SvgCanvas(_owner);
	}

	public UIElement? GetImage(SvgImageSource imageSource)
	{
		_owner.RaiseImageOpened();
		return null;
		//imageSource.RasterizePixelHeight
		//var svg = new SKSvg();

		//svg.Load(stream);

		//SKXamlCanvas canvas = new SKXamlCanvas();
		//canvas.(svg.Picture);
	}
}
