using System;
using CoreGraphics;

namespace Windows.UI.Xaml.Media
{
	public partial class MatrixTransform
	{
		internal override CGAffineTransform ToNativeTransform(CGSize size, bool withCenter = true)
		{
			var matrix = Matrix;

			return new CGAffineTransform(
				(nfloat)matrix.M11,
				(nfloat)matrix.M12,
				(nfloat)matrix.M21,
				(nfloat)matrix.M22,
				(nfloat)matrix.OffsetX,
				(nfloat)matrix.OffsetY
			);
		}
	}
}
