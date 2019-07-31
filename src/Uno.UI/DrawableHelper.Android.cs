using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using System;
using Windows.UI.Xaml.Media;

namespace Uno.UI
{
	public static class DrawableHelper
	{
		/// <summary>
		/// Returns Drawable by URI.
		/// Forwards to <see cref="Uno.DrawableHelper"/>
		/// </summary>
		/// <param name="uri">URI</param>
		/// <returns>Drawable</returns>
		public static Drawable FromUri(Uri uri) =>
			Uno.DrawableHelper.FromUri(uri);
	}
}
