#nullable enable

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Uno;
using Windows.Storage.Helpers;

namespace Windows.UI.Xaml.Media
{
	public partial class FontFamily
	{
		private FontFamilyLoader _loader;


		/// <summary>
		/// Contains the font-face name to use in CSS.
		/// </summary>
		internal string CssFontName { get; private set; }

		internal string? ExternalSource { get; private set; }

		partial void Init(string fontName)
		{
			ParseSource(Source);
			_loader = FontFamilyLoader.GetLoaderForFontFamily(this);
		}

		[MemberNotNull(nameof(CssFontName))]
		private void ParseSource(string source)
		{
			var sourceParts = source.Split('#', 2, StringSplitOptions.RemoveEmptyEntries);

			if (sourceParts.Length > 0)
			{
				if (TryGetExternalUri(sourceParts[0], out var externalUri) && externalUri is { })
				{
					ExternalSource = externalUri.OriginalString;
					CssFontName = "font" + ExternalSource.GetHashCode();
				}
				else
				{
					CssFontName = sourceParts[sourceParts.Length == 2 ? 1 : 0];
				}
			}
			else
			{
				throw new InvalidOperationException("FontFamily source cannot be empty");
			}
		}

		private static bool TryGetExternalUri(string? source, out Uri? uri)
		{
			if (source is not null && (source.IndexOf('.') > -1 || source.IndexOf('/') > -1))
			{
				uri = new Uri(source, UriKind.RelativeOrAbsolute);

				if (!uri.IsAbsoluteUri || source.StartsWith('/'))
				{
					// Support for implicit ms-appx resolution
					var assetUri = AssetsPathBuilder.BuildAssetUri(Uri.EscapeDataString(source.TrimStart('/')).Replace("%2F", "/"));
					uri = new Uri(assetUri, UriKind.RelativeOrAbsolute);
				}

				if (Uno.UI.Xaml.XamlFilePathHelper.TryGetMsAppxAssetPath(uri, out var path))
				{
					var assetUri = AssetsPathBuilder.BuildAssetUri(path);
					uri = new Uri(assetUri, UriKind.RelativeOrAbsolute);
				}

				return true;
			}

			uri = default;
			return false;
		}

		/// <summary>
		/// Use this to launch the loading of a font before it is actually required to
		/// minimize loading time and prevent potential flicking.
		/// </summary>
		/// <returns>True is the font loaded successfuly, otherwise false.</returns>
		internal static Task<bool> PreloadAsync(FontFamily family)
			=> family._loader.LoadFontAsync();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void RegisterForInvalidateMeasureOnFontLoaded(UIElement uiElement)
		{
			_loader.RegisterRemeasureOnFontLoaded(uiElement);
		}
	}
}
