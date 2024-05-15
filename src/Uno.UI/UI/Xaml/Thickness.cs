using System;
using System.ComponentModel;
using System.Globalization;

namespace Microsoft.UI.Xaml
{
	[TypeConverter(typeof(ThicknessConverter))]
	public partial struct Thickness : IEquatable<Thickness>
	{
		public static readonly Thickness Empty;

		public Thickness(double uniformLength)
			: this()
		{
			Left = Top = Right = Bottom = uniformLength;
		}

		public Thickness(double left, double top, double right, double bottom)
			: this()
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

		public Thickness(double leftRight, double topBottom)
			: this()
		{
			Left = leftRight;
			Top = topBottom;
			Right = leftRight;
			Bottom = topBottom;
		}

		public double Left;
		public double Top;
		public double Right;
		public double Bottom;

		internal Thickness GetInverse() => new Thickness(-Left, -Top, -Right, -Bottom);

		public bool Equals(Thickness other)
		{
			return Math.Abs(Left - other.Left) < double.Epsilon
			&& Math.Abs(Top - other.Top) < double.Epsilon
			&& Math.Abs(Right - other.Right) < double.Epsilon
			&& Math.Abs(Bottom - other.Bottom) < double.Epsilon;
		}

		public override bool Equals(object obj)
		{
			return (obj is Thickness) && Equals((Thickness)obj);
		}

		public override int GetHashCode()
		{
			// Analysis disable NonReadonlyReferencedInGetHashCode
			return Left.GetHashCode()
			^ Top.GetHashCode()
			^ Right.GetHashCode()
			^ Bottom.GetHashCode();
			// Analysis restore NonReadonlyReferencedInGetHashCode
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[Thickness: {0}-{1}-{2}-{3}]", Left, Top, Right, Bottom);
		}

		public static bool operator ==(Thickness t1, Thickness t2)
		{
			return t1.Equals(t2);
		}

		public static bool operator !=(Thickness t1, Thickness t2)
		{
			return !t1.Equals(t2);
		}
	}
}

