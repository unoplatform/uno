using System;
using System.Numerics;
using Windows.Foundation;
namespace Uno.UI.Composition;

// A close copy from MUX.CornerRadius
public readonly record struct CornerRadius(double TopLeft, double TopRight, double BottomRight, double BottomLeft)
{
	internal readonly record struct NonUniformCornerRadius
	(
		Vector2 TopLeft,
		Vector2 TopRight,
		Vector2 BottomRight,
		Vector2 BottomLeft
	)
	{
		public bool IsEmpty =>
			TopLeft == Vector2.Zero &&
			TopRight == Vector2.Zero &&
			BottomRight == Vector2.Zero &&
			BottomLeft == Vector2.Zero;

#if __SKIA__
		unsafe internal void GetRadii(SkiaSharp.SKPoint* radiiStore)
		{
			*(radiiStore++) = new(TopLeft.X, TopLeft.Y);
			*(radiiStore++) = new(TopRight.X, TopRight.Y);
			*(radiiStore++) = new(BottomRight.X, BottomRight.Y);
			*radiiStore = new(BottomLeft.X, BottomLeft.Y);
		}
#endif
	}

	internal readonly record struct FullCornerRadius
	(
		NonUniformCornerRadius Outer,
		NonUniformCornerRadius Inner
	)
	{
		public static FullCornerRadius None => default;

		public bool IsEmpty => Outer.IsEmpty && Inner.IsEmpty;
	}

	public CornerRadius(double uniformRadius) : this(uniformRadius, uniformRadius, uniformRadius, uniformRadius)
	{
	}

	/// <summary>
	/// Provides a Zero-valued corner radius.
	/// </summary>
	public static readonly CornerRadius None = new CornerRadius(0);

	/// <summary>
	/// Builds a uniform radius from a double;
	/// </summary>
	public static implicit operator CornerRadius(double uniformRadius) => new CornerRadius(uniformRadius);

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

		static bool IsCloseReal(double a, double b) => Math.Abs((a - b) / ((b == 0.0f) ? 1.0f : b)) < 10.0f * 1.192092896e-07F;

		if (outer)
		{
			if (!IsCloseReal(TopLeft, 0.0f))
			{
				leftTopArc = TopLeft + halfLeftBorder;
				topLeftArc = TopLeft + halfTopBorder;
			}

			if (!IsCloseReal(TopRight, 0.0f))
			{
				topRightArc = TopRight + halfTopBorder;
				rightTopArc = TopRight + halfRightBorder;
			}

			if (!IsCloseReal(BottomRight, 0.0f))
			{
				rightBottomArc = BottomRight + halfRightBorder;
				bottomRightArc = BottomRight + halfBottomBorder;
			}

			if (!IsCloseReal(BottomLeft, 0.0f))
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
