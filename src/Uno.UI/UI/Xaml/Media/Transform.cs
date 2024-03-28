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
#endif

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// Transform :  Based on the WinRT Transform
	///
	/// https://msdn.microsoft.com/en-us/library/system.windows.media.transform(v=vs.110).aspx
	/// </summary>
	// Transform isn't supposed to be abstract, but since the constructor is private protected, it
	// cannot be subclassed outside of Uno. So it doesn't matter much whether it's abstract or not.
	// We keep it abstract for now to force subclasses to implement the abstract method ToMatrix.
	public abstract partial class Transform : GeneralTransform
	{
		private protected Transform()
		{
		}

		protected static PropertyChangedCallback NotifyChangedCallback { get; } = (snd, args) =>
		{
			if (snd is Transform transform)
			{
				transform.NotifyChanged();
			}
		};

		/// <summary>
		/// Notifies that a value of this transform changed (usually this means that the <see cref="MatrixCore"/> has been updated).
		/// </summary>
		internal event EventHandler Changed;

		protected void NotifyChanged()
		{
#if __ANDROID__ || __IOS__ || __MACOS__ // On WASM currently we supports only CPU bound animations, so we have to let the transform be updated on each frame
			if (IsAnimating)
			{
				// Don't update the internal value if the value is being animated.
				// The value is expected to be animated by the platform itself.

				return;
			}
#endif

			MatrixCore = ToMatrix(new Point(0, 0));
			Changed?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// The matrix used by this transformation
		/// </summary>
		/// <remarks>This matrix does not include any origin point (i.e. equivalent to `.ToMatrix(default(Point))`)</remarks>
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

#if __ANDROID__ || __IOS__ || __MACOS__
		// Currently we support only one view par transform.
		// But we can declare a Transform as a static resource and use it on multiple views.
		// Note: This is now used only for animations
		internal virtual _View View { get; set; }
#endif

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
					// It means that it's acceptable to capture the current 'Matrix'
					return new MatrixTransform
					{
						Matrix = new Matrix(matrix.Inverse())
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
				outPoint = matrix.Transform(inPoint);
				return true;
			}
		}

		/// <inheritdoc />
		protected override Rect TransformBoundsCore(Rect rect)
		{
			return MatrixCore.Transform(rect);
		}
		#endregion
	}
}


