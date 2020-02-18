using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Uno.Extensions;

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
				return other.Width.Equals(Width)
					&& other.Height.Equals(Height);
			}

			return false;
		}

		public bool Equals(Size value) =>
			value.Width.Equals(Width)
			&& value.Height.Equals(Height);

		public override int GetHashCode() => Width.GetHashCode() ^ Height.GetHashCode();

		public override string ToString()
		{
			var sb = new StringBuilder(8);
			sb.AppendFormatInvariant(null, Width);
			sb.Append(',');
			sb.AppendFormatInvariant(null, Height);
			return sb.ToString();
		}

		public static bool operator ==(Size size1, Size size2) => size1.Equals(size2);

		public static bool operator !=(Size size1, Size size2) => !size1.Equals(size2);
	}
}
