#nullable enable

using System;
using System.Linq;
using Windows.Foundation;

namespace Windows.Storage.Streams
{
	/// <summary>
	/// This is responsible to asynchronously load the content of a remote content into a temporary file
	/// </summary>
	/// <remarks>
	/// The temporary file belong to this downloader.
	/// It's is responsibility to delete it on dispose.
	/// Users have to keep an active reference on this downloader to maintain the file alive.
	/// It might however has to share the file with an <see cref="IStreamedDataUploader"/>.
	/// </remarks>
	internal interface IStreamedDataLoader
	{
		/// <summary>
		/// An event raised when some data has been saved into the temporary file.
		/// </summary>
		public event TypedEventHandler<IStreamedDataLoader, object?>? DataUpdated;

		/// <summary>
		/// Gets the temporary file in which data is loaded
		/// </summary>
		TemporaryFile File { get; }

		/// <summary>
		/// The content type of the loaded data
		/// </summary>
		string? ContentType { get; }

		/// <summary>
		/// Throws an exception if the load failed
		/// </summary>
		void CheckState();

		/// <summary>
		/// Indicates if the given position has been or not yet,
		/// **or** the load is now completed and the given position will never be present.
		/// </summary>
		bool CanRead(ulong position);
	}
}
