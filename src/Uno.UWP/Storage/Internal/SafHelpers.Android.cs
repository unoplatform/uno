using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Provider;
using Windows.Storage.FileProperties;

namespace Uno.Storage.Internal
{
	/// <summary>
	/// Shared functionality handling for Android's Storage
	/// Access Framework-based StorageFiles and StorageFolders.
	/// </summary>
	internal static class SafHelpers
    {
		/// <summary>
		/// Retrieves basic properties for a given SAF Uri.
		/// </summary>
		/// <param name="safUri">SAF Uri.</param>
		/// <param name="includeSize">A value indicating whether the size should be included (not useful for folders).</param>
		/// <param name="token">Cancellation token.</param>
		/// <returns>Basic properties.</returns>
		public static async Task<BasicProperties> GetBasicPropertiesAsync(Android.Net.Uri safUri, bool includeSize, CancellationToken token)
		{
			if (Application.Context.ContentResolver == null)
			{
				throw new InvalidOperationException("Content resolver for app is not available.");
			}

			return await Task.Run(() =>
			{
				var cursor = Application.Context.ContentResolver.Query(safUri, null, null, null, null, null);
				if (cursor == null)
				{
					throw new UnauthorizedAccessException("Cannot access the folder.");
				}

				using (cursor)
				{
					if (cursor.MoveToFirst())
					{
						int size = 0;
						var sizeColumnIndex = cursor.GetColumnIndex(DocumentsContract.Document.ColumnSize);
						if (includeSize && sizeColumnIndex >= 0)
						{
							size = cursor.GetInt(sizeColumnIndex);
						}
						var lastModified = DateTimeOffset.MinValue;
						var lastModifiedIndex = cursor.GetColumnIndex(DocumentsContract.Document.ColumnLastModified);
						if (lastModifiedIndex >= 0)
						{
							var lastModifiedTimestamp = cursor.GetLong(lastModifiedIndex);
							lastModified = DateTimeOffset.FromUnixTimeMilliseconds(lastModifiedTimestamp);
						}
						return new BasicProperties((ulong)size, lastModified);
					}
				}
				return new BasicProperties(0, DateTimeOffset.MinValue);
			}, token);
		}
    }
}
