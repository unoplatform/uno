using System;
using System.Numerics;
using Windows.Foundation;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// Transform: Android part
	/// </summary>
	public partial class Transform
	{
		private bool _isAnimating;

		internal virtual Android.Graphics.Matrix ToNative(Android.Graphics.Matrix targetMatrix = null, Size size = new Size(), bool isBrush = false)
		{
			throw new NotImplementedException();
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
