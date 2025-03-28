using Uno.Extensions;
using System;
using System.ComponentModel;
using System.Globalization;
using Windows.Foundation;
using Uno.Helpers;

#if IS_UNO_COMPOSITION
namespace Uno.UI.Composition;
#else
namespace Windows.UI.Xaml;
#endif

/// <summary>Defines the radius of a rectangle's corners. </summary>
#if !IS_UNO_COMPOSITION
[TypeConverter(typeof(CornerRadiusConverter))]
#endif
public partial struct CornerRadius : IEquatable<CornerRadius>
{
	/// <summary>Gets or sets the radius of the top-left corner.</summary>
	/// <returns>The radius of the top-left corner. The default is 0.</returns>
	public double TopLeft;

	/// <summary>Gets or sets the radius of the top-right corner. </summary>
	/// <returns>The radius of the top-right corner. The default is 0.</returns>
	public double TopRight;

	/// <summary>Gets or sets the radius of the bottom-right corner. </summary>
	/// <returns>The radius of the bottom-right corner. The default is 0.</returns>
	public double BottomRight;

	/// <summary>Gets or sets the radius of the bottom-left corner. </summary>
	/// <returns>The radius of the bottom-left corner. The default is 0.</returns>
	public double BottomLeft;

	public CornerRadius(double uniformRadius) : this()
	{
		TopLeft = uniformRadius;
		TopRight = uniformRadius;
		BottomLeft = uniformRadius;
		BottomRight = uniformRadius;
	}

	public CornerRadius(double topLeft, double topRight, double bottomRight, double bottomLeft) : this()
	{
		TopLeft = topLeft;
		TopRight = topRight;
		BottomLeft = bottomLeft;
		BottomRight = bottomRight;
	}

#if __SKIA__ && !IS_UNO_COMPOSITION
	internal Uno.UI.Composition.CornerRadius ToUnoCompositionCornerRadius()
	{
		return new Uno.UI.Composition.CornerRadius(TopLeft, TopRight, BottomRight, BottomLeft);
	}
#endif

	private static bool Equals(CornerRadius left, CornerRadius right)
		=> left.TopLeft == right.TopLeft
			&& left.TopRight == right.TopRight
			&& left.BottomLeft == right.BottomLeft
			&& left.BottomRight == right.BottomRight;

	/// <inheritdoc />
	public bool Equals(CornerRadius other)
		=> Equals(this, other);

	/// <inheritdoc />
	public override bool Equals(object obj)
		=> obj is CornerRadius other && Equals(this, other);

	/// <inheritdoc />
	public override int GetHashCode()
		=> TopLeft.GetHashCode()
			^ TopRight.GetHashCode()
			^ BottomLeft.GetHashCode()
			^ BottomRight.GetHashCode();

	/// <inheritdoc />
	public override string ToString()
		=> "TopLeft: {0}, TopRight: {1}, BottomRight: {2}, BottomLeft: {3}".InvariantCultureFormat(TopLeft, TopRight, BottomRight, BottomLeft);

	internal string ToStringCompact()
		=> string.Format(CultureInfo.InvariantCulture, "[CornerRadius: {0}-{1}-{2}-{3}]", TopLeft, TopRight, BottomRight, BottomLeft);

	/// <summary>
	/// Provides a Zero-valued corner radius.
	/// </summary>
	public static readonly CornerRadius None = new CornerRadius(0);

	/// <summary>
	/// Builds a uniform radius from a double;
	/// </summary>
	public static implicit operator CornerRadius(double uniformRadius) => new CornerRadius(uniformRadius);

	/// <summary>
	/// Determines if two CornerRadius instances are equal.
	/// </summary>
	public static bool operator ==(CornerRadius cr1, CornerRadius cr2) => Equals(cr1, cr2);

	/// <summary>
	/// Determines if two CornerRadius instances are equal.
	/// </summary>
	public static bool operator !=(CornerRadius cr1, CornerRadius cr2) => !Equals(cr1, cr2);

	/// <summary>
	/// Retrieves the actual inner and outer radii.
	/// </summary>
	/// <param name="elementSize">Element size.</param>
	/// <param name="borderThickness">Border thickness.</param>
	/// <returns>Full corner radius.</returns>
	internal FullCornerRadius GetRadii(Size elementSize, Thickness borderThickness)
	{
		if (this == None)
		{
			return FullCornerRadius.None;
		}

		var outer = GetRadii(elementSize, borderThickness, true);
		var inner = GetRadii(elementSize, borderThickness, false);
		return new FullCornerRadius(outer, inner);
	}

