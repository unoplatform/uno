
using System.Reflection;
using Uno.Extensions;
#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno
{
	public static class DrawablesHelper
	{
		private static Type _drawables;
		private static Dictionary<string, int> _drawablesLookup;

		public static Type Drawables
		{
			get
			{
				return _drawables;
			}
			set
			{
				_drawables = value;
				Initialize();
			}
		}
		private static void Initialize()
		{
			_drawablesLookup = _drawables
				.GetFields(BindingFlags.Static | BindingFlags.Public)
				.ToDictionary(
					p => p.Name,
					p => (int)p.GetValue(null)
				);
		}


		public static int GetResourceId(string imageName)
		{
			var key = global::System.IO.Path.GetFileNameWithoutExtension(imageName);
			if (_drawablesLookup == null)
			{
				throw new global::System.InvalidOperationException("You must initialize drawable resources by invoking this in your main Module (replace \"GenericApp\"):\nUno.DrawablesHelper.Drawables = typeof(GenericApp.Resource.Drawable);");
			}
			var id = _drawablesLookup.UnoGetValueOrDefault(key, 0);
			if (id == 0)
			{
				throw new KeyNotFoundException("Couldn't find drawable with key: " + key);
			}
			return id;
		}
	}
}
#endif
