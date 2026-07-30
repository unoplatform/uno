#nullable enable

using SkiaSharp;
using System;
using System.Numerics;
using Windows.Foundation;
using Uno.Extensions;
using Uno.UI.Composition;

namespace Microsoft.UI.Composition;

public partial class CompositionShape
{
	private Matrix3x2 _combinedTransformMatrix = Matrix3x2.Identity;

	private protected Matrix3x2 CombinedTransformMatrix
	{
		get => _combinedTransformMatrix;
		private set => SetProperty(ref _combinedTransformMatrix, value);
	}

	private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
	{
		base.OnPropertyChangedCore(propertyName, isSubPropertyChange);

		switch (propertyName)
		{
			case nameof(TransformMatrix) or nameof(Scale) or nameof(RotationAngle) or nameof(CenterPoint):
				var transform = TransformMatrix;

				if (Scale != Vector2.One)
				{
					transform *= Matrix3x2.CreateScale(Scale, CenterPoint);
				}

				if (RotationAngle is not 0)
				{
					transform *= Matrix3x2.CreateRotation(RotationAngle, CenterPoint);
				}

				CombinedTransformMatrix = transform;
				break;
		}
	}

	internal virtual void Render(in Visual.PaintingSession session)
	{
		var offset = Offset;
		var transform = CombinedTransformMatrix;
		var hasOffset = offset != Vector2.Zero;
		var hasTransform = !transform.IsIdentity;

		if (hasOffset || hasTransform)
		{
			session.Canvas.Save();

			if (hasOffset)
			{
				session.Canvas.Translate(offset.X, offset.Y);
			}

			if (hasTransform)
			{
				session.Canvas.Concat(transform.ToSKMatrix());
			}
		}

		Paint(in session);

		if (hasOffset || hasTransform)
		{
			session.Canvas.Restore();
		}
	}

	internal virtual void Paint(in Visual.PaintingSession session)
	{
	}

	internal virtual bool CanPaint() => false;

	internal virtual bool HitTest(Point point) => false;
}
