using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Windows.UI.Xaml
{
	/// <summary>Defines the radius of a rectangle's corners. </summary>
	[TypeConverter(typeof(CornerRadiusConverter))]
	public partial struct CornerRadius
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

		/// <summary>
		/// Determines if two CornerRadius instances are equal.
		/// </summary>
		public override bool Equals(object obj)
		{
			if(obj is CornerRadius other)
			{
				return other.TopLeft == TopLeft
					&& other.TopRight == TopRight
					&& other.BottomLeft == BottomLeft
					&& other.BottomRight == BottomRight;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return TopLeft.GetHashCode()
				^ TopRight.GetHashCode()
				^ BottomLeft.GetHashCode()
				^ BottomRight.GetHashCode();
		}

		public override string ToString()
		{
			return "TopLeft: {0}, TopRight: {1}, BottomRight: {2}, BottomLeft: {3}".InvariantCultureFormat(TopLeft, TopRight, BottomRight, BottomLeft);
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
		/// Determines if two CornerRadius instances are equal.
		/// </summary>
		public static bool operator ==(CornerRadius cr1, CornerRadius cr2) => Equals(cr1, cr2);

		/// <summary>
		/// Determines if two CornerRadius instances are equal.
		/// </summary>
		public static bool operator !=(CornerRadius cr1, CornerRadius cr2) => !Equals(cr1, cr2);
	}
}
