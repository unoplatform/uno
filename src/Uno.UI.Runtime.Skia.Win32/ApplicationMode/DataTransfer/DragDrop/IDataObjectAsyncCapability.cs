using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Allows data transfer operations to continue in the background so that the application can remain responsive to user interaction.
/// Implements the IDataObjectAsyncCapability interface as documented in:
/// https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-idataobjectasynccapability
/// </summary>
[ComImport]
[Guid("3D8B0590-F691-11d2-8EA9-006097DF5BD4")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDataObjectAsyncCapability
{
	/// <summary>
	/// Called by a drop source to specify whether the data object supports asynchronous data extraction.
	/// </summary>
	/// <param name="fDoOpAsync">
	/// TRUE to set the asynchronous mode; FALSE to cancel asynchronous mode and revert to synchronous mode.
	/// </param>
	/// <returns>S_OK if successful, or an error value otherwise.</returns>
	HRESULT SetAsyncMode([In] bool fDoOpAsync);

	/// <summary>
	/// Called by a drop target to determine whether the data object supports asynchronous data extraction.
	/// </summary>
	/// <param name="pfIsOpAsync">
	/// When this method returns, contains TRUE if asynchronous mode is supported; otherwise, FALSE.
	/// </param>
	/// <returns>S_OK if successful, or an error value otherwise.</returns>
	HRESULT GetAsyncMode([Out] out bool pfIsOpAsync);

	/// <summary>
	/// Called by a drop source to notify the data object that an asynchronous data extraction operation has begun.
	/// The data object is expected to return immediately, and the actual data extraction continues on a background thread.
	/// </summary>
	/// <param name="pbcReserved">Reserved. Must be NULL.</param>
	/// <returns>S_OK if successful, or an error value otherwise.</returns>
	HRESULT StartOperation([In] IntPtr pbcReserved);

	/// <summary>
	/// Checks whether an asynchronous data extraction operation is currently in progress.
	/// </summary>
	/// <param name="pfInAsyncOp">
	/// When this method returns, contains TRUE if an asynchronous operation is currently in progress; otherwise, FALSE.
	/// </param>
	/// <returns>S_OK if successful, or an error value otherwise.</returns>
	HRESULT InOperation([Out] out bool pfInAsyncOp);

	/// <summary>
	/// Notifies the data object that the asynchronous data extraction operation has ended.
	/// </summary>
	/// <param name="hResult">
	/// The result of the asynchronous operation. S_OK if successful, or an error value otherwise.
	/// </param>
	/// <param name="pbcReserved">Reserved. Must be NULL.</param>
	/// <param name="dwEffects">
	/// A DROPEFFECT value that indicates the outcome of the optimized move.
	/// </param>
	/// <returns>S_OK if successful, or an error value otherwise.</returns>
	HRESULT EndOperation(
		[In] HRESULT hResult,
		[In] IntPtr pbcReserved,
		[In] uint dwEffects);
}
