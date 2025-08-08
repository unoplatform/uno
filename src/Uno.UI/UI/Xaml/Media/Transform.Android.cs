using System;
using System.Numerics;
using Uno.UI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Media
{
	/// <summary>
	/// Transform: Android part
	/// </summary>
	public partial class Transform
	{
		private bool _isAnimating;

		internal AMatrix ToNativeMatrix(AMatrix targetMatrix = null, Point origin = new Point(), Size size = new Size())
		{
			var matrix = ToMatrix(origin, size);

			if (targetMatrix == null)
			{
				targetMatrix = new AMatrix();
			}

			targetMatrix.SetValues(new[]
			{
				matrix.M11, matrix.M21, matrix.M31 * (float)size.Width,
				matrix.M12, matrix.M22, matrix.M32 * (float)size.Height,
				0, 0, 1
			});

			return targetMatrix;
		}

		// Currently we support only one view par transform.
		// But we can declare a Transform as a static resource and use it on multiple views.
		// Note: This is now used only for animations
		internal virtual bool IsAnimating
		{
			get => _isAnimating;
			set
			{
				if (_isAnimating != value)
				{
					_isAnimating = value;

					if (_isAnimating)
					{
						// We don't use the NotifyChanged() since it filters out change notifications when IsAnimating.
						// Note: we also bypass the MatrixCore update which is actually irrelevant until animation completes.
						Changed?.Invoke(this, EventArgs.Empty);
					}
					else
					{
						// Notify a change so the result matrix will be updated (as updates were ignored due to 'Animations' DP precedence),
						// and the NativeRenderTransformAdapter will then apply this final matrix.
						NotifyChanged();
					}
				}
			}
		}
	}
}
