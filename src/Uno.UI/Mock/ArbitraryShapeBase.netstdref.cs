using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Uno.Disposables;
using System.Text;

namespace Windows.UI.Xaml.Shapes
{
	public abstract partial class ArbitraryShapeBase : Shape
	{
		private IDisposable BuildDrawableLayer() => null;

		private Windows.Foundation.Size GetActualSize() => default;
	}
}
