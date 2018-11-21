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
		private IDisposable BuildDrawableLayer() { throw new NotImplementedException(); }

		private Windows.Foundation.Size GetActualSize() { throw new NotImplementedException(); }
	}
}
