using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Shapes
{
	public abstract partial class ArbitraryShapeBase : Shape
	{
#pragma warning disable 649 // unused member
		private float _scaleX;
		private float _scaleY;
#pragma warning restore 649 // unused member

		private IDisposable BuildDrawableLayer() { return null; }

		private Windows.Foundation.Size GetActualSize() { return Size.Empty; }
	}
}
