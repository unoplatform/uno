using System;
using System.Numerics;
using System.Security;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media
{
	public partial struct Matrix : IFormattable
	{
		internal Matrix3x2 Inner;

		internal Matrix(Matrix3x2 matrix)
		{
			Inner = matrix;
		}

		public Matrix(double m11, double m12, double m21, double m22, double offsetX, double offsetY)
		{
			Inner = new Matrix3x2((float)m11, (float)m12, (float)m21, (float)m22, (float)offsetX, (float)offsetY);
		}

		public static Matrix Identity { get; } = new Matrix(Matrix3x2.Identity);

		public bool IsIdentity => this.Equals(Identity);

		public double M11 { get => Inner.M11; set => Inner.M11 = (float)value; }

		public double M12 { get => Inner.M12; set => Inner.M12 = (float)value; }

		public double M21 { get => Inner.M21; set => Inner.M21 = (float)value; }

		public double M22 { get => Inner.M22; set => Inner.M22 = (float)value; }

		public double OffsetX { get => Inner.M31; set => Inner.M31 = (float)value; }

		public double OffsetY { get => Inner.M32; set => Inner.M32 = (float)value; }

		public bool Equals(Matrix value) => Inner == value.Inner;

		public override bool Equals(object o)
			=> o is Matrix m ? Inner.Equals(m.Inner) : false;

		public override int GetHashCode()
			=> Inner.GetHashCode();

		public override string ToString()
			=> Inner.ToString();

		public string ToString(IFormatProvider provider)
			=> Inner.ToString();

		public string ToString(string format, IFormatProvider provider)
			=> Inner.ToString();

		public Point Transform(Point point)
		{
			var o = Vector2.Transform(new Vector2((float)point.X, (float)point.Y), Inner);
			return new Point(o.X, o.Y);
		}

		public static bool operator ==(Matrix matrix1, Matrix matrix2)
			=> matrix1.Inner.Equals(matrix2.Inner);

		public static bool operator !=(Matrix matrix1, Matrix matrix2)
			=> !matrix1.Inner.Equals(matrix2.Inner);
	}
}
