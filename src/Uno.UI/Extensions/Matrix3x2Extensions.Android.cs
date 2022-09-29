#nullable disable

using System.Numerics;

namespace Uno.UI.Extensions
{
	internal static class Matrix3x2Extensions
	{
		public static Android.Graphics.Matrix ToNative(this Matrix3x2 matrix)
		{
			var nativeMatrix = new Android.Graphics.Matrix();
			nativeMatrix.SetValues(new[]
			{
				matrix.M11, matrix.M21, matrix.M31,
				matrix.M12, matrix.M22, matrix.M32,
				0, 0, 1
			});
			return nativeMatrix;
		}
	}
}
