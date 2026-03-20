using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Media.Media3D;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_Matrix3D
{
	[TestMethod]
	public void When_Default_Constructor()
	{
		var matrix = new Matrix3D();

		Assert.AreEqual(0, matrix.M11);
		Assert.AreEqual(0, matrix.M12);
		Assert.AreEqual(0, matrix.M13);
		Assert.AreEqual(0, matrix.M14);
		Assert.AreEqual(0, matrix.M21);
		Assert.AreEqual(0, matrix.M22);
		Assert.AreEqual(0, matrix.M23);
		Assert.AreEqual(0, matrix.M24);
		Assert.AreEqual(0, matrix.M31);
		Assert.AreEqual(0, matrix.M32);
		Assert.AreEqual(0, matrix.M33);
		Assert.AreEqual(0, matrix.M34);
		Assert.AreEqual(0, matrix.OffsetX);
		Assert.AreEqual(0, matrix.OffsetY);
		Assert.AreEqual(0, matrix.OffsetZ);
		Assert.AreEqual(0, matrix.M44);
	}

	[TestMethod]
	public void When_Full_Constructor()
	{
		var matrix = new Matrix3D(
			1, 2, 3, 4,
			5, 6, 7, 8,
			9, 10, 11, 12,
			13, 14, 15, 16);

		Assert.AreEqual(1, matrix.M11);
		Assert.AreEqual(2, matrix.M12);
		Assert.AreEqual(3, matrix.M13);
		Assert.AreEqual(4, matrix.M14);
		Assert.AreEqual(5, matrix.M21);
		Assert.AreEqual(6, matrix.M22);
		Assert.AreEqual(7, matrix.M23);
		Assert.AreEqual(8, matrix.M24);
		Assert.AreEqual(9, matrix.M31);
		Assert.AreEqual(10, matrix.M32);
		Assert.AreEqual(11, matrix.M33);
		Assert.AreEqual(12, matrix.M34);
		Assert.AreEqual(13, matrix.OffsetX);
		Assert.AreEqual(14, matrix.OffsetY);
		Assert.AreEqual(15, matrix.OffsetZ);
		Assert.AreEqual(16, matrix.M44);
	}

	[TestMethod]
	public void When_Identity()
	{
		var identity = Matrix3D.Identity;

		Assert.AreEqual(1, identity.M11);
		Assert.AreEqual(0, identity.M12);
		Assert.AreEqual(0, identity.M13);
		Assert.AreEqual(0, identity.M14);
		Assert.AreEqual(0, identity.M21);
		Assert.AreEqual(1, identity.M22);
		Assert.AreEqual(0, identity.M23);
		Assert.AreEqual(0, identity.M24);
		Assert.AreEqual(0, identity.M31);
		Assert.AreEqual(0, identity.M32);
		Assert.AreEqual(1, identity.M33);
		Assert.AreEqual(0, identity.M34);
		Assert.AreEqual(0, identity.OffsetX);
		Assert.AreEqual(0, identity.OffsetY);
		Assert.AreEqual(0, identity.OffsetZ);
		Assert.AreEqual(1, identity.M44);
	}

	[TestMethod]
	public void When_IsIdentity_True()
	{
		var identity = Matrix3D.Identity;
		Assert.IsTrue(identity.IsIdentity);
	}

	[TestMethod]
	public void When_IsIdentity_False()
	{
		var matrix = new Matrix3D(
			1, 0, 0, 0,
			0, 1, 0, 0,
			0, 0, 1, 0,
			10, 0, 0, 1);

		Assert.IsFalse(matrix.IsIdentity);
	}

	[TestMethod]
	public void When_Equality_True()
	{
		var matrix1 = new Matrix3D(
			1, 2, 3, 4,
			5, 6, 7, 8,
			9, 10, 11, 12,
			13, 14, 15, 16);

		var matrix2 = new Matrix3D(
			1, 2, 3, 4,
			5, 6, 7, 8,
			9, 10, 11, 12,
			13, 14, 15, 16);

		Assert.IsTrue(matrix1 == matrix2);
		Assert.IsTrue(matrix1.Equals(matrix2));
		Assert.AreEqual(matrix1, matrix2);
	}

	[TestMethod]
	public void When_Equality_False()
	{
		var matrix1 = new Matrix3D(
			1, 2, 3, 4,
			5, 6, 7, 8,
			9, 10, 11, 12,
			13, 14, 15, 16);

		var matrix2 = new Matrix3D(
			1, 2, 3, 4,
			5, 6, 7, 8,
			9, 10, 11, 12,
			13, 14, 15, 17); // Different M44

		Assert.IsTrue(matrix1 != matrix2);
		Assert.IsFalse(matrix1.Equals(matrix2));
		Assert.AreNotEqual(matrix1, matrix2);
	}

	[TestMethod]
	public void When_Multiply_Identity()
	{
		var matrix = new Matrix3D(
			1, 2, 3, 4,
			5, 6, 7, 8,
			9, 10, 11, 12,
			13, 14, 15, 16);

		var result = matrix * Matrix3D.Identity;

		Assert.AreEqual(matrix, result);
	}

	[TestMethod]
	public void When_Multiply_Translation()
	{
		var identity = Matrix3D.Identity;
		var translation = new Matrix3D(
			1, 0, 0, 0,
			0, 1, 0, 0,
			0, 0, 1, 0,
			10, 20, 30, 1);

		var result = identity * translation;

		Assert.AreEqual(10, result.OffsetX);
		Assert.AreEqual(20, result.OffsetY);
		Assert.AreEqual(30, result.OffsetZ);
	}

	[TestMethod]
	public void When_HasInverse_True()
	{
		var identity = Matrix3D.Identity;
		Assert.IsTrue(identity.HasInverse);
	}

	[TestMethod]
	public void When_HasInverse_False()
	{
		// Singular matrix (all zeros)
		var singular = new Matrix3D();
		Assert.IsFalse(singular.HasInverse);
	}

	[TestMethod]
	public void When_Invert_Identity()
	{
		var matrix = Matrix3D.Identity;
		matrix.Invert();

		Assert.IsTrue(matrix.IsIdentity);
	}

	[TestMethod]
	public void When_Invert_Translation()
	{
		var matrix = new Matrix3D(
			1, 0, 0, 0,
			0, 1, 0, 0,
			0, 0, 1, 0,
			10, 20, 30, 1);

		matrix.Invert();

		Assert.AreEqual(-10, matrix.OffsetX, 1e-10);
		Assert.AreEqual(-20, matrix.OffsetY, 1e-10);
		Assert.AreEqual(-30, matrix.OffsetZ, 1e-10);
	}

	[TestMethod]
	public void When_Invert_Singular_Throws()
	{
		var singular = new Matrix3D();
		Assert.ThrowsExactly<InvalidOperationException>(() => singular.Invert());
	}

	[TestMethod]
	public void When_ToString()
	{
		var identity = Matrix3D.Identity;
		var str = identity.ToString();

		Assert.IsNotNull(str);
		Assert.AreEqual(nameof(Matrix3D.Identity), str);
	}

#if HAS_UNO
	[TestMethod]
	public void When_ToMatrix4x4_And_Back()
	{
		var original = new Matrix3D(
			1, 2, 3, 4,
			5, 6, 7, 8,
			9, 10, 11, 12,
			13, 14, 15, 16);

		var matrix4x4 = original.ToMatrix4x4();
		var roundTripped = Matrix3D.FromMatrix4x4(matrix4x4);

		// Note: Some precision loss due to float conversion
		Assert.AreEqual(original.M11, roundTripped.M11, 1e-5);
		Assert.AreEqual(original.M22, roundTripped.M22, 1e-5);
		Assert.AreEqual(original.M33, roundTripped.M33, 1e-5);
		Assert.AreEqual(original.M44, roundTripped.M44, 1e-5);
	}
#endif
}
