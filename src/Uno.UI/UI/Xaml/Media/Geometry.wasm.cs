using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Wasm;

namespace Microsoft.UI.Xaml.Media
{
	partial class Geometry
	{
		internal virtual SvgElement GetSvgElement() => throw new NotSupportedException($"{nameof(GetSvgElement)} is not implemented for {this}.");

		internal virtual IFormattable ToPathData() => throw new NotSupportedException($"{nameof(ToPathData)} is not implemented for {this}.");

		internal virtual void Invalidate() { }
	}
}
