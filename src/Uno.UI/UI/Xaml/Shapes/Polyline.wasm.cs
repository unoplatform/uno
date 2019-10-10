using System;
using Windows.Foundation;
using Windows.UI.Xaml.Wasm;
using Uno;
using Uno.Extensions;

namespace Windows.UI.Xaml.Shapes
{
	partial class Polyline
	{
		private readonly SvgElement _polyline = new SvgElement("polyline");

		protected override SvgElement GetMainSvgElement()
		{
			return _polyline;
		}
	}
}