	/// <summary>
	/// Retrieves the non-uniform radii for a border.
	/// </summary>
	/// <param name="elementSize">Element size.</param>
	/// <param name="borderThickness">Border thickness.</param>
	/// <param name="outer">True to return outer corner radii, false for inner.</param>
	/// <returns>Radii.</returns>
	private NonUniformCornerRadius GetRadii(Size elementSize, Thickness borderThickness, bool outer)
	{
		var halfLeftBorder = borderThickness.Left * 0.5;
		var halfTopBorder = borderThickness.Top * 0.5;
		var halfRightBorder = borderThickness.Right * 0.5;
		var halfBottomBorder = borderThickness.Bottom * 0.5;

		double leftTopArc, topLeftArc, topRightArc, rightTopArc, rightBottomArc, bottomRightArc, leftBottomArc, bottomLeftArc;
		leftTopArc = topLeftArc = topRightArc = rightTopArc = rightBottomArc = bottomRightArc = leftBottomArc = bottomLeftArc = 0;

		if (outer)
		{
			if (!MathHelpers.IsCloseReal(TopLeft, 0.0f))
			{
				leftTopArc = TopLeft + halfLeftBorder;
				topLeftArc = TopLeft + halfTopBorder;
			}

			if (!MathHelpers.IsCloseReal(TopRight, 0.0f))
			{
				topRightArc = TopRight + halfTopBorder;
				rightTopArc = TopRight + halfRightBorder;
			}

			if (!MathHelpers.IsCloseReal(BottomRight, 0.0f))
			{
				rightBottomArc = BottomRight + halfRightBorder;
				bottomRightArc = BottomRight + halfBottomBorder;
			}

			if (!MathHelpers.IsCloseReal(BottomLeft, 0.0f))
			{
				bottomLeftArc = BottomLeft + halfBottomBorder;
				leftBottomArc = BottomLeft + halfLeftBorder;
			}
		}
		else
		{
			leftTopArc = Math.Max(0.0f, TopLeft - halfLeftBorder);
			topLeftArc = Math.Max(0.0f, TopLeft - halfTopBorder);
			topRightArc = Math.Max(0.0f, TopRight - halfTopBorder);
			rightTopArc = Math.Max(0.0f, TopRight - halfRightBorder);
			rightBottomArc = Math.Max(0.0f, BottomRight - halfRightBorder);
			bottomRightArc = Math.Max(0.0f, BottomRight - halfBottomBorder);
			bottomLeftArc = Math.Max(0.0f, BottomLeft - halfBottomBorder);
			leftBottomArc = Math.Max(0.0f, BottomLeft - halfLeftBorder);
		}

		// Adjust the corner radius to fit element size
		// When neighboring corners "overlap", we distribute
		// them "fairly" along the side.
		double ratio;

		if (leftTopArc + rightTopArc > elementSize.Width)
		{
			ratio = leftTopArc / (leftTopArc + rightTopArc);
			leftTopArc = ratio * elementSize.Width;
			rightTopArc = elementSize.Width - leftTopArc;
		}

		if (topRightArc + bottomRightArc > elementSize.Height)
		{
			ratio = topRightArc / (topRightArc + bottomRightArc);
			topRightArc = ratio * elementSize.Height;
			bottomRightArc = elementSize.Height - topRightArc;
		}

		if (rightBottomArc + leftBottomArc > elementSize.Width)
		{
			ratio = rightBottomArc / (rightBottomArc + leftBottomArc);
			rightBottomArc = ratio * elementSize.Width;
			leftBottomArc = elementSize.Width - rightBottomArc;
		}

		if (bottomLeftArc + topLeftArc > elementSize.Height)
		{
			ratio = bottomLeftArc / (bottomLeftArc + topLeftArc);
			bottomLeftArc = ratio * elementSize.Height;
			topLeftArc = elementSize.Height - bottomLeftArc;
		}

		return new(
			new((float)leftTopArc, (float)topLeftArc),
			new((float)rightTopArc, (float)topRightArc),
			new((float)rightBottomArc, (float)bottomRightArc),
			new((float)leftBottomArc, (float)bottomLeftArc));
	}
}
