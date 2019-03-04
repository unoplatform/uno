using System.Numerics;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	public  partial class MatrixTransform
	{
		internal override Matrix3x2 ToNativeTransform(Size size)
		{
			var matrix = Matrix;

			return new Matrix3x2(
				(float) matrix.M11,
				(float) matrix.M12,
				(float) matrix.M21,
				(float) matrix.M22,
				(float) matrix.OffsetX,
				(float) matrix.OffsetY);
		}
	}
}
