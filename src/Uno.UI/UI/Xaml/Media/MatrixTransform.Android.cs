using System;
using Uno;
using Uno.Extensions;
using Uno.Logging;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	[NotImplemented]
	public  partial class MatrixTransform
	{
		// Not implemented yet
		// The transform matrix cannot be applied directly and must
		// first be decomposed to fit in the Android API.
		// See https://math.stackexchange.com/questions/13150/extracting-rotation-scale-values-from-2d-transformation-matrix

		protected override void Update()
		{
			this.Log().Error("MatrixTransform is not implemented");
			base.Update();
		}
	}
}
