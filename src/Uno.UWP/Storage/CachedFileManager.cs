#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Provider;

namespace Windows.Storage
{
	/// <summary>
	/// Lets apps manage real-time updates to files.
	/// </summary>
	public static partial class CachedFileManager
	{
		/// <summary>
		/// Lets apps defer real-time updates for a specified file.
		/// </summary>
		/// <param name="file">The file to defer updates for.</param>
		/// <remarks>
		/// In case of Uno Platform, this method currently
		/// does not have any impact.
		/// </remarks>
		public static void DeferUpdates(IStorageFile file)
		{
		}

		/// <summary>
		/// Initiates updates for the specified file. This method contacts the app that provided the file to perform the updates.
		/// </summary>
		/// <param name="file">The file to update.</param>
		/// <returns>
		/// When this method completes, it returns a FileUpdateStatus
		/// enum value that describes the status of the updates to the file.
		/// </returns>
		/// <remarks>
		///	On most Uno Platform targets, this method immediately returns
		///	success, as the file is already updated. In case of WASM using
		///	the download file picker, this triggers the download file dialog.
		/// </remarks>
		public static IAsyncOperation<FileUpdateStatus> CompleteUpdatesAsync(IStorageFile file) =>
			AsyncOperation.FromTask(ct => CompleteUpdatesTaskAsync(file, ct));
	}
}
