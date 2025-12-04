using System;
using System.Runtime.InteropServices;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Helper class to handle asynchronous data operations from IDataObject (used by Chromium-based applications).
/// Based on: https://learn.microsoft.com/en-us/windows/win32/shell/datascenarios#async
/// Uses IDataObjectAsyncCapability interface for async operations.
/// </summary>
internal static unsafe class AsyncDataHelper
{
	// IDataObjectAsyncCapability IID from urlmon.h
	private static readonly Guid IID_IDataObjectAsyncCapability = new("3D8B0590-F691-11d2-8EA9-006097DF5BD4");

	/// <summary>
	/// Checks if the data object supports asynchronous operations.
	/// </summary>
	public static bool SupportsAsyncOperation(IDataObject* dataObject)
	{
		try
		{
			// Query for IDataObjectAsyncCapability interface
			void* asyncCap = null;
			var hr = dataObject->QueryInterface(&IID_IDataObjectAsyncCapability, &asyncCap);
			
			if (hr.Succeeded && asyncCap != null)
			{
				// Release the interface
				((IUnknown*)asyncCap)->Release();
				return true;
			}
			
			return false;
		}
		catch (Exception ex)
		{
			typeof(AsyncDataHelper).Log().LogError($"Error checking async operation support: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Initiates an asynchronous data operation.
	/// </summary>
	public static bool StartAsyncOperation(IDataObject* dataObject)
	{
		try
		{
			Windows.Win32.UI.Shell.IDataObjectAsyncCapability asyncCap = null;
			var hr = dataObject->QueryInterface(&IID_IDataObjectAsyncCapability, (void**)&asyncCap);
			
			if (hr.Failed || asyncCap == null)
			{
				typeof(AsyncDataHelper).Log().LogDebug("IDataObjectAsyncCapability not supported");
				return false;
			}

			try
			{
				// Set async mode to TRUE to indicate we want async data
				hr = asyncCap.SetAsyncMode(true);
				if (hr.Failed)
				{
					typeof(AsyncDataHelper).Log().LogWarning($"SetAsyncMode failed: {Win32Helper.GetErrorMessage(hr)}");
					return false;
				}

				// Start the async operation
				hr = asyncCap.StartOperation(null);
				if (hr.Failed)
				{
					typeof(AsyncDataHelper).Log().LogWarning($"StartOperation failed: {Win32Helper.GetErrorMessage(hr)}");
					return false;
				}

				typeof(AsyncDataHelper).Log().LogInformation("Async operation started successfully");
				return true;
			}
			finally
			{
				if (asyncCap != null)
				{
					Marshal.ReleaseComObject(asyncCap);
				}
			}
		}
		catch (Exception ex)
		{
			typeof(AsyncDataHelper).Log().LogError($"Error starting async operation: {ex.Message}");
			return false;
		}
	}

	/// <summary>
	/// Checks if an async operation is still in progress.
	/// </summary>
	public static bool IsAsyncInProgress(IDataObject* dataObject)
	{
		try
		{
			Windows.Win32.IDataObjectAsyncCapability asyncCap = null;
			var hr = dataObject->QueryInterface(&IID_IDataObjectAsyncCapability, (void**)&asyncCap);
			
			if (hr.Failed || asyncCap == null)
			{
				return false;
			}

			try
			{
				BOOL inProgress = false;
				hr = asyncCap.InOperation(&inProgress);
				return hr.Succeeded && inProgress;
			}
			finally
			{
				if (asyncCap != null)
				{
					Marshal.ReleaseComObject(asyncCap);
				}
			}
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Waits for an async operation to complete with a timeout.
	/// </summary>
	public static bool WaitForAsyncCompletion(IDataObject* dataObject, int timeoutMs = 5000)
	{
		var startTime = Environment.TickCount;
		
		while (IsAsyncInProgress(dataObject))
		{
			if (Environment.TickCount - startTime > timeoutMs)
			{
				typeof(AsyncDataHelper).Log().LogWarning($"Async operation timed out after {timeoutMs}ms");
				return false;
			}
			
			// Small sleep to avoid busy-waiting
			Thread.Sleep(10);
		}
		
		typeof(AsyncDataHelper).Log().LogInformation("Async operation completed");
		return true;
	}

	/// <summary>
	/// Completes an async operation.
	/// </summary>
	public static void CompleteAsyncOperation(IDataObject* dataObject, HRESULT result)
	{
		try
		{
			Windows.Win32.IDataObjectAsyncCapability asyncCap = null;
			var hr = dataObject->QueryInterface(&IID_IDataObjectAsyncCapability, (void**)&asyncCap);
			
			if (hr.Failed || asyncCap == null)
			{
				return;
			}

			try
			{
				asyncCap.EndOperation(result, null, 0);
			}
			finally
			{
				if (asyncCap != null)
				{
					Marshal.ReleaseComObject(asyncCap);
				}
			}
		}
		catch (Exception ex)
		{
			typeof(AsyncDataHelper).Log().LogError($"Error completing async operation: {ex.Message}");
		}
	}
}
