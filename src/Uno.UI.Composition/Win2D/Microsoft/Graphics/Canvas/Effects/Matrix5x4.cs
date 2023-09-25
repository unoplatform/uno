#nullable enable

namespace Microsoft.Graphics.Canvas.Effects
{
	public struct Matrix5x4
	{
		public float M11;
		public float M12;
		public float M13;
		public float M14;
		public float M21;
		public float M22;
		public float M23;
		public float M24;
		public float M31;
		public float M32;
		public float M33;
		public float M34;
		public float M41;
		public float M42;
		public float M43;
		public float M44;
		public float M51;
		public float M52;
		public float M53;
		public float M54;

		public static Matrix5x4 Identity => new Matrix5x4()
		{
			M11 = 1,
			M12 = 0,
			M13 = 0,
			M14 = 0,
			M21 = 0,
			M22 = 1,
			M23 = 0,
			M24 = 0,
			M31 = 0,
			M32 = 0,
			M33 = 1,
			M34 = 0,
			M41 = 0,
			M42 = 0,
			M43 = 0,
			M44 = 1,
			M51 = 0,
			M52 = 0,
			M53 = 0,
			M54 = 0
		};

		internal float[] ToArray()
		{
			return new float[20]
			{
					M11, M12, M13, M14,
					M21, M22, M23, M24,
					M31, M32, M33, M34,
					M41, M42, M43, M44,
					M51, M52, M53, M54
			};
		}

		public override bool Equals(object? obj) => base.Equals(obj);

		public override int GetHashCode() => base.GetHashCode();

		public static bool operator ==(Matrix5x4 left, Matrix5x4 right) => left.Equals(right);

		public static bool operator !=(Matrix5x4 left, Matrix5x4 right) => !(left == right);
	}
}
