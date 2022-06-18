#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Foundation;

namespace Windows.UI.Xaml.Media
{
	partial class FontFamily
	{
		private class FontLoader
		{
			private static readonly Dictionary<FontFamily, FontLoader> _loaders = new();

			internal static FontLoader GetLoaderForFontFamily(FontFamily forFamily)
			{
				if (_loaders.TryGetValue(forFamily, out var loader))
				{
					// There is already a loader for this font family
					return loader;
				}

				loader = new FontLoader(forFamily);
				_loaders.Add(forFamily, loader);

				return loader;
			}

			private IList<WeakReference>? _waitingList;

			private FontLoader(FontFamily fontFamily)
			{
				ParseSource(fontFamily.Source);
			}

			private void ParseSource(string source)
			{
				var sourceParts = source.Split(new[] { '#' }, 2, StringSplitOptions.RemoveEmptyEntries);

				if (sourceParts.Length == 2)
				{
					if (TryGetExternalUri(sourceParts[0], out var externalUri) && externalUri is { })
					{
						_externalSource = externalUri.OriginalString;
						CssFontName = $"font{_externalSource.GetHashCode()}";
					}
					else
					{
						CssFontName = sourceParts[1];
					}
				}
				else
				{
					if (TryGetExternalUri(sourceParts[0], out var externalUri) && externalUri is { })
					{
						_externalSource = externalUri.OriginalString;
						CssFontName = $"font{_externalSource.GetHashCode()}";
					}
					else
					{
						CssFontName = sourceParts[0];
					}
				}

			}

			private static bool TryGetExternalUri(string? source, out Uri? uri)
			{
				if (source is not null && (source.IndexOf('.') > -1 || source.IndexOf('/') > -1))
				{
					uri = new Uri(source, UriKind.RelativeOrAbsolute);

					if (uri.IsAbsoluteUri && uri.Scheme is "ms-appx")
					{
						// TODO-AGNÈS: resolve managed path here
						uri = new Uri(uri.PathAndQuery.TrimStart('/'), UriKind.Relative);
					}

					return true;
				}

				uri = default;
				return false;
			}

			internal string CssFontName { get; private set; }
			private string _externalSource;

			internal bool IsLoaded { get; private set; }
			internal bool IsLoading { get; private set; }

			internal static void NotifyFontLoaded(string cssFontName)
			{
				var loaderEntry = _loaders
					.FirstOrDefault(l => l.Value.CssFontName.Equals(cssFontName, StringComparison.Ordinal));

				if (loaderEntry is { Value: { } loader })
				{
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
			}

			internal void RegisterRemeasureOnFontLoaded(UIElement uiElement)
			{
				if (IsLoaded)
				{
					return; // already loaded: nothing to do
				}

				var weak = new WeakReference(uiElement);

				// TODO-AGNÈS: use weak reference pooling instead
				(_waitingList ??= new List<WeakReference>()).Add(weak);

				if (IsLoading)
				{
					return; // already loading
				}

				LaunchLoading();
			}

			internal void LaunchLoading()
			{
				if (IsLoaded || IsLoading)
				{
					return; // Already loaded
				}

				IsLoading = true;

				if (_externalSource is { Length: > 0})
				{
					WebAssemblyRuntime.InvokeJS($"Windows.UI.Xaml.Media.FontFamily.loadFont(\"{CssFontName}\",\"{_externalSource}\")");
				}
				else
				{
					WebAssemblyRuntime.InvokeJS($"Windows.UI.Xaml.Media.FontFamily.forceFontUsage(\"{CssFontName}\")");
				}
			}
		}
	}
}
