using System;
using Windows.Foundation;
using Uno.Disposables;

namespace Windows.UI.Xaml.Shapes
{
	partial class ArbitraryShapeBase
	{
#pragma warning disable CS0067, CS0649
		private double _scaleX;
		private double _scaleY;
#pragma warning restore CS0067, CS0649

		private IDisposable BuildDrawableLayer()
		{
			return Disposable.Empty;
		}

		private Size GetActualSize() => Size.Empty;
	}
}
