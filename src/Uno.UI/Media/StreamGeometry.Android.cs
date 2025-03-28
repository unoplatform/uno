using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Android.Graphics;

namespace Uno.Media
{
	public sealed partial class StreamGeometry : Geometry
	{
		private readonly static bool CanUseFastPathCopy = Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.JellyBeanMr1;

		public override Path ToPath()
		{
			// We need to return a new instance because the calling site
			// could apply transformations on it.

			if (CanUseFastPathCopy)
			{
				var path = new Path(bezierPath);
				path.SetFillType(FillRule == FillRule.EvenOdd ? Path.FillType.EvenOdd : Path.FillType.Winding);
				return path;
			}
			else
			{
				// On android 4.1.x and below, the Path ctor that takes a another path does not 
				// copy the original path properly, whereas the "set" method does. We use it only 
				// for API 16 and below to avoid calling two methods for a Path cloning.
				//
				// This issue is most probably located in the Skia 2D C++ rendering library, which 
				// is used by Android.
				var newPath = new Path();
				newPath.Set(bezierPath);
				newPath.SetFillType(FillRule == FillRule.EvenOdd ? Path.FillType.EvenOdd : Path.FillType.Winding);
				return newPath;
			}
		}
	}
}
