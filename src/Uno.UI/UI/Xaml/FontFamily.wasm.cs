#if __WASM__
#nullable enable

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Uno;

namespace Windows.UI.Xaml.Media
{
	public partial class FontFamily
	{
		private FontFamilyLoader _loader;

		partial void Init(string fontName)
			=> _loader = FontFamilyLoader.GetLoaderForFontFamily(this);

		/// <summary>
		/// Contains the font-face name to use in CSS.
		/// </summary>
		internal string CssFontName
			=> _loader.CssFontName;

		/// <summary>
		/// Use this to launch the loading of a font before it is actually required to
		/// minimize loading time and prevent potential flicking.
		/// </summary>
		/// <returns>True is the font loaded successfuly, otherwise false.</returns>
		internal static Task<bool> Preload(FontFamily family)
			=> family._loader.LoadFontAsync();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void RegisterForInvalidateMeasureOnFontLoaded(UIElement uiElement)
		{
			_loader.RegisterRemeasureOnFontLoaded(uiElement);
		}
	}
}
#endif
