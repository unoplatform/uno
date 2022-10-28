using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Globalization;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Windows.UI.Xaml;

/// <summary>Defines the radius of a rectangle's corners. </summary>
[TypeConverter(typeof(CornerRadiusConverter))]
public partial struct CornerRadius : IEquatable<CornerRadius>
{
	/// <summary>Gets or sets the radius of the top-left corner.</summary>
	/// <returns>The radius of the top-left corner. The default is 0.</returns>
	public double TopLeft { get; set; }

	/// <summary>Gets or sets the radius of the top-right corner. </summary>
	/// <returns>The radius of the top-right corner. The default is 0.</returns>
	public double TopRight { get; set; }

	/// <summary>Gets or sets the radius of the bottom-right corner. </summary>
	/// <returns>The radius of the bottom-right corner. The default is 0.</returns>
	public double BottomRight { get; set; }

	/// <summary>Gets or sets the radius of the bottom-left corner. </summary>
	/// <returns>The radius of the bottom-left corner. The default is 0.</returns>
	public double BottomLeft { get; set; }

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
	/// Retrieves the radii for a border.
	/// </summary>
	/// <param name="borderThickness">Border thickness.</param>
	/// <param name="outer">True to return outer corner radii, false for inner.</param>
	/// <returns>Radii.</returns>
	internal FullCornerRadius GetRadii(Size elementSize, Thickness borderThickness, bool outer)
	{
		var halfLeft = borderThickness.Left * 0.5;
		var halfTop = borderThickness.Top * 0.5;
		var halfRight = borderThickness.Right * 0.5;
		var halfBottom = borderThickness.Bottom * 0.5;

		double leftTop, topLeft, topRight, rightTop, rightBottom, bottomRight, leftBottom, bottomLeft;
		leftTop = topLeft = topRight = rightTop = rightBottom = bottomRight = leftBottom = bottomLeft = 0;

		if (outer)
		{
			if (!MathHelpers.IsCloseReal(TopLeft, 0.0f))
			{
				leftTop = TopLeft + halfLeft;
				topLeft = TopLeft + halfTop;
			}

			if (!MathHelpers.IsCloseReal(TopRight, 0.0f))
			{
				topRight = TopRight + halfTop;
				rightTop = TopRight + halfRight;
			}

			if (!MathHelpers.IsCloseReal(BottomRight, 0.0f))
			{
				rightBottom = BottomRight + halfRight;
				bottomRight = BottomRight + halfBottom;
			}

			if (!MathHelpers.IsCloseReal(BottomLeft, 0.0f))
			{
				bottomLeft = BottomLeft + halfBottom;
				leftBottom = BottomLeft + halfLeft;
			}
		}
		else
		{
			leftTop = Math.Max(0.0f, TopLeft - halfLeft);
			topLeft = Math.Max(0.0f, TopLeft - halfTop);
			topRight = Math.Max(0.0f, TopRight - halfTop);
			rightTop = Math.Max(0.0f, TopRight - halfRight);
			rightBottom = Math.Max(0.0f, BottomRight - halfRight);
			bottomRight = Math.Max(0.0f, BottomRight - halfBottom);
			bottomLeft = Math.Max(0.0f, BottomLeft - halfBottom);
			leftBottom = Math.Max(0.0f, BottomLeft - halfLeft);
		}

		// Adjust the corner radius to fit element size
		// When neighboring corners "overlap", we distribute
		// them "fairly" along the side.
		double ratio;

		if (leftTop + rightTop > elementSize.Width)
		{
			ratio = leftTop / (leftTop + rightTop);
			leftTop = ratio * elementSize.Width;
			rightTop = elementSize.Width - leftTop;
		}

		if (topRight + bottomRight > elementSize.Height)
		{
			ratio = topRight / (topRight + bottomRight);
			topRight = ratio * elementSize.Height;
			bottomRight = elementSize.Height - topRight;
		}

		if (rightBottom + leftBottom > elementSize.Width)
		{
			ratio = rightBottom / (rightBottom + leftBottom);
			rightBottom = ratio * elementSize.Width;
			leftBottom = elementSize.Width - rightBottom;
		}

		if (bottomLeft + topLeft > elementSize.Height)
		{
			ratio = bottomLeft / (bottomLeft + topLeft);
			bottomLeft = ratio * elementSize.Height;
			topLeft = elementSize.Height - bottomLeft;
		}

		return new(
			new((float)leftTop, (float)topLeft),
			new((float)rightTop, (float)topRight),
			new((float)rightBottom, (float)bottomRight),
			new((float)leftBottom, (float)bottomLeft));
	}
}
