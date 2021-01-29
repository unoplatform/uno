#if __ANDROID__
using Android.Graphics.Drawables;
using AndroidX.Core.Content;
using AndroidX.Core.Graphics.Drawable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;

namespace Uno.Helpers
{
	public static class DrawableHelper
	{
		public static Dictionary<string, int> DrawableMap { get; set; }

		private static Type _drawables;
		public static Type Drawables
		{
			get => _drawables;
			set
			{
				_drawables = value;
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
			return GetResourceId(key);
		}


		/// <summary>
		/// Returns the Id of the bundled image.
		/// </summary>
		/// <param name="imagePath">Path of the image</param>
		/// <returns>Resource's id</returns>
		internal static int? FindResourceIdFromPath(string imagePath)
		{
			var key = System.IO.Path.GetFileNameWithoutExtension(AndroidResourceNameEncoder.EncodeDrawablePath(imagePath));
			return GetResourceId(key);
		}

		/// <summary>
		/// Finds a Drawable by URI
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <returns>Drawable</returns>
		public static Drawable FromUri(Uri uri)
		{
			var id = FindResourceId(uri?.AbsoluteUri);
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

		private static int? GetResourceId(string resourceKey)
		{
			if (Drawables == null)
			{
				throw new InvalidOperationException("Drawable resources were not initialized. "
					+ "On Android, local assets are only available after App.InitializeComponent() has been called.");
			}

			var id = DrawableMap.UnoGetValueOrDefault(resourceKey);

			if (id == 0)
			{
				if (typeof(DrawableHelper).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
				{
					typeof(DrawableHelper).Log().Error($"Couldn't find drawable with key: {resourceKey}");
				}

				Console.WriteLine($"STEVE : Couldn't find drawable with key: {resourceKey}");
				foreach (var key in DrawableMap.Keys)
				{
					Console.WriteLine($"STEVE : {key}");
				}


				return null;
			}

			return id;
		}
	}
}
#endif
