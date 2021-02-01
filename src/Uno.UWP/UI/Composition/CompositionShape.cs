#nullable enable

using System;
using System.Numerics;

namespace Windows.UI.Composition
{
	public partial class CompositionShape : global::Windows.UI.Composition.CompositionObject
	{
		internal CompositionShape() => throw new InvalidOperationException($"Use the Compositor ctor");

		internal CompositionShape(Compositor compositor) : base(compositor)
		{
		}

		public Matrix3x2 TransformMatrix { get; set; } = Matrix3x2.Identity;

		public Vector2 Scale { get; set; } = new Vector2(1, 1);

		public float RotationAngleInDegrees { get; set; }

		public float RotationAngle { get; set; }

		public Vector2 Offset { get; set; }

		public Vector2 CenterPoint { get; set; }
	}
}
