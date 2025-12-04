using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Helper class for managing asynchronous data extraction from IDataObject instances
/// that support IDataObjectAsyncCapability.
/// </summary>
internal static class DataObjectAsyncCapabilityHelper
{
	/// <summary>
	/// Attempts to query the IDataObject for IDataObjectAsyncCapability support.
	/// </summary>
	/// <param name="dataObject">The data object to query.</param>
	/// <param name="asyncCapability">The async capability interface if supported, null otherwise.</param>
	/// <returns>True if the data object supports async capability, false otherwise.</returns>
	public static unsafe bool TryGetAsyncCapability(
		IDataObject* dataObject,
		out IDataObjectAsyncCapability? asyncCapability)
	{
		asyncCapability = null;

		try
		{
			// Try to query for IDataObjectAsyncCapability
			var iid = typeof(IDataObjectAsyncCapability).GUID;
			var result = ((IUnknown*)dataObject)->QueryInterface(in iid, out var ppv);

			if (result.Succeeded && ppv != null)
			{
				asyncCapability = Marshal.GetObjectForIUnknown((IntPtr)ppv) as IDataObjectAsyncCapability;
				return asyncCapability != null;
			}
		}
		catch (Exception ex)
		{
			typeof(DataObjectAsyncCapabilityHelper).Log().Error(
				$"Failed to query for IDataObjectAsyncCapability: {ex.Message}");
		}

		return false;
	}

	/// <summary>
	/// Checks if the data object is currently in an asynchronous operation.
	/// </summary>
	public static bool IsInAsyncOperation(IDataObjectAsyncCapability asyncCapability)
	{
		try
		{
			var hr = asyncCapability.InOperation(out var inOperation);
			return hr.Succeeded && inOperation;
		}
		catch (Exception ex)
		{
			typeof(DataObjectAsyncCapabilityHelper).Log().Error(
				$"Failed to check async operation status: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Checks if the data object supports asynchronous mode.
	/// </summary>
	public static bool SupportsAsyncMode(IDataObjectAsyncCapability asyncCapability)
	{
		try
		{
			var hr = asyncCapability.GetAsyncMode(out var isAsync);
			return hr.Succeeded && isAsync;
		}
		catch (Exception ex)
		{
			typeof(DataObjectAsyncCapabilityHelper).Log().Error(
				$"Failed to check async mode: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Sets the data object to asynchronous mode.
	/// </summary>
	public static bool SetAsyncMode(IDataObjectAsyncCapability asyncCapability, bool enable)
	{
		try
		{
			var hr = asyncCapability.SetAsyncMode(enable);
			return hr.Succeeded;
		}
		catch (Exception ex)
		{
			typeof(DataObjectAsyncCapabilityHelper).Log().Error(
				$"Failed to set async mode: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Starts an asynchronous operation.
	/// </summary>
	public static bool StartOperation(IDataObjectAsyncCapability asyncCapability)
	{
		try
		{
			var hr = asyncCapability.StartOperation(IntPtr.Zero);
			return hr.Succeeded;
		}
		catch (Exception ex)
		{
			typeof(DataObjectAsyncCapabilityHelper).Log().Error(
				$"Failed to start async operation: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Ends an asynchronous operation.
	/// </summary>
	public static bool EndOperation(
		IDataObjectAsyncCapability asyncCapability,
		HRESULT result,
		uint effects = 0)
	{
		try
		{
			var hr = asyncCapability.EndOperation(result, IntPtr.Zero, effects);
			return hr.Succeeded;
		}
		catch (Exception ex)
		{
			typeof(DataObjectAsyncCapabilityHelper).Log().Error(
				$"Failed to end async operation: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Waits for an asynchronous operation to complete, polling the InOperation status.
	/// </summary>
	/// <param name="asyncCapability">The async capability interface.</param>
	/// <param name="timeout">Maximum time to wait.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>True if the operation completed, false if timed out or cancelled.</returns>
	public static async Task<bool> WaitForOperationAsync(
		IDataObjectAsyncCapability asyncCapability,
		TimeSpan timeout,
		CancellationToken cancellationToken = default)
	{
		var startTime = DateTime.UtcNow;

		while (DateTime.UtcNow - startTime < timeout)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				return false;
			}

			if (!IsInAsyncOperation(asyncCapability))
			{
				return true;
			}

			// Wait a short time before polling again
			await Task.Delay(50, cancellationToken).ConfigureAwait(false);
		}

		return false;
	}
}
