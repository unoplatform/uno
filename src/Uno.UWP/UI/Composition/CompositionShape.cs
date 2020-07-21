using System.Numerics;

namespace Windows.UI.Composition
{
	public  partial class CompositionShape : global::Windows.UI.Composition.CompositionObject
	{
		public Matrix3x2 TransformMatrix { get; set; }

		public  Vector2 Scale { get; set; }

		public  float RotationAngleInDegrees { get; set; }

		public  float RotationAngle { get; set; }

		public  global::System.Numerics.Vector2 Offset { get; set; }

		public  global::System.Numerics.Vector2 CenterPoint { get; set; }
	}
}
