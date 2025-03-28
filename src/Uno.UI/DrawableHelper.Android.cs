#nullable enable
using Android.Graphics.Drawables;
using System;
using System.ComponentModel;
using Windows.UI.Xaml.Media;

namespace Uno.UI
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static class DrawableHelper
	{
		/// <summary>
		/// Returns Drawable by URI.
		/// Forwards to <see cref="Uno.DrawableHelper"/>
		/// </summary>
		/// <param name="uri">URI</param>
		/// <returns>Drawable</returns>
		public static Drawable? FromUri(Uri uri) =>
			Uno.Helpers.DrawableHelper.FromUri(uri);
	}
}
