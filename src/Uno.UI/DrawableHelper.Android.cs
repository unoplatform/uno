using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Xaml.Media;

namespace Uno.UI
{
    public static class DrawableHelper
    {
		public static Drawable FromUri(Uri uri)
		{
			var id = ImageSource.FindResourceId(uri?.AbsoluteUri);
			var drawable = id.HasValue
				? ContextCompat.GetDrawable(ContextHelper.Current, id.Value)
				: null;

			if (drawable != null)
			{
				// Makes the drawable compatible with DrawableCompat pre-Lollipop.
				drawable = Android.Support.V4.Graphics.Drawable.DrawableCompat.Wrap(drawable);
				drawable = drawable.Mutate();
			}

			return drawable;
		}
	}
}
