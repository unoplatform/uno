#nullable enable

using System.Numerics;
using Uno.Extensions;
using Uno.UI.Composition;
using System;
using System.Linq;
using SkiaSharp;
using Windows.Foundation;

namespace Microsoft.UI.Composition;

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

	/// <summary>
	/// Returns the bounds of the clip. The clip itself could be non-rectangular, e.g, rounded rectangle or path.
	/// Note that this already handles TransformMatrix
	/// </summary>
	internal Rect? GetBounds(Visual visual)
	{
		if (GetBoundsCore(visual) is { } bounds)
		{
			return TransformMatrix.Transform(bounds);
		}

		return null;
	}

	/// <summary>
	/// Returns the bounds of the clip. The clip itself could be non-rectangular, e.g, rounded rectangle or path.
	/// Note that implementors should not handle TransformMatrix. The result is already transformed by <see cref="GetBounds"/>.
	/// </summary>
	private protected virtual Rect? GetBoundsCore(Visual visual)
		=> null;

	internal virtual SKPath? GetClipPath(Visual visual) => null;
	/// <summary>
	/// Optionally overridable if the clip path can be provided as a rounded rect.
	/// </summary>
	private protected virtual SKRoundRect? GetClipRoundedRect(Visual visual) => null;
	/// <summary>
	/// Optionally overridable if the clip path can be provided as a rect.
	/// </summary>
	private protected virtual SKRect? GetClipRect(Visual visual) => null;

	internal void ApplyClip(Visual visual, SKCanvas canvas)
	{
		if (GetClipRect(visual) is { } clipRect)
		{
			canvas.ClipRect(clipRect, antialias: true);
		}
		else if (GetClipRoundedRect(visual) is { } roundedRect)
		{
			canvas.ClipRoundRect(roundedRect, antialias: true);
		}
		else if (GetClipPath(visual) is { } clipPath)
		{
			canvas.ClipPath(clipPath, antialias: true);
		}
	}
}
