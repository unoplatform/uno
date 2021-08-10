using System;
using Windows.UI.Xaml.Wasm;

namespace Windows.UI.Xaml.Media
{
	partial class Geometry
	{
		internal virtual SvgElement GetSvgElement() => throw new NotSupportedException($"{nameof(GetSvgElement)} is not implemented for {this}.");

		internal virtual void Invalidate() { }
	}
}
