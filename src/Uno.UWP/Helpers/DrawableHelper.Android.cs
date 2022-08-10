#if __ANDROID__
using Android.Graphics.Drawables;
using AndroidX.Core.Content;
using AndroidX.Core.Graphics.Drawable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;

namespace Uno.Helpers
{
	public static class DrawableHelper
	{
		private static Dictionary<string, int> _drawablesLookup;
		private static Type _drawables;

		private static Func<string, int> _resolver;

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
		[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public static int? FindResourceId(string imageName)
		{
			var key = AndroidResourceNameEncoder.Encode(System.IO.Path.GetFileNameWithoutExtension(imageName));

			int id;

			if (_resolver != null)
			{
				id = _resolver(key);
			}
			else
			{
				if (_drawablesLookup == null)
				{
					throw new InvalidOperationException("Drawable resources were not initialized. "
						+ "On Android, local assets are only available after App.InitializeComponent() has been called.");
				}

				id = _drawablesLookup.UnoGetValueOrDefault(key, 0);
			}

			if (id == 0)
			{
				if (typeof(DrawableHelper).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					typeof(DrawableHelper).Log().Error("Couldn't find drawable with key: " + key);
				}

				return null;
			}

			return id;
		}

		/// <summary>
		/// Returns the Id of the bundled image.
		/// </summary>
		/// <param name="imagePath">Path of the image</param>
		/// <returns>Resource's id</returns>
		internal static int? FindResourceIdFromPath(string imagePath)
		{
			var key = System.IO.Path.GetFileNameWithoutExtension(AndroidResourceNameEncoder.EncodeDrawablePath(imagePath));
			return FindResourceId(key);
		}

		/// <summary>
		/// Finds a Drawable by URI
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <returns>Drawable</returns>
		public static Drawable FromUri(Uri uri)
		{
			var id = FindResourceIdFromPath(uri.PathAndQuery.TrimStart(new[] { '/' }));
			var drawable = id.HasValue
				? ContextCompat.GetDrawable(ContextHelper.Current, id.Value)
				: null;

			if (drawable != null)
			{
				// Makes the drawable compatible with DrawableCompat pre-Lollipop.
				drawable = DrawableCompat.Wrap(drawable);
				drawable = drawable.Mutate();
			}

			return drawable;
		}

		/// <summary>
		/// Sets a function to resolve drawable Ids from their string equivalent, used internally by Uno.
		/// </summary>
		public static void SetDrawableResolver(Func<string, int> resolver) => _resolver = resolver;

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
