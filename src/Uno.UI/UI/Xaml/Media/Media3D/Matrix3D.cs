using System;
using System.Numerics;

namespace Microsoft.UI.Xaml.Media.Media3D;

/// <summary>
/// Represents a 4 × 4 matrix that is used for transformations in a three-dimensional (3-D) space.
/// </summary>
public partial struct Matrix3D : IFormattable, IEquatable<Matrix3D>
{
	public double M11;
	public double M12;
	public double M13;
	public double M14;
	public double M21;
	public double M22;
	public double M23;
	public double M24;
	public double M31;
	public double M32;
	public double M33;
	public double M34;
	public double OffsetX;
	public double OffsetY;
	public double OffsetZ;
	public double M44;

	/// <summary>
	/// Initializes a new instance of the Matrix3D structure.
	/// </summary>
	public Matrix3D(
		double m11, double m12, double m13, double m14,
		double m21, double m22, double m23, double m24,
		double m31, double m32, double m33, double m34,
		double offsetX, double offsetY, double offsetZ, double m44)
	{
		M11 = m11;
		M12 = m12;
		M13 = m13;
		M14 = m14;
		M21 = m21;
		M22 = m22;
		M23 = m23;
		M24 = m24;
		M31 = m31;
		M32 = m32;
		M33 = m33;
		M34 = m34;
		OffsetX = offsetX;
		OffsetY = offsetY;
		OffsetZ = offsetZ;
		M44 = m44;
	}

	/// <summary>
	/// Gets an identity Matrix3D.
	/// </summary>
	public static Matrix3D Identity => new Matrix3D(
		1, 0, 0, 0,
		0, 1, 0, 0,
		0, 0, 1, 0,
		0, 0, 0, 1);

	/// <summary>
	/// Gets a value that indicates whether this Matrix3D is an identity matrix.
	/// </summary>
	public bool IsIdentity =>
		M11 == 1 && M12 == 0 && M13 == 0 && M14 == 0 &&
		M21 == 0 && M22 == 1 && M23 == 0 && M24 == 0 &&
		M31 == 0 && M32 == 0 && M33 == 1 && M34 == 0 &&
		OffsetX == 0 && OffsetY == 0 && OffsetZ == 0 && M44 == 1;

	/// <summary>
	/// Gets a value that indicates whether this Matrix3D is invertible.
	/// </summary>
	public bool HasInverse => GetDeterminant() != 0;

	/// <summary>
	/// Inverts this Matrix3D structure.
	/// </summary>
	/// <exception cref="InvalidOperationException">The matrix is not invertible.</exception>
	public void Invert()
	{
		var matrix4x4 = ToMatrix4x4();
		if (!Matrix4x4.Invert(matrix4x4, out var inverted))
		{
			throw new InvalidOperationException("Matrix is not invertible.");
		}
		this = FromMatrix4x4(inverted);
	}

	/// <summary>
	/// Multiplies two Matrix3D structures.
	/// </summary>
	public static Matrix3D operator *(Matrix3D matrix1, Matrix3D matrix2)
	{
		return new Matrix3D(
			matrix1.M11 * matrix2.M11 + matrix1.M12 * matrix2.M21 + matrix1.M13 * matrix2.M31 + matrix1.M14 * matrix2.OffsetX,
			matrix1.M11 * matrix2.M12 + matrix1.M12 * matrix2.M22 + matrix1.M13 * matrix2.M32 + matrix1.M14 * matrix2.OffsetY,
			matrix1.M11 * matrix2.M13 + matrix1.M12 * matrix2.M23 + matrix1.M13 * matrix2.M33 + matrix1.M14 * matrix2.OffsetZ,
			matrix1.M11 * matrix2.M14 + matrix1.M12 * matrix2.M24 + matrix1.M13 * matrix2.M34 + matrix1.M14 * matrix2.M44,

			matrix1.M21 * matrix2.M11 + matrix1.M22 * matrix2.M21 + matrix1.M23 * matrix2.M31 + matrix1.M24 * matrix2.OffsetX,
			matrix1.M21 * matrix2.M12 + matrix1.M22 * matrix2.M22 + matrix1.M23 * matrix2.M32 + matrix1.M24 * matrix2.OffsetY,
			matrix1.M21 * matrix2.M13 + matrix1.M22 * matrix2.M23 + matrix1.M23 * matrix2.M33 + matrix1.M24 * matrix2.OffsetZ,
			matrix1.M21 * matrix2.M14 + matrix1.M22 * matrix2.M24 + matrix1.M23 * matrix2.M34 + matrix1.M24 * matrix2.M44,

			matrix1.M31 * matrix2.M11 + matrix1.M32 * matrix2.M21 + matrix1.M33 * matrix2.M31 + matrix1.M34 * matrix2.OffsetX,
			matrix1.M31 * matrix2.M12 + matrix1.M32 * matrix2.M22 + matrix1.M33 * matrix2.M32 + matrix1.M34 * matrix2.OffsetY,
			matrix1.M31 * matrix2.M13 + matrix1.M32 * matrix2.M23 + matrix1.M33 * matrix2.M33 + matrix1.M34 * matrix2.OffsetZ,
			matrix1.M31 * matrix2.M14 + matrix1.M32 * matrix2.M24 + matrix1.M33 * matrix2.M34 + matrix1.M34 * matrix2.M44,

			matrix1.OffsetX * matrix2.M11 + matrix1.OffsetY * matrix2.M21 + matrix1.OffsetZ * matrix2.M31 + matrix1.M44 * matrix2.OffsetX,
			matrix1.OffsetX * matrix2.M12 + matrix1.OffsetY * matrix2.M22 + matrix1.OffsetZ * matrix2.M32 + matrix1.M44 * matrix2.OffsetY,
			matrix1.OffsetX * matrix2.M13 + matrix1.OffsetY * matrix2.M23 + matrix1.OffsetZ * matrix2.M33 + matrix1.M44 * matrix2.OffsetZ,
			matrix1.OffsetX * matrix2.M14 + matrix1.OffsetY * matrix2.M24 + matrix1.OffsetZ * matrix2.M34 + matrix1.M44 * matrix2.M44
		);
	}

