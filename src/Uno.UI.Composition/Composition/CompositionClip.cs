#nullable enable

using System.Numerics;
using Uno.Extensions;
using Uno.UI.Composition;

namespace Windows.UI.Composition;

public partial class CompositionClip : CompositionObject, I2DTransformableObject
{
	private Matrix3x2 _transformMatrix = Matrix3x2.Identity;
	private Vector2 _scale = new Vector2(1, 1);
	private float _rotationAngle;
	private Vector2 _offset = Vector2.Zero;
	private Vector2 _centerPoint = Vector2.Zero;
	private Vector2 _anchorPoint = Vector2.Zero;

	internal CompositionClip(Compositor compositor) : base(compositor)
	{

	}

	public Matrix3x2 TransformMatrix
	{
		get => _transformMatrix;
		set => SetProperty(ref _transformMatrix, value);
	}

	public Vector2 Scale
	{
		get => _scale;
		set => SetProperty(ref _scale, value);
	}

	public float RotationAngleInDegrees
	{
		get => (float)MathEx.ToDegree(_rotationAngle);
		set => RotationAngle = (float)MathEx.ToRadians(value);
	}

	public float RotationAngle
	{
		get => _rotationAngle;
		set => SetProperty(ref _rotationAngle, value);
	}

	public Vector2 Offset
	{
		get => _offset;
		set => SetProperty(ref _offset, value);
	}

	public Vector2 CenterPoint
	{
		get => _centerPoint;
		set => SetProperty(ref _centerPoint, value);
	}

	public Vector2 AnchorPoint
	{
		get => _anchorPoint;
		set => SetProperty(ref _anchorPoint, value);
	}
}
