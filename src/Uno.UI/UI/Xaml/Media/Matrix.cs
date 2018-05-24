using System;
using System.Numerics;
using System.Security;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	public struct Matrix : IFormattable
	{
		private Matrix3x2 _inner;

		public Matrix(double m11, double m12, double m21, double m22, double offsetX, double offsetY)
		{
			_inner = new Matrix3x2((float)m11, (float)m12, (float)m21, (float)m22, (float)offsetX, (float)offsetY);
		}

		public static Matrix Identity { get; } = 
			new Matrix(
				1, 0, 
				0, 1, 
				0, 0
			);

		public bool IsIdentity => this.Equals(Identity);

		public double M11 { get => _inner.M11; set => _inner.M11 = (float)value; }

		public double M12 { get => _inner.M12; set => _inner.M12 = (float)value; }

		public double M21 { get => _inner.M21; set => _inner.M21 = (float)value; }

		public double M22 { get => _inner.M22; set => _inner.M22 = (float)value; }

		public double OffsetX { get => _inner.M31; set => _inner.M31 = (float)value; }

		public double OffsetY { get => _inner.M32; set => _inner.M32 = (float)value; }

		public bool Equals(Matrix value) => _inner == value._inner;

		public override bool Equals(object o) 
			=> o is Matrix m ? _inner.Equals(m._inner) : false;

		public override int GetHashCode() 
			=> _inner.GetHashCode();

		public override string ToString() 
			=> _inner.ToString();

		public string ToString(IFormatProvider provider) 
			=> _inner.ToString();

		public string ToString(string format, IFormatProvider provider) 
			=> _inner.ToString();

		public Point Transform(Point point)
		{
			var o = Vector2.Transform(new Vector2((float)point.X, (float)point.Y), _inner);
			return new Point(o.X, o.Y);
		}

		public static bool operator ==(Matrix matrix1, Matrix matrix2)
			=> matrix1._inner.Equals(matrix2._inner);

		public static bool operator !=(Matrix matrix1, Matrix matrix2) 
			=> !matrix1._inner.Equals(matrix2._inner);
	}
}