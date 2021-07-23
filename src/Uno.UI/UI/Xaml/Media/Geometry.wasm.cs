using System;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	partial class Geometry
	{
		internal virtual SvgElement GetSvgElement() => throw new NotSupportedException($"{this} is not well implemented.");

		internal virtual void Invalidate() { }
	}
}
