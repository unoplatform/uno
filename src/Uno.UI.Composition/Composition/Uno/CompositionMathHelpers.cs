using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Uno.UI.Composition;

internal static class CompositionMathHelpers
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsCloseReal(float a, float b, float epsilon = 10.0f * float.Epsilon)
		=> MathF.Abs(a - b) <= epsilon;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool IsCloseRealZero(float a, float epsilon = 10.0f * float.Epsilon)
		=> MathF.Abs(a) < epsilon;

	/// <summary>
	/// The matrix that takes a visual's <paramref name="totalMatrix"/> back out of its descendants' TotalMatrix, so the
	/// subtree can be drawn in the visual's own space. When the matrix flattens an axis (Lottie's Scale(x, y, 0) zeroes
	/// M33) it has no inverse, and this returns a generalized inverse G with totalMatrix * G * totalMatrix == totalMatrix.
	/// That is exact for a subtree drawn under totalMatrix again, as a recording replayed in place is, but it is not an
	/// inverse to map points with.
	/// </summary>
	/// <param name="isExact">False for a generalized inverse: totalMatrix * inverse is then a projection, not identity.</param>
	/// <returns>False, with an all-NaN result, when the matrix collapses everything onto a line or a point.</returns>
	internal static bool TryGetSubtreeInverse(in Matrix4x4 totalMatrix, out Matrix4x4 inverse, out bool isExact)
	{
		isExact = Matrix4x4.Invert(totalMatrix, out inverse);
		if (isExact)
		{
			return true;
		}

		// A rank-3 matrix has rank-1 cofactors: their rows are right null vectors (m * v = 0) and their columns left null
		// vectors (u * m = 0). Adding u ⊗ v makes the matrix invertible, and that inverse is a generalized inverse of it.
		var cofactors = default(Matrix4x4);
		var (maxRow, maxColumn, max) = (0, 0, 0f);
		for (var row = 0; row < 4; row++)
		{
			for (var column = 0; column < 4; column++)
			{
				var cofactor = cofactors[row, column] = Cofactor(totalMatrix, row, column);
				if (MathF.Abs(cofactor) > max)
				{
					(maxRow, maxColumn, max) = (row, column, MathF.Abs(cofactor));
				}
			}
		}

		if (max == 0)
		{
			return false;
		}

		var u = Vector4.Normalize(new Vector4(cofactors[0, maxColumn], cofactors[1, maxColumn], cofactors[2, maxColumn], cofactors[3, maxColumn]));
		var v = Vector4.Normalize(new Vector4(cofactors[maxRow, 0], cofactors[maxRow, 1], cofactors[maxRow, 2], cofactors[maxRow, 3]));

		var updated = totalMatrix;
		for (var row = 0; row < 4; row++)
		{
			for (var column = 0; column < 4; column++)
			{
				updated[row, column] += u[row] * v[column];
			}
		}

		// Rounding can make a degenerate matrix look rank 3, and the inverse found then doesn't undo it.
		if (Matrix4x4.Invert(updated, out inverse) && IsGeneralizedInverse(totalMatrix, inverse))
		{
			return true;
		}

		inverse = Matrix4x4.Identity * float.NaN;
		return false;
	}

	// matrix * inverse * matrix == matrix, within float precision. NaN or infinite entries fail it.
	private static bool IsGeneralizedInverse(in Matrix4x4 matrix, in Matrix4x4 inverse)
		=> MaxAbs(matrix * inverse * matrix - matrix) <= 1e-4f * MaxAbs(matrix);

	private static float MaxAbs(in Matrix4x4 m)
	{
		var max = 0f;
		for (var row = 0; row < 4; row++)
		{
			for (var column = 0; column < 4; column++)
			{
				max = MathF.Max(max, MathF.Abs(m[row, column]));
			}
		}

		return max;
	}

	private static float Cofactor(in Matrix4x4 m, int row, int column)
	{
		// The rows and columns of the 3x3 minor, in order, skipping the given row and column.
		var (r0, r1, r2) = (row == 0 ? 1 : 0, row <= 1 ? 2 : 1, row <= 2 ? 3 : 2);
		var (c0, c1, c2) = (column == 0 ? 1 : 0, column <= 1 ? 2 : 1, column <= 2 ? 3 : 2);

		var minor =
			m[r0, c0] * (m[r1, c1] * m[r2, c2] - m[r1, c2] * m[r2, c1])
			- m[r0, c1] * (m[r1, c0] * m[r2, c2] - m[r1, c2] * m[r2, c0])
			+ m[r0, c2] * (m[r1, c0] * m[r2, c1] - m[r1, c1] * m[r2, c0]);

		return ((row + column) & 1) == 0 ? minor : -minor;
	}
}
