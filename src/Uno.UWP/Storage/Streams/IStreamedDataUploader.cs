#nullable enable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Windows.Storage.Streams
{
	/// <summary>
	/// This is responsible to asynchronously upload the content of a remote
	/// </summary>
	/// <remarks>
	/// The temporary file belong to this uploader.
	/// It's is responsibility to delete it on dispose.
	/// Users have to keep an active reference on this uploader to maintain the file alive.
	/// It might however has to share the file with an <see cref="IStreamedDataLoader"/>.
	/// </remarks>
	internal interface IStreamedDataUploader
	{
		/// <summary>
		/// Gets the temporary file in which data is loaded
		/// </summary>
		TemporaryFile File { get; }

		/// <summary>
		/// Throws an exception if the load failed
		/// </summary>
		void CheckState();

		/// <summary>
		/// Send a chunk of data to the remote
		/// </summary>
		public Task<bool> Push(ulong index, ulong length, CancellationToken ct);
	}
}
