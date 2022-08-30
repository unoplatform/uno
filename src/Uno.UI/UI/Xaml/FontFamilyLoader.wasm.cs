#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Windows.Storage.Helpers;

namespace Windows.UI.Xaml.Media;

/// <summary>
/// WebAssembly-specific asynchronous font loader
/// </summary>
class FontFamilyLoader
{
	private static readonly Dictionary<FontFamily, FontFamilyLoader> _loaders = new(new FontFamilyComparer());
	private static readonly Dictionary<string, FontFamilyLoader> _loadersFromCssName = new();

	private readonly FontFamily _fontFamily;
	private string? _externalSource;
	private IList<ManagedWeakReference>? _waitingList;

	public string CssFontName { get; private set; }

	public bool IsLoaded { get; private set; }

	public bool IsLoading { get; private set; }

	/// <summary>
	/// Gets a loader for the specific <see cref="FontFamily"/>
	/// </summary>
	internal static FontFamilyLoader GetLoaderForFontFamily(FontFamily forFamily)
	{
		if (_loaders.TryGetValue(forFamily, out var loader))
		{
			// There is already a loader for this font family
			return loader;
		}

		loader = new FontFamilyLoader(forFamily);

		return loader;
	}

	private FontFamilyLoader(FontFamily fontFamily)
	{
		_fontFamily = fontFamily;

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Creating font loader for {fontFamily.Source}");
		}

		ParseSource(fontFamily.Source);

		_loaders.Add(fontFamily, this);
	}

	[MemberNotNull(nameof(CssFontName))]
	private void ParseSource(string source)
	{
		var sourceParts = source.Split(new[] { '#' }, 2, StringSplitOptions.RemoveEmptyEntries);

		if (sourceParts.Length > 0)
		{
			if (TryGetExternalUri(sourceParts[0], out var externalUri) && externalUri is { })
			{
				_externalSource = externalUri.OriginalString;
				CssFontName = "font" + _externalSource.GetHashCode();
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

			if (uri.IsAbsoluteUri && uri.Scheme is "ms-appx")
			{
				var assetUri = AssetsPathBuilder.BuildAssetUri(uri.PathAndQuery.TrimStart('/').ToString());
				uri = new Uri(assetUri, UriKind.RelativeOrAbsolute);
			}

			return true;
		}

		uri = default;
		return false;
	}

	/// <summary>
	/// Typescript-invoked method to notify that a font has been loaded properly
	/// </summary>
	internal static void NotifyFontLoaded(string cssFontName)
	{
		if (_loadersFromCssName.TryGetValue(cssFontName, out var loader))
		{
			_loadersFromCssName.Remove(cssFontName);

			if (typeof(FontFamilyLoader).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(FontFamilyLoader).Log().Debug($"Font sucessfully loaded: {loader._fontFamily.Source} ({_loadersFromCssName.Count} loaders active)");
			}

			loader.IsLoading = false;
			loader.IsLoaded = true;

			if (loader._waitingList is { Count: > 0 })
			{
				foreach (var waiting in loader._waitingList)
				{
					if (waiting.IsAlive && waiting.Target is UIElement ue)
					{
						ue.InvalidateMeasure();
					}
				}
			}

			loader._waitingList = null;
		}
		else
		{
			if (typeof(FontFamilyLoader).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(FontFamilyLoader).Log().Warn($"Unable to mark font [{cssFontName}] as loaded, the was already loaded. ({_loadersFromCssName.Count} loaders active)");
			}
		}
	}

	/// <summary>
	/// Typescript-invoked method to notify that a font failed to load properly
	/// </summary>
	internal static void NotifyFontLoadFailed(string cssFontName)
	{
		if (_loadersFromCssName.TryGetValue(cssFontName, out var loader))
		{
			_loadersFromCssName.Remove(cssFontName);

			if (typeof(FontFamilyLoader).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(FontFamilyLoader).Log().Warn($"Failed to load the font [{loader._fontFamily.Source}] could not be loaded. ({_loadersFromCssName.Count} loaders active)");
			}

			loader.IsLoading = false;
			loader.IsLoaded = true;

			if (loader._waitingList is { Count: > 0 })
			{
				foreach (var waiting in loader._waitingList)
				{
					if (waiting.IsAlive && waiting.Target is UIElement ue)
					{
						ue.InvalidateMeasure();
					}
				}
			}

			loader._waitingList = null;
		}
		else
		{
			if (typeof(FontFamilyLoader).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(FontFamilyLoader).Log().Warn($"Unable to mark font [{cssFontName}] as loaded, the was already loaded. ({_loadersFromCssName.Count} loaders active)");
			}
		}
	}

	/// <summary>
	/// Registers a <see cref="UIElement"/> for measure invalidation
	/// when a font has been loaded.
	/// </summary>
	internal void RegisterRemeasureOnFontLoaded(UIElement uiElement)
	{
		if (IsLoaded)
		{
			return; // already loaded: nothing to do
		}

		var weak = WeakReferencePool.RentSelfWeakReference(uiElement);

		(_waitingList ??= new List<ManagedWeakReference>()).Add(weak);

		if (IsLoading)
		{
			return; // already loading
		}

		LoadFontAsync();
	}

	internal void LoadFontAsync()
	{
		if (IsLoaded || IsLoading)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Font is already loaded: {_fontFamily.Source}");
			}

			return; // Already loaded
		}

		IsLoading = true;
		_loadersFromCssName.Add(CssFontName, this);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Loading font: {_fontFamily.Source} ({_fontFamily.CssFontName}/{_externalSource}, {_loadersFromCssName.Count} loaders active)");
		}

		if (_externalSource is { Length: > 0 })
		{
			WebAssemblyRuntime.InvokeJS($"Windows.UI.Xaml.Media.FontFamily.loadFont(\"{CssFontName}\",\"{_externalSource}\")");
		}
		else
		{
			WebAssemblyRuntime.InvokeJS($"Windows.UI.Xaml.Media.FontFamily.forceFontUsage(\"{CssFontName}\")");
		}
	}

	private class FontFamilyComparer : IEqualityComparer<FontFamily>
	{
		public bool Equals(FontFamily x, FontFamily y)
			=> string.Equals(x.Source, y.Source, StringComparison.OrdinalIgnoreCase);

		public int GetHashCode(FontFamily obj)
			=> obj.Source.GetHashCode();
	}
}
