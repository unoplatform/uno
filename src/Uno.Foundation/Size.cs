using System.ComponentModel;
using System.Diagnostics;
using System.Security;

namespace Windows.Foundation
{
	[DebuggerDisplay("{Width}x{Height}")]
	[TypeConverter(typeof(SizeConverter))]
	public partial struct Size
	{
		public Size(double width, double height)
		{
			Width = width;
			Height = height;
		}

		public static Size Empty => new Size(double.NegativeInfinity, double.NegativeInfinity);

		public double Height { get; set; }

		public bool IsEmpty => double.IsNegativeInfinity(Width) && double.IsNegativeInfinity(Height);

		public double Width { get; set; }

		public override bool Equals(object o)
		{
			if (o is Size other)
			{
				return other.Width == Width
					&& other.Height == Height;
			}

			return false;
		}

		public bool Equals(Size value)
		{
			return value.Width == Width
					&& value.Height == Height;
		}

		public override int GetHashCode() => Width.GetHashCode() ^ Height.GetHashCode();

		public override string ToString() => $"[{Width};{Height}]";

		public static bool operator ==(Size size1, Size size2) => size1.Equals(size2);

		public static bool operator !=(Size size1, Size size2) => !size1.Equals(size2);
	}
}
