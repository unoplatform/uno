#nullable enable
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using Uno.Extensions;

namespace Windows.Foundation;

[DebuggerDisplay("[Rect {Size}@{Location}]")]
[Uno.Foundation.Internals.Bindable]
public partial struct Rect
{
	// These are public in WinUI (with the underscore!), but we don't want to expose it for now at least.
	private float _x;
	private float _y;
	private float _width;
	private float _height;

	private const string _negativeErrorMessage = "Non-negative number required.";
	private const float Epsilon = 0.00001f;

	private static readonly char[] _commaSpaceArray = new[] { ',', ' ' };

	public static Rect Empty { get; } = new Rect
	{
		X = double.PositiveInfinity,
		Y = double.PositiveInfinity,
		Width = double.NegativeInfinity,
		Height = double.NegativeInfinity
	};

	internal static Rect Infinite { get; } = new Rect
	{
		X = double.NegativeInfinity,
		Y = double.NegativeInfinity,
		Width = double.PositiveInfinity,
		Height = double.PositiveInfinity
	};

	public Rect(Point point, Size size) : this(point.X, point.Y, size.Width, size.Height) { }

	public Rect(double x, double y, double width, double height)
		: this((float)x, (float)y, (float)width, (float)height)
	{
	}

	public Rect(float x, float y, float width, float height)
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

		_x = x;
		_y = y;
		_width = width;
		_height = height;
	}

#if HAS_UNO_WINUI
	public Rect(float x, float y, float width, float height)
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
#endif

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

	public double X
	{
		get => _x;
		set => _x = (float)value;
	}

	public double Y
	{
		get => _y;
		set => _y = (float)value;
	}

	public double Width
	{
		get => _width;
		// TODO: Disallow negative, as WinUI does.
		set => _width = (float)value;
	}

	public double Height
	{
		get => _height;
		// TODO: Disallow negative, as WinUI does.
		set => _height = (float)value;
	}

	public double Left => _x;
	public double Top => _y;

	// TODO: Do we need any special handling for "this.IsEmpty" case?
	public double Right => _x + _width;
	public double Bottom => _y + _height;

	public bool IsEmpty => Empty.Equals(this);

	/// <summary>
	/// This indicates that this rect is equals to the <see cref="Infinite"/>.
	/// Unlike the <see cref="IsFinite"/>, this **DOES NOT** indicates that the rect is infinite on at least one of its axis.
	/// </summary>
	internal bool IsInfinite => Infinite.Equals(this);

	/// <summary>
	/// This make sure that this rect does not have any infinite value on any of its axis.
	/// </summary>
	/// <remarks>This is **NOT** the opposite of <see cref="IsInfinite"/>.</remarks>
	internal bool IsFinite => !double.IsInfinity(X) && !double.IsInfinity(Y) && !double.IsInfinity(Width) && !double.IsInfinity(Height);

	/// <summary>
	/// Indicates that this rect does not have any infinite or NaN on any on its axis.
	/// (I.e. it's a valid rect for standard layouting logic)
	/// </summary>
	internal bool IsValid => IsFinite && !double.IsNaN(X) && !double.IsNaN(Y) && !double.IsNaN(Width) && !double.IsNaN(Height);

	internal bool IsUniform => Math.Abs(Left - Top) < Epsilon && Math.Abs(Left - Right) < Epsilon && Math.Abs(Left - Bottom) < Epsilon;

	public static implicit operator Rect(string text)
	{
		if (text == null)
		{
			return default;
		}

		var parts = text
			.Split(_commaSpaceArray, StringSplitOptions.RemoveEmptyEntries)
			.SelectToArray(s => double.Parse(s, NumberFormatInfo.InvariantInfo));

		if (parts.Length != 4)
		{
			throw new ArgumentException(
				"Cannot create a Rect from " + text + ": needs 4 parts separated by a comma or a space.");
		}

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
		if (rect.IsEmpty)
		{
			return "Empty.";
		}

		var sb = new StringBuilder();
		sb.Append(rect.X.ToStringInvariant());
		sb.Append(',');
		sb.Append(rect.Y.ToStringInvariant());
		sb.Append(',');
		sb.Append(rect.Width.ToStringInvariant());
		sb.Append(',');
		sb.Append(rect.Height.ToStringInvariant());
		return sb.ToString();
	}

	public override string ToString() => (string)this;

	internal string ToDebugString()
		=> IsEmpty ? "--empty--"
			: IsInfinite ? "--infinite--"
			: FormattableString.Invariant($"{Width:F2}x{Height:F2}@{Location.ToDebugString()}");

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

	/// <summary>
	/// Provides the location of this rectangle.
	/// </summary>
	/// <remarks>This method is not provided by UWP, hence it is marked internal.</remarks>
	/// <remarks>Unlike the Location property, this is accessible to UWP code through an extension method.</remarks>
	internal Point GetLocation() => Location;

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

		X -= width;
		Y -= height;
		Width += width;
		Width += width;
		Height += height;
		Height += height;

		if (Width < 0.0 || Height < 0.0)
		{
			this = Rect.Empty;
		}
	}

	public bool Contains(Point point)
		// We include points on the edge as "contained".
		// We do "point.X - Width <= X" instead of "point.X <= X + Width"
		// so that this check works when Width is PositiveInfinity
		// and X is NegativeInfinity.
		=> point.X >= X
			&& point.X - Width <= X
			&& point.Y >= Y
			&& point.Y - Height <= Y;

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
		var right = Math.Max(Left + Width, rect.Right);
		var top = Math.Min(Top, rect.Top);
		var bottom = Math.Max(Top + Height, rect.Bottom);
		this = new Rect(left, top, right - left, bottom - top);
	}

	public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();

	public bool Equals(Rect value)
		=> value.X == X
			&& value.Y == Y
			&& value.Width == Width
			&& value.Height == Height;

	public override bool Equals(object? obj)
		=> obj is Rect r ? r.Equals(this) : base.Equals(obj);

	public static bool operator ==(Rect left, Rect right) => left.Equals(right);

	public static bool operator !=(Rect left, Rect right) => !left.Equals(right);
}
