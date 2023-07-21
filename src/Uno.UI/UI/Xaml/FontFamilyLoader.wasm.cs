#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Windows.Storage.Helpers;

using NativeMethods = __Windows.UI.Xaml.Media.FontFamilyLoader.NativeMethods;

namespace Windows.UI.Xaml.Media;

/// <summary>
/// WebAssembly-specific asynchronous font loader
/// </summary>
internal partial class FontFamilyLoader
{
	private static readonly Dictionary<FontFamily, FontFamilyLoader> _loaders = new(new FontFamilyComparer());
	private static readonly Dictionary<string, FontFamilyLoader> _loadersFromCssName = new();

	private readonly FontFamily _fontFamily;
	private IList<ManagedWeakReference>? _waitingList;
	private TaskCompletionSource<bool>? _loadOperation;

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

		_loaders.Add(fontFamily, this);
	}

	/// <summary>
	/// Typescript-invoked method to notify that a font has been loaded properly
	/// </summary>
	[JSExport]
	internal static void NotifyFontLoaded(string cssFontName)
	{
		if (_loadersFromCssName.TryGetValue(cssFontName, out var loader))
		{
			_loadersFromCssName.Remove(cssFontName);

			if (typeof(FontFamilyLoader).Log().IsEnabled(LogLevel.Debug))
			{
				typeof(FontFamilyLoader).Log().Debug($"Font sucessfully loaded: {loader._fontFamily.Source} ({_loadersFromCssName.Count} loaders active)");
			}

			loader.SetLoadCompleted(true);

			if (loader._waitingList is { Count: > 0 })
			{
				Controls.TextBlockMeasureCache.Instance.Clear(loader._fontFamily);

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
	[JSExport]
	internal static void NotifyFontLoadFailed(string cssFontName)
	{
		if (_loadersFromCssName.TryGetValue(cssFontName, out var loader))
		{
			_loadersFromCssName.Remove(cssFontName);

			if (typeof(FontFamilyLoader).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(FontFamilyLoader).Log().Warn($"Failed to load the font [{loader._fontFamily.Source}] could not be loaded. ({_loadersFromCssName.Count} loaders active)");
			}

			loader.SetLoadCompleted(false);

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

	private void SetLoadCompleted(bool result)
	{
		IsLoading = false;
		IsLoaded = true;
		_loadOperation?.TrySetResult(result);
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

		_ = LoadFontAsync();
	}

	/// <summary>
	/// Loads a font asynchronously
	/// </summary>
	/// <returns>A task indicating if the font loaded sucessfuly</returns>
	internal async Task<bool> LoadFontAsync()
	{
		try
		{
			if (IsLoaded || IsLoading)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Font is already loaded: {_fontFamily.Source}");
				}

				return _loadOperation != null
					? await _loadOperation.Task
					: true; // Already loaded
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Loading font: {_fontFamily.Source} ({_fontFamily.CssFontName}/{_fontFamily.ExternalSource}, {_loadersFromCssName.Count} loaders active)");
			}

			IsLoading = true;
			_loadersFromCssName.Add(_fontFamily.CssFontName, this);

			if (_fontFamily.ExternalSource is { Length: > 0 })
			{
				NativeMethods.LoadFont(_fontFamily.CssFontName, _fontFamily.ExternalSource);
			}
			else
			{
				NativeMethods.ForceFontUsage(_fontFamily.CssFontName);
			}

			_loadOperation = new TaskCompletionSource<bool>();
			return await _loadOperation.Task;
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed loading font: {_fontFamily.Source} ({_fontFamily.CssFontName}/{_fontFamily.ExternalSource}, {_loadersFromCssName.Count} loaders active)", e);
			}

			NotifyFontLoadFailed(_fontFamily.CssFontName);

			return false;
		}
	}

	private class FontFamilyComparer : IEqualityComparer<FontFamily>
	{
		public bool Equals(FontFamily? x, FontFamily? y)
			=> string.Equals(x!.CssFontName, y!.CssFontName, StringComparison.OrdinalIgnoreCase);

		public int GetHashCode(FontFamily obj)
			=> obj.CssFontName.GetHashCode();
	}
}
