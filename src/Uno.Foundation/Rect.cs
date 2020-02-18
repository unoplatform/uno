using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Uno.Extensions;

namespace Windows.Foundation
{
	[DebuggerDisplay("[Rect {Size}@{Location}]")]
	public partial struct Rect
	{
		private const string _negativeErrorMessage = "Non-negative number required.";

		public static Rect Empty { get; } = new Rect
		{
			X = double.PositiveInfinity,
			Y = double.PositiveInfinity,
			Width = double.NegativeInfinity,
			Height = double.NegativeInfinity
		};

		public Rect(Point point, Size size) : this(point.X, point.Y, size.Width, size.Height) { }

		public Rect(double x, double y, double width, double height)
		{
			if (!Uno.FoundationFeatureConfiguration.Rect.AllowNegativeWidthHeight)
			{
				if (width < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(width), _negativeErrorMessage);
				}

				if (height < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(width), _negativeErrorMessage);
				}
			}

			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public Rect(Point point1, Point point2)
		{
			if (point1.X < point2.X) // This will return false is any is NaN, and as it's the common case, we keep it first
			{
				X = point1.X;
				Width = point2.X - point1.X;
			}
			else if (double.IsNaN(point1.X) || double.IsNaN(point2.X))
			{
				X = double.NaN;
				Width = double.NaN;
			}
			else
			{
				X = point2.X;
				Width = point1.X - point2.X;
			}

			if (point1.Y < point2.Y) // This will return false is any is NaN, and as it's the common case, we keep it first
			{
				Y = point1.Y;
				Height = point2.Y - point1.Y;
			}
			else if (double.IsNaN(point1.Y) || double.IsNaN(point2.Y))
			{
				Y = double.NaN;
				Height = double.NaN;
			}
			else
			{
				Y = point2.Y;
				Height = point1.Y - point2.Y;
			}
		}

		public double X { get; set; }
		public double Y { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }

		public double Left => X;
		public double Top => Y;
		public double Right => X + Width;
		public double Bottom => Y + Height;

		public bool IsEmpty => Empty.Equals(this);

		public static implicit operator Rect(string text)
		{
			var parts = text
				.Split(new[] { ',' })
				.SelectToArray(s => double.Parse(s, NumberFormatInfo.InvariantInfo));

			return new Rect
			(
				parts[0],
				parts[1],
				parts[2],
				parts[3]
			);
		}

		public static implicit operator string(Rect rect)
		{
			var sb = new StringBuilder();
			sb.AppendFormatInvariant(null, rect.X);
			sb.Append(',');
			sb.AppendFormatInvariant(null, rect.Y);
			sb.Append(',');
			sb.AppendFormatInvariant(null, rect.Width);
			sb.Append(',');
			sb.AppendFormatInvariant(null, rect.Height);
			return sb.ToString();
		}

		public override string ToString() => (string)this;

		/// <summary>
		/// Provides the size of this rectangle.
		/// </summary>
		/// <remarks>This property is not provided by UWP, hence it is marked internal.</remarks>
		internal Size Size
		{
			get => new Size(Width, Height);
			set
			{
				Width = value.Width;
				Height = value.Height;
			}
		}

		/// <summary>
		/// Provides the location of this rectangle.
		/// </summary>
		/// <remarks>This property is not provided by UWP, hence it is marked internal.</remarks>
		internal Point Location
		{
			get => new Point(X, Y);
			set
			{
				X = value.X;
				Y = value.Y;
			}
		}

		/// <summary>Expands or shrinks the rectangle by using the specified width and height amounts, in all directions. </summary>
		/// <param name="width">The amount by which to expand or shrink the left and right sides of the rectangle.</param>
		/// <param name="height">The amount by which to expand or shrink the top and bottom sides of the rectangle.</param>
		/// <exception cref="T:System.InvalidOperationException">This method is called on the <see cref="P:System.Windows.Rect.Empty" /> rectangle.</exception>
		internal void Inflate(double width, double height)
		{
			if (this.IsEmpty)
			{
				throw new InvalidOperationException("Can't inflate empty rectangle");
			}

			this.X -= width;
			this.Y -= height;
			this.Width += width;
			this.Width += width;
			this.Height += height;
			this.Height += height;

			if (this.Width < 0.0 || this.Height < 0.0)
			{
				this = Rect.Empty;
			}
		}

		public bool Contains(Point point) =>
			point.X >= X
			&& point.X <= X + Width
			&& point.Y >= Y
			&& point.Y <= Y + Height;

		/// <summary>
		/// Finds the intersection of the rectangle represented by the current Windows.Foundation.Rect
		/// and the rectangle represented by the specified Windows.Foundation.Rect, and stores
		/// the result as the current Windows.Foundation.Rect.
		/// </summary>
		/// <remarks>
		/// Use .IntersectWith() extensions if you want a version without side-effects.
		/// </remarks>
		/// <param name="rect">The rectangle to intersect with the current rectangle.</param>
		public void Intersect(Rect rect)
		{
			var left = Math.Max(Left, rect.Left);
			var right = Math.Min(Right, rect.Right);
			var top = Math.Max(Top, rect.Top);
			var bottom = Math.Min(Bottom, rect.Bottom);
			if (right >= left && bottom >= top)
			{
				this = new Rect(left, top, right - left, bottom - top);
			}
			else
			{
				this = Empty;
			}
		}

		/// <summary>
		/// Finds the union of the rectangle represented by the current Windows.Foundation.Rect
		/// and the rectangle represented by the specified Windows.Foundation.Rect, and stores
		/// the result as the current Windows.Foundation.Rect.
		/// </summary>
		/// <param name="rect">The rectangle to union with the current rectangle.</param>
		public void Union(Rect rect)
		{
			var left = Math.Min(Left, rect.Left);
			var right = Math.Max(left + Width, rect.Right);
			var top = Math.Min(Top, rect.Top);
			var bottom = Math.Max(top + Height, rect.Bottom);
			this = new Rect(left, top, right - left, bottom - top);
		}

		public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();

		public bool Equals(Rect value)
			=> value.X == X
				&& value.Y == Y
				&& value.Width == Width
				&& value.Height == Height;

		public override bool Equals(object obj)
			=> obj is Rect r ? r.Equals(this) : base.Equals(obj);

		public static bool operator ==(Rect left, Rect right) => left.Equals(right);

		public static bool operator !=(Rect left, Rect right) => !left.Equals(right);
	}
}
