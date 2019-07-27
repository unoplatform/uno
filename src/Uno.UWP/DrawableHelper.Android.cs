#if __ANDROID__
using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.UI
{
	public static class DrawableHelper
	{
		private static Dictionary<string, int> _drawablesLookup;
		private static Type _drawables;

		public static Drawable FromUri(Uri uri)
		{
			var id = FindResourceId(uri?.AbsoluteUri);
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
		
		public static Type Drawables
		{
			get => _drawables;
			set
			{				
				_drawables = value;
				InitializeDrawablesLookup();
			}
		}

		/// <summary>
		/// Returns the Id of the bundled image.
		/// </summary>
		/// <param name="imageName">Name of the image</param>
		/// <returns>Resource's id</returns>
		public static int? FindResourceId(string imageName)
		{
			var key = System.IO.Path.GetFileNameWithoutExtension(imageName);
			if (_drawablesLookup == null)
			{
				throw new Exception("You must initialize drawable resources by invoking this in your main Module (replace \"GenericApp\"):\nWindows.UI.Xaml.Media.ImageSource.Drawables = typeof(GenericApp.Resource.Drawable);");
			}
			var id = _drawablesLookup.UnoGetValueOrDefault(key, 0);
			if (id == 0)
			{
				if (typeof(DrawableHelper).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
				{
					typeof(DrawableHelper).Log().Error("Couldn't find drawable with key: " + key);
				}

				return null;
			}

			return id;
		}

		private static void InitializeDrawablesLookup()
		{
			_drawablesLookup = _drawables
				.GetFields(BindingFlags.Static | BindingFlags.Public)
				.ToDictionary(
					p => p.Name,
					p => (int)p.GetValue(null)
				);
		}
	}
}
#endif
