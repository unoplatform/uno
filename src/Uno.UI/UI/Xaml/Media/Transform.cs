using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;
using Uno.Extensions;

#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#elif __WASM__
using _View = Windows.UI.Xaml.UIElement;
#else
using _View = System.Object;
#endif

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// Transform :  Based on the WinRT Transform
	/// 
	/// https://msdn.microsoft.com/en-us/library/system.windows.media.transform(v=vs.110).aspx
	/// </summary>
	public abstract partial class Transform : GeneralTransform
	{
		protected static PropertyChangedCallback NotifyChangedCallback { get; } = (snd, args) =>
		{
			// Don't update the internal value if the value is being animated.
			// The value is being animated by the platform itself.

			if (snd is Transform transform
				&& args.NewPrecedence != DependencyPropertyValuePrecedences.Animations
				&& !args.BypassesPropagation)
			{
				transform.MatrixCore = transform.ToMatrix(new Point(0, 0));
				transform.NotifyChanged();
			}
		};

		/// <summary>
		/// Notifies that a value of this transform changed (usually this means that the <see cref="Matrix"/> has been updated).
		/// </summary>
		internal event EventHandler Changed;

		protected void NotifyChanged()
			=> Changed?.Invoke(this, EventArgs.Empty);

		/// <summary>
		/// The matrix used by this transformation
		/// </summary>
		internal Matrix3x2 MatrixCore { get; private set; } = Matrix3x2.Identity;

		/// <summary>
		/// Converts the transform to a standard transform matrix
		/// </summary>
		/// <param name="relativeOrigin">The origin of the transform relative to the <paramref name="viewSize"/>.</param>
		/// <param name="viewSize">The size of the view to transform, in virtual pixels</param>
		/// <returns>An affine matrix of the transformation</returns>
		internal Matrix3x2 ToMatrix(Point relativeOrigin, Size viewSize)
			=> ToMatrix(new Point(relativeOrigin.X * viewSize.Width, relativeOrigin.Y * viewSize.Height));

		/// <summary>
		/// Converts the transform to a standard transform matrix
		/// </summary>
		/// <param name="absoluteOrigin">The absolute origin of the transform, in virtual pixels.</param>
		/// <returns>An affine matrix of the transformation</returns>
		internal abstract Matrix3x2 ToMatrix(Point absoluteOrigin);

		// Currently we support only one view par transform.
		// But we can declare a Transform as a static resource and use it on multiple views.
		// Note: This is now used only for animations
		internal _View View { get; set; }

		#region GeneralTransform overrides
		/// <inheritdoc />
		protected override GeneralTransform InverseCore
		{
			get
			{
				var matrix = MatrixCore;
				if (matrix.IsIdentity)
				{
					return this;
				}
				else
				{
					// The Inverse transform is not expected to reflect future changes on this transform
					// It means that it's  acceptable to capture the current 'Matrix'
					Matrix3x2.Invert(matrix, out var inverse);
					return new MatrixTransform
					{
						Matrix = new Matrix(inverse)
					};
				}
			}
		}

		/// <inheritdoc />
		protected override bool TryTransformCore(Point inPoint, out Point outPoint)
		{
			var matrix = MatrixCore;
			if (matrix.IsIdentity)
			{
				outPoint = inPoint;
				return false;
			}
			else
			{
				outPoint = new Point
				(
					(inPoint.X * matrix.M11) + (inPoint.Y * matrix.M21) + matrix.M31,
					(inPoint.X * matrix.M12) + (inPoint.Y * matrix.M22) + matrix.M32
				);
				return true;
			}
		}

		/// <inheritdoc />
		protected override Rect TransformBoundsCore(Rect rect)
		{
			return rect.Transform(MatrixCore);
		}
		#endregion
	}
}


