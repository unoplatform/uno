using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.Storage.Streams;

/// <summary>
/// Commits data in a buffer to a backing store.
/// </summary>
public partial class DataWriterStoreOperation : IAsyncOperation<uint>, IAsyncInfo, IAsyncOperationInternal<uint>
{
	private readonly IAsyncOperationInternal<uint> _asyncOperation;

	internal DataWriterStoreOperation(IAsyncOperation<uint> asyncOperation)
	{
		_asyncOperation = (IAsyncOperationInternal<uint>)asyncOperation;
	}

	/// <summary>
	/// Gets or sets the handler to call when the data store operation is complete.
	/// </summary>
	public AsyncOperationCompletedHandler<uint> Completed
	{
		get => _asyncOperation.Completed;
		set => _asyncOperation.Completed = value;
	}

	/// <summary>
	/// Gets the error code for the data store operation if it fails.
	/// </summary>
	public Exception ErrorCode => _asyncOperation.ErrorCode;

	/// <summary>
	/// Gets a unique identifier that represents the data store operation.
	/// </summary>
	public uint Id => _asyncOperation.Id;

	/// <summary>
	/// Gets the current status of the data store operation.
	/// </summary>
	public AsyncStatus Status => _asyncOperation.Status;

	/// <summary>
	/// Requests the cancellation of the data store operation.
	/// </summary>
	public void Cancel() => _asyncOperation.Cancel();

	/// <summary>
	/// Requests that work associated with the data store operation should stop.
	/// </summary>
	public void Close() => _asyncOperation.Close();

	/// <summary>
	/// Returns the result of the data store operation.
	/// </summary>
	/// <returns></returns>
	public uint GetResults() => _asyncOperation.GetResults();

	Task<uint> IAsyncOperationInternal<uint>.Task => _asyncOperation.Task;
}
