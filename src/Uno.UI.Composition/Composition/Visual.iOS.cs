#nullable enable

using CoreAnimation;
using System.Numerics;
using UIKit;
using System;
using CoreGraphics;
using ObjCRuntime;

namespace Windows.UI.Composition
{
	public partial class Visual : global::Windows.UI.Composition.CompositionObject
	{
		internal UIView? NativeOwner { get; set; }

		internal CALayer NativeLayer { get; } = new CALayer();

		partial void InitializePartial()
		{
			NativeLayer.MasksToBounds = false;
		}

		partial void OnCenterPointChanged(Vector3 value)
		{
			Update();
		}

		private void Update()
		{
			NativeLayer.Frame = new CoreGraphics.CGRect(0, 0, _size.X, _size.Y);

			// Anchor point is in the middle of the view - this code moves it around
			var centerX = _centerPoint.X / NativeLayer.Frame.Size.Width;
			var centerY = _centerPoint.Y / NativeLayer.Frame.Size.Height;

			NativeLayer.Position = new CGPoint(_centerPoint.X + NativeLayer.Frame.X, _centerPoint.Y + NativeLayer.Frame.Y);

			// relative to width and height, if it is not Zero.
			NativeLayer.AnchorPoint = new CGPoint(
				nfloat.IsNaN(centerX) ? 0 : centerX,
				nfloat.IsNaN(centerY) ? 0 : centerY
			);
		}

		partial void OnOffsetChanged(Vector3 value)
		{
			UpdateTransform();
		}

		private void UpdateTransform()
		{
			var matrix = CATransform3D.MakeTranslation((nfloat)_offset.X, (nfloat)_offset.Y, (nfloat)_offset.Z);
			matrix = matrix.Rotate((nfloat)ToRadians(RotationAngleInDegrees), _rotationAxis.X, _rotationAxis.Y, _rotationAxis.Z);

			CATransaction.Begin();
			CATransaction.DisableActions = true;

			NativeLayer.Transform = matrix;

			CATransaction.Commit();
		}

		partial void OnScaleChanged(Vector3 value)
		{

		}

		partial void OnSizeChanged(Vector2 value)
		{
			Update();
		}

		partial void OnRotationAngleChanged(float value)
		{
			UpdateTransform();
		}

		partial void OnRotationAxisChanged(Vector3 value)
		{
			Update();
		}

		internal static double ToRadians(double angle)
		{
			return (Math.PI / 180.0) * angle;
		}
	}
}
