using Android.Graphics;
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
		internal bool IsAnimating
		{
			get => _isAnimating;
			set
			{
				if (_isAnimating != value)
				{
					_isAnimating = value;
					NotifyChanged();
				}
			}
		}
	}
}
