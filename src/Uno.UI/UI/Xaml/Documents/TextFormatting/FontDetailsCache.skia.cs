#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.ComponentModel;
using System.Linq;
using Uno;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Xaml.Media;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

internal static class ApplicationTypeFontLoader
{
	static Dictionary<Uri, Task<StorageFile>> _activeLookups = new();
	static object _activeLookupsGate = new();

	public static event Action<Uri>? FontUpdated;

	public static SKTypeface? FindFont(Uri uri)
	{
		if (XamlFilePathHelper.TryGetMsAppxAssetPath(uri, out var path))
		{
			Console.WriteLine($"Try loading font {uri}");

			var platformPartialPath = path.Replace('/', global::System.IO.Path.DirectorySeparatorChar);

			var localFilePath = global::System.IO.Path.Combine(
				global::Windows.ApplicationModel.Package.Current.InstalledLocation.Path
				, platformPartialPath);

			if (File.Exists(localFilePath))
			{
				Console.WriteLine($"Using Font local file {localFilePath}");

				// SKTypeface.FromFile may return null if the file is not found (SkiaSharp is not yet nullable attributed)
				return SKTypeface.FromFile(localFilePath);
			}

			lock (_activeLookupsGate)
			{
				if (!_activeLookups.TryGetValue(uri, out var lookupTask))
				{
					Console.WriteLine($"Initiate reading Font app file {uri}");

					_activeLookups[uri] = lookupTask = GetAppFileAsync(uri);
				}

				if (!lookupTask.IsCompleted)
				{
					Console.WriteLine($"Font App File reading is not available {uri}");

					return null;
				}

				Console.WriteLine($"Font App File {lookupTask.Result.Path}");

				if (SKTypeface.FromFile(lookupTask.Result.Path) is { } typeFace)
				{
					Console.WriteLine($"Loaded Font App File {lookupTask.Result.Path}");
					return typeFace;
				}
			}
		}

		return null;
	}

	private static async Task<StorageFile> GetAppFileAsync(Uri uri)
	{
		var file = await StorageFile.GetFileFromApplicationUriAsync(uri);

		Console.WriteLine($"Font File read {file.Path}");

		FontUpdated?.Invoke(uri);

		return file;
	}
}

internal static class FontDetailsCache
{
	private record struct FontCacheEntry(
		string? Name,
		float FontSize,
		FontWeight Weight,
		FontStretch Stretch,
		FontStyle Style);

	private static Dictionary<FontCacheEntry, FontDetails> _fontCache = new();
	private static object _fontCacheGate = new();

	internal static void OnFontLoaded(string font, SKTypeface? typeface)
	{
		lock (_fontCacheGate)
		{
			foreach (var (key, details) in _fontCache)
			{
				if (key.Name == font)
				{
					if (typeface is null)
					{
						// font load failed.
						details.LoadFailed();
					}
					else
					{
						details.Update(typeface);
					}
				}
			}
		}
	}

	private static async Task<SKTypeface?> LoadTypefaceFromApplicationUriAsync(Uri uri, FontWeight weight, FontStyle style, FontStretch stretch)
	{
		try
		{
			var manifestUri = new Uri(uri.OriginalString + ".manifest");
			var path = Uri.UnescapeDataString(manifestUri.PathAndQuery).TrimStart('/');
			if (await StorageFileHelper.ExistsInPackage(path))
			{
				var manifestFile = await StorageFile.GetFileFromApplicationUriAsync(manifestUri);
				var manifestStream = await manifestFile.OpenStreamForReadAsync();
				uri = new Uri(FontManifestHelpers.GetFamilyNameFromManifest(manifestStream, weight, style, stretch));
			}
		}
		catch (Exception e)
		{
			if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
			{
				typeof(FontDetailsCache).Log().LogError($"Failed to load font manifest for {uri}: {e}");
			}
		}

		if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(FontDetailsCache).Log().LogDebug($"Fetching font from {uri}");
		}

		var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
		var stream = await file.OpenStreamForReadAsync();
		return stream is null ? null : SKTypeface.FromStream(stream);
	}

	private static FontDetails GetFontInternal(
		string name,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		var skWeight = weight.ToSkiaWeight();
		var skWidth = stretch.ToSkiaWidth();
		var skSlant = style.ToSkiaSlant();

		SKTypeface? skTypeFace;
		bool temporaryDefaultFont = false;

		var hashIndex = name.IndexOf('#');
		if (hashIndex > 0)
		{
			name = name.Substring(0, hashIndex);
		}

		if (Uri.TryCreate(name, UriKind.Absolute, out var uri) && uri.IsLocalResource())
		{
			var task = LoadTypefaceFromApplicationUriAsync(uri, weight, style, stretch);
			if (task.IsCompleted)
			{
				if (task.IsCompletedSuccessfully)
				{
					skTypeFace = task.Result;
				}
				else
				{
					// Load failed.
					OnFontLoaded(name, null);
					skTypeFace = null;
				}
			}
			else
			{
				temporaryDefaultFont = true;
				skTypeFace = null;
				task.ContinueWith(task =>
				{
					NativeDispatcher.Main.Enqueue(() =>
					{
						try
						{
							if (task.IsCompletedSuccessfully)
							{
								OnFontLoaded(name, task.Result);
							}
							else
							{
								// Load failed.
								OnFontLoaded(name, null);
							}
						}
						catch (Exception e)
						{
							if (typeof(FontDetailsCache).Log().IsEnabled(LogLevel.Error))
							{
								typeof(FontDetailsCache).Log().LogError($"Failed to load font {name} from ms-appx: {e}");
							}
						}
					});
				});
			}
		}
		else
		{
			// FromFontFamilyName may return null: https://github.com/mono/SkiaSharp/issues/1058
			skTypeFace = SKTypeface.FromFamilyName(name, skWeight, skWidth, skSlant);
		}

		if (skTypeFace == null)
		{
			if (typeof(Inline).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(Inline).Log().LogWarning($"The font {name} could not be found, using system default");
			}

			skTypeFace = SKTypeface.FromFamilyName(FeatureConfiguration.Font.DefaultTextFontFamily, skWeight, skWidth, skSlant)
				?? SKTypeface.FromFamilyName(null, skWeight, skWidth, skSlant)
				?? SKTypeface.FromFamilyName(null);
		}

		return FontDetails.Create(skTypeFace, fontSize, temporaryDefaultFont);
	}

	public static FontDetails GetFont(
		string? name,
		float fontSize,
		FontWeight weight,
		FontStretch stretch,
		FontStyle style)
	{
		if (name == null || string.Equals(name, "XamlAutoFontFamily", StringComparison.OrdinalIgnoreCase))
		{
			name = FeatureConfiguration.Font.DefaultTextFontFamily;
		}

		var key = new FontCacheEntry(name, fontSize, weight, stretch, style);

		lock (_fontCacheGate)
		{
			if (!_fontCache.TryGetValue(key, out var value))
			{
				_fontCache[key] = value = GetFontInternal(name, fontSize, weight, stretch, style);
			}
			return value;
		}
	}
}
