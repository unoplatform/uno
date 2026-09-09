using System;
using System.ComponentModel;
using System.Globalization;

#if IS_UNO_COMPOSITION
namespace Uno.UI.Composition;
#else
namespace Microsoft.UI.Xaml;
#endif

#if !IS_UNO_COMPOSITION
[TypeConverter(typeof(ThicknessConverter))]
#endif
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

#if __SKIA__ && !IS_UNO_COMPOSITION
	internal Uno.UI.Composition.Thickness ToUnoCompositionThickness()
	{
		return new Uno.UI.Composition.Thickness(Left, Top, Right, Bottom);
	}
#endif

	public double Left { get; set; }
	public double Top { get; set; }
	public double Right { get; set; }
	public double Bottom { get; set; }

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

	// Compile-linked into Uno.UI.Composition, which cannot see Uno.UI.Helpers.Boxes - and this is a
	// diagnostic-only path where the boxing does not matter.
#pragma warning disable UnoInternal0002
	public override string ToString()
	{
		return string.Format(CultureInfo.InvariantCulture, "[Thickness: {0}-{1}-{2}-{3}]", Left, Top, Right, Bottom);
	}
#pragma warning restore UnoInternal0002

	public static bool operator ==(Thickness t1, Thickness t2)
	{
		return t1.Equals(t2);
	}

	public static bool operator !=(Thickness t1, Thickness t2)
	{
		return !t1.Equals(t2);
	}
}
