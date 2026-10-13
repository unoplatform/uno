#nullable enable

using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition;

namespace Uno.UI.Tests.CompositionTests;

[UITestClass]
public class Given_CompositionMathHelpers
{
	[TestMethod]
	public void When_Invertible_Then_Exact_Inverse()
	{
		var matrix = Matrix4x4.CreateRotationX(0.5f) * Matrix4x4.CreateScale(2) * Matrix4x4.CreateTranslation(10, 20, 30);

		Assert.IsTrue(CompositionMathHelpers.TryGetSubtreeInverse(matrix, out var inverse, out var isExact));
		Assert.IsTrue(isExact);
		AssertClose(Matrix4x4.Identity, matrix * inverse);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	public void When_Z_Flattened_Then_Generalized_Inverse_Undoes_The_2D_Part()
	{
		// AnimatedAcceptVisualSource's outer Scale(2, 2, 0), placed on screen.
		var matrix = new Matrix4x4(
			2, 0, 0, 0,
			0, 2, 0, 0,
			0, 0, 0, 0,
			524, 66, 0, 1);

		Assert.IsTrue(CompositionMathHelpers.TryGetSubtreeInverse(matrix, out var inverse, out var isExact));
		Assert.IsFalse(isExact);
		AssertClose(matrix, matrix * inverse * matrix);

		Matrix3x2.Invert(matrix.ToMatrix3x2(), out var inverse2D);
		AssertClose(inverse2D, inverse.ToMatrix3x2());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	public void When_Rotated_In_3D_Under_Flattening_Then_Generalized_Inverse()
	{
		// Unlike a Lottie transform, this singular matrix's Z row isn't zero.
		var matrix = Matrix4x4.CreateRotationY(0.5f) * Matrix4x4.CreateScale(2, 2, 0) * Matrix4x4.CreateTranslation(100, 50, 0);

		Assert.IsTrue(CompositionMathHelpers.TryGetSubtreeInverse(matrix, out var inverse, out var isExact));
		Assert.IsFalse(isExact);
		AssertClose(matrix, matrix * inverse * matrix);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	[DataRow(0f, 1f)]
	[DataRow(0f, 0f)]
	public void When_Rank_Two_Or_Less_Then_None(float scaleX, float scaleY)
	{
		var matrix = Matrix4x4.CreateScale(scaleX, scaleY, 0) * Matrix4x4.CreateTranslation(10, 20, 0);

		Assert.IsFalse(CompositionMathHelpers.TryGetSubtreeInverse(matrix, out var inverse, out _));
		Assert.IsTrue(float.IsNaN(inverse.M11));
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25393")]
	public void When_Rank_Two_With_Rounding_Then_No_Inverse_That_Fails_To_Undo_It()
	{
		// Rank 2, but rounding leaves its cofactors slightly off zero, so it can look rank 3.
		var matrix = Matrix4x4.CreateRotationZ(0.3f) * Matrix4x4.CreateScale(0, 1, 0) * Matrix4x4.CreateRotationZ(0.4f);

		if (CompositionMathHelpers.TryGetSubtreeInverse(matrix, out var inverse, out _))
		{
			AssertClose(matrix, matrix * inverse * matrix);
		}
		else
		{
			Assert.IsTrue(float.IsNaN(inverse.M11));
		}
	}

	private static void AssertClose(Matrix4x4 expected, Matrix4x4 actual)
	{
		for (var row = 0; row < 4; row++)
		{
			for (var column = 0; column < 4; column++)
			{
				Assert.AreEqual(expected[row, column], actual[row, column], 1e-3f, $"[{row}, {column}] of {actual}, expected {expected}");
			}
		}
	}

	private static void AssertClose(Matrix3x2 expected, Matrix3x2 actual)
		=> AssertClose(new Matrix4x4(expected), new Matrix4x4(actual));
}