	/// <summary>
	/// Compares two Matrix3D instances for equality.
	/// </summary>
	public static bool operator ==(Matrix3D matrix1, Matrix3D matrix2) => matrix1.Equals(matrix2);

	/// <summary>
	/// Compares two Matrix3D instances for inequality.
	/// </summary>
	public static bool operator !=(Matrix3D matrix1, Matrix3D matrix2) => !matrix1.Equals(matrix2);

	/// <inheritdoc/>
	public override bool Equals(object o) => o is Matrix3D matrix && Equals(matrix);

	/// <summary>
	/// Compares two Matrix3D instances for equality.
	/// </summary>
	public bool Equals(Matrix3D value) =>
		M11 == value.M11 && M12 == value.M12 && M13 == value.M13 && M14 == value.M14 &&
		M21 == value.M21 && M22 == value.M22 && M23 == value.M23 && M24 == value.M24 &&
		M31 == value.M31 && M32 == value.M32 && M33 == value.M33 && M34 == value.M34 &&
		OffsetX == value.OffsetX && OffsetY == value.OffsetY && OffsetZ == value.OffsetZ && M44 == value.M44;

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hash = new HashCode();
		hash.Add(M11);
		hash.Add(M12);
		hash.Add(M13);
		hash.Add(M14);
		hash.Add(M21);
		hash.Add(M22);
		hash.Add(M23);
		hash.Add(M24);
		hash.Add(M31);
		hash.Add(M32);
		hash.Add(M33);
		hash.Add(M34);
		hash.Add(OffsetX);
		hash.Add(OffsetY);
		hash.Add(OffsetZ);
		hash.Add(M44);
		return hash.ToHashCode();
	}

	/// <inheritdoc/>
	public override string ToString() => ToString(null, null);

	/// <summary>
	/// Creates a string representation of this Matrix3D.
	/// </summary>
	public string ToString(IFormatProvider provider) => ToString(null, provider);

	/// <summary>
	/// Creates a string representation of this Matrix3D.
	/// </summary>
	public string ToString(string format, IFormatProvider provider)
	{
		var separator = ",";
		return string.Format(provider,
			"{1}{0}{2}{0}{3}{0}{4}{0}{5}{0}{6}{0}{7}{0}{8}{0}{9}{0}{10}{0}{11}{0}{12}{0}{13}{0}{14}{0}{15}{0}{16}",
			separator,
			M11, M12, M13, M14,
			M21, M22, M23, M24,
			M31, M32, M33, M34,
			OffsetX, OffsetY, OffsetZ, M44);
	}

	/// <summary>
	/// Converts this Matrix3D to a System.Numerics.Matrix4x4.
	/// </summary>
	internal Matrix4x4 ToMatrix4x4() => new Matrix4x4(
		(float)M11, (float)M12, (float)M13, (float)M14,
		(float)M21, (float)M22, (float)M23, (float)M24,
		(float)M31, (float)M32, (float)M33, (float)M34,
		(float)OffsetX, (float)OffsetY, (float)OffsetZ, (float)M44);

	/// <summary>
	/// Creates a Matrix3D from a System.Numerics.Matrix4x4.
	/// </summary>
	internal static Matrix3D FromMatrix4x4(Matrix4x4 matrix) => new Matrix3D(
		matrix.M11, matrix.M12, matrix.M13, matrix.M14,
		matrix.M21, matrix.M22, matrix.M23, matrix.M24,
		matrix.M31, matrix.M32, matrix.M33, matrix.M34,
		matrix.M41, matrix.M42, matrix.M43, matrix.M44);

	private double GetDeterminant()
	{
		// Using cofactor expansion along the first row
		double a = M11 * GetMinor3x3(M22, M23, M24, M32, M33, M34, OffsetY, OffsetZ, M44);
		double b = M12 * GetMinor3x3(M21, M23, M24, M31, M33, M34, OffsetX, OffsetZ, M44);
		double c = M13 * GetMinor3x3(M21, M22, M24, M31, M32, M34, OffsetX, OffsetY, M44);
		double d = M14 * GetMinor3x3(M21, M22, M23, M31, M32, M33, OffsetX, OffsetY, OffsetZ);
		return a - b + c - d;
	}

	private static double GetMinor3x3(
		double m11, double m12, double m13,
		double m21, double m22, double m23,
		double m31, double m32, double m33)
	{
		return m11 * (m22 * m33 - m23 * m32)
			 - m12 * (m21 * m33 - m23 * m31)
			 + m13 * (m21 * m32 - m22 * m31);
	}
}
