// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.Foundation;

namespace DirectUI;

internal class RectUtil
{
	/// <summary>
	/// Update this rectangle to be the intersection of this and rect.
	/// If either this or rect are Empty, the result is Empty as well.
	/// </summary>
	/// <param name="rect">Input rect.</param>
	/// <param name="rect2">Other rect.</param>
	public static void Intersect(
		ref Rect rect, //TODO:MZ: Verify rect is changed
		Rect rect2)
	{
		if (AreDisjoint(rect, rect2))
		{
			rect.X = DoubleUtil.PositiveInfinity;
			rect.Y = DoubleUtil.PositiveInfinity;
			rect.Width = DoubleUtil.NegativeInfinity;
			rect.Height = DoubleUtil.NegativeInfinity;
		}
		else
		{
			var left = DoubleUtil.Max(rect.X, rect2.X);
			var top = DoubleUtil.Max(rect.Y, rect2.Y);

			//  Max with 0 to prevent double weirdness from causing us to be
			// (-epsilon..0)
			rect.Width = DoubleUtil.Max(DoubleUtil.Min(rect.X + rect.Width, rect2.X + rect2.Width) - left, 0);
			rect.Height = DoubleUtil.Max(DoubleUtil.Min(rect.Y + rect.Height, rect2.Y + rect2.Height) - top, 0);

			rect.X = left;
			rect.Y = top;
		}
	}

	/// <summary>
	/// Returns true if either rect is empty or the rects
	/// have an empty intersection.
	/// </summary>
	/// <param name="rect">First rect.</param>
	/// <param name="rect2">Second rect.</param>
	/// <returns>A value indicting if the two rects have common area.</returns>
	public static bool AreDisjoint(
		Rect rect,
		Rect rect2)
	{
		bool doIntersect =
			!(rect.Width < 0 || rect2.Width < 0) &&
			(rect2.X <= rect.X + rect.Width) &&
			(rect2.X + rect2.Width >= rect.X) &&
			(rect2.Y <= rect.Y + rect.Height) &&
			(rect2.Y + rect2.Height >= rect.Y);
		return !doIntersect;
	}

}
