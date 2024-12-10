using System;
using System.Numerics;
using System.Security;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	public partial struct Matrix
	{
		internal Matrix(Matrix3x2 matrix)
		{
			M11 = matrix.M11;
			M12 = matrix.M12;
			M21 = matrix.M21;
			M22 = matrix.M22;
			OffsetX = matrix.M31;
			OffsetY = matrix.M32;
		}

		internal Matrix3x2 ToMatrix3x2()
			=> new Matrix3x2((float)M11, (float)M12, (float)M21, (float)M22, (float)OffsetX, (float)OffsetY);

		public Matrix(double m11, double m12, double m21, double m22, double offsetX, double offsetY)
		{
			M11 = m11;
			M12 = m12;
			M21 = m21;
			M22 = m22;
			OffsetX = offsetX;
			OffsetY = offsetY;
		}

		public static Matrix Identity { get; } = new Matrix(Matrix3x2.Identity);

		public bool IsIdentity => this.Equals(Identity);

		public double M11;
		public double M12;
		public double M21;
		public double M22;
		public double OffsetX;
		public double OffsetY;

		public bool Equals(Matrix value) => this == value;

		public override bool Equals(object o)
			=> o is Matrix m && Equals(m);

		public override int GetHashCode()
			=> ToMatrix3x2().GetHashCode();

		public override string ToString()
			=> $"{M11},{M12},{M21},{M22},{OffsetX},{OffsetY}";

		public Point Transform(Point point)
		{
			// https://github.com/dotnet/runtime/blob/a50e1e6f6ff33513cef6ac9757a9061fbd0ba26e/src/libraries/System.Private.CoreLib/src/System/Numerics/Vector2.cs#L431-L437
			return new Point(
				(point.X * M11) + (point.Y * M21) + OffsetX,
				(point.X * M12) + (point.Y * M22) + OffsetY
			);
		}

		public static bool operator ==(Matrix matrix1, Matrix matrix2)
			=> matrix1.M11 == matrix2.M11
				&& matrix1.M22 == matrix2.M22
				&& matrix1.M12 == matrix2.M12
				&& matrix1.M21 == matrix2.M21
				&& matrix1.OffsetX == matrix2.OffsetX
				&& matrix1.OffsetY == matrix2.OffsetY;


		public static bool operator !=(Matrix matrix1, Matrix matrix2)
			=> !(matrix1 == matrix2);
	}
}
