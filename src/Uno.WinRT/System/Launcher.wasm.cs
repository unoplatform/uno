using System;
using System.IO;
using System.Threading.Tasks;
using Uno.Foundation;
using Uno.Foundation.Logging;
using Windows.Storage;

using NativeMethods = __Windows.__System.Launcher.NativeMethods;

namespace Windows.System
{
	public static partial class Launcher
	{
		internal static Task<bool> LaunchUriPlatformAsync(Uri uri)
		{
			var result = NativeMethods.Open(uri.OriginalString);
			return Task.FromResult(result == "True");
		}

		internal static async Task<bool> LaunchFilePlatformAsync(IStorageFile file)
		{
			try
			{
				// The browser sandbox has no access to the OS file associations, so the content
				// goes to the browser, which shows it in a new tab or downloads it.
				using var stream = await file.OpenStreamForReadAsync();
				using var buffer = new MemoryStream();
				await stream.CopyToAsync(buffer);

				var result = NativeMethods.OpenFile(buffer.ToArray(), file.ContentType ?? string.Empty, file.Name);
				return result == "True";
			}
			catch (Exception exception)
			{
				if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
				{
					typeof(Launcher).Log().Error($"Failed to {nameof(LaunchFileAsync)}.", exception);
				}

				return false;
			}
		}
	}
}
