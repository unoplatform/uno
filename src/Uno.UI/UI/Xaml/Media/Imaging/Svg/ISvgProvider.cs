using System;
using Windows.Foundation;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Media.Imaging.Svg;

internal interface ISvgProvider
{
	UIElement GetCanvas();

	bool IsParsed { get; }

	Size SourceSize { get; }

	event EventHandler? SourceLoaded;
}
