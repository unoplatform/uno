using Android.Graphics;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Text;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// Transform: Android part
	/// </summary>
	public partial class Transform
	{
		internal virtual Android.Graphics.Matrix ToNativeTransform(Android.Graphics.Matrix targetMatrix = null, Size size = new Size(), bool isBrush = false)
		{
			throw new NotImplementedException();
		}
	}
}
