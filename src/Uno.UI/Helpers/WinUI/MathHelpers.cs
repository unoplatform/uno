// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// inlined.cpp, math.cpp

using System;
using Windows.Foundation;

namespace Uno.UI.Helpers.WinUI
{
	internal static class MathHelpers
	{
		private const double REAL_EPSILON = 1.192092896e-07F /* FLT_EPSILON */;

		/// <summary>
		/// Determines if two rectangles intersect.
		/// </summary>
		/// <param name="a">First rectangle.</param>
		/// <param name="b">Second rectangle.</param>
		/// <returns>True if rectangles intersect.</returns>
		internal static bool DoRectsIntersect(Rect a, Rect b) =>
			(a.Left < b.Right) &&
			(a.Top < b.Bottom) &&
			(a.Right > b.Left) &&
			(a.Bottom > b.Top);

		internal static bool IsEmptyRect(Rect rect) =>
			rect.IsEmpty ||
			rect.Width <= 0 ||
			rect.Height <= 0;

		/// <summary>
		/// Returns the Dot Product of two vectors (point structs).
		/// </summary>
		/// <param name="vecA">First vector.</param>
		/// <param name="vecB">Second vector.</param>
		/// <returns>Dot product.</returns>
		internal static double DotProduct(Point vecA, Point vecB) =>
			(vecA.X * vecB.X) + (vecA.Y * vecB.Y);

		/// <summary>
		/// Determines if rect "contained" is inside of "container".
		/// This is an inclusive comparison (a rect is contained inside itself).
		/// </summary>
		/// <param name="container">Container rect.</param>
		/// <param name="contained">Contained rect.</param>
		/// <returns>True if "container" contains "contained".</returns>
		internal static bool DoesRectContainRect(Rect container, Rect contained) =>
			(container.Left <= contained.Left) &&
			(container.Top <= contained.Top) &&
			(container.Right >= contained.Right) &&
			(container.Bottom >= contained.Bottom);

		/// <summary>
		/// Converts a Rect into an array of 4 points (in counter-clockwise order)
		/// </summary>
		/// <param name="rect">Rect.</param>
		/// <returns>Points of the rect.</returns>
		internal static Point[] RectToPoints(Rect rect)
		{
			var points = new Point[4];
			points[0].X = rect.Left;
			points[0].Y = rect.Top;
			points[1].X = rect.Left;
			points[1].Y = rect.Bottom;
			points[2].X = rect.Right;
			points[2].Y = rect.Bottom;
			points[3].X = rect.Right;
			points[3].Y = rect.Top;

			EnsureCounterClockwiseWindingOrder(points);

			return points;
		}

