#nullable enable

using System.Numerics;

namespace Microsoft.UI.Composition;

internal static class CompositionMatrixExtensions
{
	public static Matrix3x2 ToMatrix3x2(this Matrix4x4 m)
		=> new Matrix3x2(m.M11, m.M12, m.M21, m.M22, m.M41, m.M42);
}
