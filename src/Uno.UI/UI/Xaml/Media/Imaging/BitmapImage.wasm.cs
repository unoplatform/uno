using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation;
using Windows.Graphics.Display;
using Windows.Storage.Streams;
using Path = global::System.IO.Path;

namespace Windows.UI.Xaml.Media.Imaging
{
	public sealed partial class BitmapImage : BitmapSource
	{
		private static readonly string UNO_BOOTSTRAP_APP_BASE = Environment.GetEnvironmentVariable(nameof(UNO_BOOTSTRAP_APP_BASE));

		internal ResolutionScale? ScaleOverride { get; set; }

		internal string ContentType { get; set; } = "application/octet-stream";

		private protected override bool TryOpenSourceAsync(
			CancellationToken ct,
			int? targetWidth,
			int? targetHeight,
			out Task<ImageData> asyncImage)
		{
			if (WebUri is {} uri)
			{
				var hasFileScheme = uri.IsAbsoluteUri && uri.Scheme == "file";

				// Local files are assumed as coming from the remote server
				var newUri = hasFileScheme switch
				{
					true => new Uri(uri.PathAndQuery.TrimStart('/'), UriKind.Relative),
					_ => uri
				};

				asyncImage = AssetResolver.ResolveImageAsync(this, newUri, ScaleOverride);

				return true;
			}

			if (_stream is {} stream)
			{
				void OnProgress(ulong position, ulong? length)
				{
					if (position > 0 && length is { } actualLength)
					{
						var percent = (int)((position / (float)actualLength) * 100);
						RaiseDownloadProgress(percent);
					}
				}

				var streamWithContentType = stream.TrySetContentType(ContentType);

				asyncImage = OpenFromStream(streamWithContentType, OnProgress, ct);

				return true;
			}
			
			asyncImage = default;
			return false;
		}

		internal static class AssetResolver
		{
			private static readonly Lazy<Task<HashSet<string>>> _assets = new Lazy<Task<HashSet<string>>>(GetAssets);

			private static async Task<HashSet<string>> GetAssets()
			{
				var assetsUri = !string.IsNullOrEmpty(UNO_BOOTSTRAP_APP_BASE) ? $"{UNO_BOOTSTRAP_APP_BASE}/uno-assets.txt" : "uno-assets.txt";

				var assets = await WebAssemblyRuntime.InvokeAsync($"fetch('{assetsUri}').then(r => r.text())");

				return new HashSet<string>(Regex.Split(assets, "\r\n|\r|\n"));
			}

			internal static async Task<ImageData> ResolveImageAsync(ImageSource source, Uri uri, ResolutionScale? scaleOverride)
			{
				try
				{
					// ms-appx comes in as a relative path
					if (uri.IsAbsoluteUri)
					{
						if (uri.Scheme == "http" || uri.Scheme == "https")
						{
							return new ImageData
							{
								Kind = ImageDataKind.Url,
								Value = uri.AbsoluteUri,
								Source = source
							};
						}

						// TODO: Implement ms-appdata
						return new ImageData();
					}

					// POTENTIAL BUG HERE: if the "fetch" failed, the application
					// will never retry to fetch it again.
					var assets = await _assets.Value;

					return new ImageData
					{
						Kind = ImageDataKind.Url,
						Value = GetScaledPath(uri.OriginalString, assets, scaleOverride),
						Source = source
					};
				}
				catch (Exception e)
				{
					return new ImageData { Kind = ImageDataKind.Error, Error = e };
				}
			}

			private static string GetScaledPath(string path, HashSet<string> assets, ResolutionScale? scaleOverride)
			{
				if (!string.IsNullOrEmpty(path))
				{
					var directory = Path.GetDirectoryName(path);
					var filename = Path.GetFileNameWithoutExtension(path);
					var extension = Path.GetExtension(path);

					var resolutionScale = scaleOverride == null ? (int)DisplayInformation.GetForCurrentView().ResolutionScale : (int)scaleOverride;

					for (var i = KnownScales.Length - 1; i >= 0; i--)
					{
						var probeScale = KnownScales[i];
					
						if (resolutionScale >= probeScale)
						{
							var filePath = Path.Combine(directory, $"{filename}.scale-{probeScale}{extension}");

							if (assets.Contains(filePath))
							{
								return !string.IsNullOrEmpty(UNO_BOOTSTRAP_APP_BASE) ?
									$"{UNO_BOOTSTRAP_APP_BASE}/{filePath}" :
									filePath;
							}
						}
					}

					return !string.IsNullOrEmpty(UNO_BOOTSTRAP_APP_BASE) ? $"{UNO_BOOTSTRAP_APP_BASE}/{path}" : path;
				}

				return path;
			}

			private static readonly int[] KnownScales =
			{
				(int)ResolutionScale.Scale100Percent,
				(int)ResolutionScale.Scale120Percent,
				(int)ResolutionScale.Scale125Percent,
				(int)ResolutionScale.Scale140Percent,
				(int)ResolutionScale.Scale150Percent,
				(int)ResolutionScale.Scale160Percent,
				(int)ResolutionScale.Scale175Percent,
				(int)ResolutionScale.Scale180Percent,
				(int)ResolutionScale.Scale200Percent,
				(int)ResolutionScale.Scale225Percent,
				(int)ResolutionScale.Scale250Percent,
				(int)ResolutionScale.Scale300Percent,
				(int)ResolutionScale.Scale350Percent,
				(int)ResolutionScale.Scale400Percent,
				(int)ResolutionScale.Scale450Percent,
				(int)ResolutionScale.Scale500Percent
			};
		}

		internal override void ReportImageLoaded()
		{
			RaiseImageOpened();
		}

		internal override void ReportImageFailed(string errorMessage)
		{
			RaiseImageFailed(new Exception("Unable to load image: " + errorMessage));
		}
	}
}
