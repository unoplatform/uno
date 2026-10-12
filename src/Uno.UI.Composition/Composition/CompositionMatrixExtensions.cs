#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;
using Uno.Extensions;

namespace Microsoft.UI.Composition;

internal static class CompositionMatrixExtensions
{
	public static Matrix3x2 ToMatrix3x2(this Matrix4x4 m)
		=> new Matrix3x2(m.M11, m.M12, m.M21, m.M22, m.M41, m.M42);

	/// <summary>
	/// True when the matrix maps the z=0 plane without perspective, which is what makes
	/// <see cref="ToMatrix3x2"/> exact. A pure 3D rotation qualifies: the content is planar, so its
	/// footprint stays affine even though the matrix carries depth terms.
	/// </summary>
	public static bool IsPlanarAffine(this Matrix4x4 m)
		=> m.M14 == 0f && m.M24 == 0f && m.M44 == 1f;

	/// <summary>
	/// Root-space bounds of <paramref name="rect"/> under a matrix that may carry perspective — which a
	/// <see cref="Matrix3x2"/> cannot express, so the corners are projected and divided through. Returns false
	/// when a corner falls on or behind the eye plane: the image is unbounded there, and the caller must
	/// neither cull nor clip against it.
	/// </summary>
	public static bool TryTransformBounds(this Rect rect, Matrix4x4 m, out Rect bounds)
	{
		if (m.IsPlanarAffine())
		{
			bounds = rect.Transform(m.ToMatrix3x2());
			return true;
		}

		if (!TryProject(m, rect.Left, rect.Top, out var x0, out var y0)
			|| !TryProject(m, rect.Right, rect.Top, out var x1, out var y1)
			|| !TryProject(m, rect.Right, rect.Bottom, out var x2, out var y2)
			|| !TryProject(m, rect.Left, rect.Bottom, out var x3, out var y3))
		{
			bounds = default;
			return false;
		}

		var minX = Math.Min(Math.Min(x0, x1), Math.Min(x2, x3));
		var maxX = Math.Max(Math.Max(x0, x1), Math.Max(x2, x3));
		var minY = Math.Min(Math.Min(y0, y1), Math.Min(y2, y3));
		var maxY = Math.Max(Math.Max(y0, y1), Math.Max(y2, y3));
		bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
		return true;
	}

	private static bool TryProject(in Matrix4x4 m, double x, double y, out double px, out double py)
	{
		var w = (x * m.M14) + (y * m.M24) + m.M44;
		if (w <= float.Epsilon)
		{
			px = py = 0;
			return false;
		}

		px = ((x * m.M11) + (y * m.M21) + m.M41) / w;
		py = ((x * m.M12) + (y * m.M22) + m.M42) / w;
		return true;
	}
}
