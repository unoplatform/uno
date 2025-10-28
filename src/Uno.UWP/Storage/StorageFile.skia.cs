using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Helpers;
using Windows.ApplicationModel;
using Windows.Storage;

namespace Windows.Storage
{
	partial class StorageFile
	{
		internal static string ResourcePathBase { get; set; } = Package.Current.InstalledPath;

		private static async Task<StorageFile> GetFileFromApplicationUri(CancellationToken ct, Uri uri)
		{
			if (uri.Scheme != "ms-appx")
			{
				// ms-appdata is handled by the caller.
				throw new InvalidOperationException("Uri is not using the ms-appx or ms-appdata scheme");
			}

			var path = Uri.UnescapeDataString(uri.PathAndQuery).TrimStart('/');

			var resourcePathname = global::System.IO.Path.Combine(ResourcePathBase, uri.Host, path);

			if (resourcePathname != null)
			{
				return await StorageFile.GetFileFromPathAsync(resourcePathname);
			}
			else
			{
				throw new FileNotFoundException($"The file [{path}] cannot be found  in the package directory");
			}
		}
	}
}

namespace Uno.Storage
{
	internal class StorageFile
	{
		public void RegisterProvider(IStorageFileProvider provider, StorageFileProviderPriority priority)
		{
			
		}
	}

	internal enum StorageFileProviderPriority
	{
		/// <summary>
		/// Prioerity used by default file system lookup if no other is specified.
		/// </summary>
		Default = 0,

		/// <summary>
		/// Used to register low priority providers that should be used only if no other provider matches.
		/// </summary>
		Low = -100,

		/// <summary>
		/// Represents a high priority level with a value of 100.
		/// </summary>
		High = 100,
	}

	internal interface IStorageFileProvider
	{
		public ValueTask<StorageFile> GetFileAsync(Uri uri, CancellationToken ct);
	}
}
