/*

Implementation based on https://github.com/roubachof/Sharpnado.MaterialFrame.
with some modifications and removal of unused features.

*/

using System;

namespace Uno.UI.Xaml.Media
{
    internal static class JniExtensions
    {
		public static bool IsNullOrDisposed(this Java.Lang.Object javaObject) =>
			javaObject == null || javaObject.Handle == IntPtr.Zero;
	}
}