		/// <summary>
		/// Does this polygon intersect the other?
		/// </summary>
		/// <param name="pPtPolyA">Polygon A.</param>
		/// <param name="pPtPolyB">Polygon B.</param>
		/// <returns>True if the polygons intersect.</returns>
		/// <remarks>
		/// For this method to work, the points in both polygons
		/// MUST BE wound counter-clockwise.
		/// </remarks>
		internal static bool DoPolygonsIntersect(
			Point[] pPtPolyA,
			Point[] pPtPolyB)
		{
			int cPolyA = pPtPolyA.Length;
			// Test B in A
			for (int i = 0; i < pPtPolyA.Length; i++)
			{
				Point vecEdge = pPtPolyA[(i + 1) % cPolyA] - pPtPolyA[i];
				if (WhichSide(pPtPolyB, pPtPolyA[i], vecEdge) < 0)
				{
					// The whole of the polygon is entirely on the outside of the edge,
					// so we can never intersect
					return false;
				}
			}

			int cPolyB = pPtPolyB.Length;
			// Test A in B
			for (int i = 0; i < cPolyB; i++)
			{
				Point vecEdge = pPtPolyB[(i + 1) % cPolyB] - pPtPolyB[i];
				if (WhichSide(pPtPolyA, pPtPolyB[i], vecEdge) < 0)
				{
					// The whole of the polygon is entirely on the outside of the edge,
					// so we can never intersect
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Is polygon A wholly contained inside polygon B?		
		/// Ignores the z values in polygon A.
		/// </summary>
		/// <param name="pPtPolyA">First polygon.</param>
		/// <param name="pPtPolyB">Second polygon.</param>
		/// <returns>True if polygon is entirely contained.</returns>
		internal static bool IsEntirelyContained(Point[] pPtPolyA, Point[] pPtPolyB)
		{
			var cPolyB = pPtPolyB.Length;
			for (uint i = 0; i < cPolyB; i++)
			{
				Point vecEdge = pPtPolyB[(i + 1) % cPolyB] - pPtPolyB[i];
				if (WhichSide(pPtPolyA, pPtPolyB[i], vecEdge) <= 0)
				{
					// The whole of the polygon is entirely on the outside of the edge,
					// so we can never intersect
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Internal helper to determine which side of a given edge a set
		/// of points is.
		/// </summary>
		/// <param name="pPtPoly"></param>
		/// <param name="ptCurrent"></param>
		/// <param name="vecEdge"></param>
		/// <returns>
		/// -1 if all points are on the outside,
		/// 1 if all are on the inside,
		/// 0 if there are points on both sides.
		/// </returns>
		private static int WhichSide(Point[] pPtPoly, Point ptCurrent, Point vecEdge)
		{
			uint nPositive = 0;
			uint nNegative = 0;
			uint nZero = 0;

			//
			// The idea here is to use the sign dot product of the outward facing normal
			// of the edge on one polygon with a vector from the start of the edge to each
			// point in the other polygon.  We choose the winding order of the polygons so
			// that all positive dot products mean on the inside and all negative ones mean
			// outside - if all the points are on the same side of this edge, then they are
			// either all out or all in. If we have hits on both sides then the polygon
			// crosses the edge.
			//

			// Get the outward facing normal to the edge by transposing it
			Point vecEdgeNormal = Point.Zero;
			vecEdgeNormal.X = vecEdge.Y;
			vecEdgeNormal.Y = -vecEdge.X;

			int cPoly = pPtPoly.Length;
			for (uint i = 0; i < cPoly; i++)
			{
				// Get a vector from the current point in the other polygon
				// to each of the points in this polygon
				Point vecCurrent = PointSubtract(ptCurrent, pPtPoly[i]);

				// Use a DOT product to determine side
				double rDot = DotProduct(vecCurrent, vecEdgeNormal);
				if (rDot > 0.0f)
				{
					nPositive++;
				}
				else if (rDot < 0.0f)
				{
					nNegative++;
				}
				else
				{
					nZero++;
				}

				// We can early out if we have points on both side of the line
				// ( meaning we crossed an edge ) or if we have a zero ( meaning
				// the edges are overlapping )
				if (((nPositive > 0) && (nNegative > 0))
					 || (nZero > 0))
				{
					return 0;
				}
			}

			// We went through the entire polygon - we either had all in or all out
			// or we would have returned sooner
			return (nPositive > 0) ? 1 : -1;
		}

		private static Point PointSubtract(Point from, Point to) => to - from;

		private static bool EnsureCounterClockwiseWindingOrder(Point[] points)
		{
			// Ensure that the vertices have clockwise winding order
			if (points.Length > 2)
			{
				var vec1 = PointSubtract(points[0], points[1]);
				var vec2 = PointSubtract(points[1], points[2]);
				var lastIndex = points.Length - 1;

				// If the cross-product is positive (i.e. the winding is CW)
				if (vec1.X * vec2.Y - vec2.X * vec1.Y > 0.0f)
				{
					// Reverse the vertex order
					for (int i = 0; i < points.Length / 2; i++)
					{
						// Swap
						var ptTemp = points[i];
						points[i] = points[lastIndex - i];
						points[lastIndex - i] = ptTemp;
					}
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Return TRUE if two points are close. Close is defined as near enough
		/// that the rounding to 32bit float precision could have resulted in the
		/// difference. We define an arbitrary number of allowed rounding errors (10).
		/// We divide by b to normalize the difference. It doesn't matter which point
		/// we divide by - if they're significantly different, we'll return true, and
		/// if they're really close, then a==b (almost).
		/// </summary>
		/// <param name="a">input number to compare.</param>
		/// <param name="b">input number to compare.</param>
		/// <returns>TRUE if the numbers are close enough.</returns>
		internal static bool IsCloseReal(double a, double b)
		{
			// if b == 0.0f we don't want to divide by zero. If this happens
			// it's sufficient to use 1.0 as the divisor because REAL_EPSILON
			// should be good enough to test if a number is close enough to zero.

			// NOTE: if b << a, this could cause an FP overflow. Currently we mask
			// these exceptions, but if we unmask them, we should probably check
			// the divide.

			// We assume we can generate an overflow exception without taking down
			// the system. We will still get the right results based on the FPU
			// default handling of the overflow.

			// Ensure that anyone clearing the overflow mask comes and revisits this
			// assumption. If you hit this Assert, it means that the #O exception mask
			// has been cleared. Go check c_wFPCtrlExceptions.
			return Math.Abs((a - b) / ((b == 0.0f) ? 1.0f : b)) < 10.0f * REAL_EPSILON;
		}
	}
}
