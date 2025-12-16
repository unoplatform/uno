using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Windows.Win32.System.SystemServices;
using Uno.Disposables;
using Uno.Foundation.Logging;
using IDataObject = Windows.Win32.System.Com.IDataObject;

namespace Uno.UI.Runtime.Skia.Win32;

// IDropTarget implementation for handling drag-and-drop operations
internal partial class Win32DragDropExtension
{
	// Supported TYMED types for drag-drop operations
	private const TYMED SupportedTymed = TYMED.TYMED_HGLOBAL | TYMED.TYMED_ISTREAM | TYMED.TYMED_FILE;

	unsafe HRESULT IDropTarget.Interface.DragEnter(IDataObject* dataObject, MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		Debug.Assert(_manager is not null && _coreDragDropManager is not null);

		IEnumFORMATETC* enumFormatEtc;
		var hResult = dataObject->EnumFormatEtc((uint)DATADIR.DATADIR_GET, &enumFormatEtc);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IDataObject.EnumFormatEtc)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return HRESULT.E_UNEXPECTED;
		}

		using var enumFormatDisposable = new DisposableStruct<IntPtr>(static p => ((IEnumFORMATETC*)p)->Release(), (IntPtr)enumFormatEtc);

		enumFormatEtc->Reset();
		const int formatBufferLength = 100;
		var formatBuffer = stackalloc FORMATETC[formatBufferLength];
		uint fetchedFormatCount;
		hResult = enumFormatEtc->Next(formatBufferLength, formatBuffer, &fetchedFormatCount);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(PInvoke.RegisterDragDrop)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return HRESULT.E_UNEXPECTED;
		}

		var position = new System.Drawing.Point(pt.x, pt.y);

		var success = PInvoke.ScreenToClient(_hwnd, ref position);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}"); }
		var scaledPosition = GetScaledPosition(position.X, position.Y);

		var src = new DragEventSource(scaledPosition, grfKeyState);

		var formats = new Span<FORMATETC>(formatBuffer, (int)fetchedFormatCount);
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			var log = $"{nameof(IDropTarget.Interface.DragEnter)} @ {position}, formats: ";
			foreach (var format in formats)
			{
				log += $"{(CLIPBOARD_FORMAT)format.cfFormat}(tymed={(TYMED)format.tymed}) ";
			}
			this.Log().Trace(log);
		}

		var package = new DataPackage();

		var formatEtcList = formats.ToArray();
		var formatList =
			formatEtcList
			.Where(static formatetc =>
			{
				// Check if any of the supported TYMED types are available (tymed is a bitmask)
				if (((TYMED)formatetc.tymed & SupportedTymed) == 0)
				{
					typeof(Win32DragDropExtension).LogTrace()?.Trace($"{nameof(IDropTarget.Interface.DragEnter)} found {Enum.GetName((CLIPBOARD_FORMAT)formatetc.cfFormat)}, but {nameof(TYMED)} ({(TYMED)formatetc.tymed}) does not include any supported types ({SupportedTymed})");
					return false;
				}

				return true;
			})
			.Select(f => (CLIPBOARD_FORMAT)f.cfFormat)
			.ToList();

		var mediumsToDispose = new List<STGMEDIUM>();
		var allocatedHGlobals = new List<HGLOBAL>();
		using var mediumsDisposable = new DisposableStruct<List<STGMEDIUM>>(static list =>
		{
			foreach (var medium in list)
			{
				PInvoke.ReleaseStgMedium(&medium);
			}
		}, mediumsToDispose);
		using var hGlobalsDisposable = new DisposableStruct<List<HGLOBAL>>(static list =>
		{
			foreach (var hGlobal in list)
			{
				PInvoke.GlobalFree(hGlobal);
			}
		}, allocatedHGlobals);
		Win32ClipboardExtension.ReadContentIntoPackage(package, formatList, format =>
		{
			var formatEtc = formatEtcList.First(f => f.cfFormat == (int)format);

			// Try to get data preferring HGLOBAL, then IStream, then FILE
			var preferredFormatEtc = formatEtc with { tymed = (uint)TYMED.TYMED_HGLOBAL };
			var getDataResult = dataObject->GetData(preferredFormatEtc, out STGMEDIUM medium);

			if (getDataResult.Failed || medium.tymed != TYMED.TYMED_HGLOBAL)
			{
				// Try IStream if HGLOBAL failed
				if (((TYMED)formatEtc.tymed & TYMED.TYMED_ISTREAM) != 0)
				{
					preferredFormatEtc = formatEtc with { tymed = (uint)TYMED.TYMED_ISTREAM };
					getDataResult = dataObject->GetData(preferredFormatEtc, out medium);

					if (getDataResult.Succeeded && medium.tymed == TYMED.TYMED_ISTREAM)
					{
						// Convert IStream to HGLOBAL
						var hGlobal = ReadIStreamToHGlobal(medium.u.pstm);
						mediumsToDispose.Add(medium);
						if (hGlobal.HasValue)
						{
							allocatedHGlobals.Add(hGlobal.Value);
						}
						return hGlobal;
					}
				}

				// Try FILE if IStream failed
				if (((TYMED)formatEtc.tymed & TYMED.TYMED_FILE) != 0)
				{
					preferredFormatEtc = formatEtc with { tymed = (uint)TYMED.TYMED_FILE };
					getDataResult = dataObject->GetData(preferredFormatEtc, out medium);

					if (getDataResult.Succeeded && medium.tymed == TYMED.TYMED_FILE)
					{
						// Convert file path to HGLOBAL containing file contents
						var hGlobal = ReadFileToHGlobal(medium.u.lpszFileName);
						mediumsToDispose.Add(medium);
						if (hGlobal.HasValue)
						{
							allocatedHGlobals.Add(hGlobal.Value);
						}
						return hGlobal;
					}
				}

				if (getDataResult.Failed)
				{
					typeof(Win32DragDropExtension).LogError()?.Error($"GetData failed for format {format}: {Win32Helper.GetErrorMessage(getDataResult)}");
					return null;
				}
			}

			mediumsToDispose.Add(medium);
			return medium.u.hGlobal;
		});

		// Create DragUI for visual feedback during drag operation
		var dragUI = CreateDragUIForExternalDrag(dataObject, formatEtcList);

		// DROPEFFECT and DataPackageOperation have the same binary representation
		var info = new CoreDragInfo(src, package.GetView(), (DataPackageOperation)(*pdwEffect), dragUI);
		_coreDragDropManager.DragStarted(info);

		*pdwEffect = (DROPEFFECT)_manager.ProcessMoved(src);

		return HRESULT.S_OK;
	}

	unsafe HRESULT IDropTarget.Interface.DragOver(MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		var position = new System.Drawing.Point(pt.x, pt.y);
		var success = PInvoke.ScreenToClient(_hwnd, ref position);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}"); }
		var scaledPosition = GetScaledPosition(position.X, position.Y);
		var src = new DragEventSource(scaledPosition, grfKeyState);

		this.LogTrace()?.Trace($"{nameof(IDropTarget.Interface.DragOver)} @ {position}");

		*pdwEffect = (DROPEFFECT)_manager.ProcessMoved(src);

		return HRESULT.S_OK;
	}

	HRESULT IDropTarget.Interface.DragLeave()
	{
		this.LogTrace()?.Trace($"{nameof(IDropTarget.Interface.DragLeave)}");

		_manager.ProcessAborted(_fakePointerId);

		return HRESULT.S_OK;
	}

	unsafe HRESULT IDropTarget.Interface.Drop(IDataObject* dataObject, MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		var position = new System.Drawing.Point(pt.x, pt.y);
		var success = PInvoke.ScreenToClient(_hwnd, ref position);
		if (!success) { this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}"); }
		var scaledPosition = GetScaledPosition(position.X, position.Y);
		var src = new DragEventSource(scaledPosition, grfKeyState);

		this.LogTrace()?.Trace($"{nameof(IDropTarget.Interface.Drop)} @ {position}");

		*pdwEffect = (DROPEFFECT)_manager.ProcessReleased(src);

		return HRESULT.S_OK;
	}

	private Point GetScaledPosition(float x, float y)
	{
		var xamlRoot = _manager.ContentRoot.GetOrCreateXamlRoot();
		return new Point(x / xamlRoot.RasterizationScale, y / xamlRoot.RasterizationScale);
	}

	/// <summary>
	/// Reads data from an IStream and returns it as an HGLOBAL.
	/// </summary>
	private static unsafe HGLOBAL? ReadIStreamToHGlobal(IStream* pStream)
	{
		if (pStream is null)
		{
			return null;
		}

		try
		{
			// Get the stream size using STATFLAG_NONAME (0x1) to skip the name
			STATSTG stat;
			var hResult = pStream->Stat(&stat, 0x1);
			if (hResult.Failed)
			{
				typeof(Win32DragDropExtension).LogError()?.Error($"IStream.Stat failed: {Win32Helper.GetErrorMessage(hResult)}");
				return null;
			}

			var streamSize = (int)stat.cbSize;
			if (streamSize <= 0)
			{
				return null;
			}

			// Allocate global memory
			var hGlobal = PInvoke.GlobalAlloc(Windows.Win32.System.Memory.GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (nuint)streamSize);
			if (hGlobal == IntPtr.Zero)
			{
				typeof(Win32DragDropExtension).LogError()?.Error($"GlobalAlloc failed: {Win32Helper.GetErrorMessage()}");
				return null;
			}

			var pBuffer = PInvoke.GlobalLock(hGlobal);
			if (pBuffer is null)
			{
				PInvoke.GlobalFree(hGlobal);
				typeof(Win32DragDropExtension).LogError()?.Error($"GlobalLock failed: {Win32Helper.GetErrorMessage()}");
				return null;
			}

			try
			{
				// Seek to the beginning of the stream (STREAM_SEEK_SET = 0)
				const int STREAM_SEEK_SET = 0;
				pStream->Seek(0, STREAM_SEEK_SET, null);

				// Read the stream data
				uint bytesRead;
				hResult = pStream->Read(pBuffer, (uint)streamSize, &bytesRead);
				if (hResult.Failed)
				{
					PInvoke.GlobalUnlock(hGlobal);
					PInvoke.GlobalFree(hGlobal);
					typeof(Win32DragDropExtension).LogError()?.Error($"IStream.Read failed: {Win32Helper.GetErrorMessage(hResult)}");
					return null;
				}

				return (HGLOBAL)hGlobal;
			}
			finally
			{
				PInvoke.GlobalUnlock(hGlobal);
			}
		}
		catch (Exception ex)
		{
			typeof(Win32DragDropExtension).LogError()?.Error($"ReadIStreamToHGlobal failed: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Reads a file from the given path and returns its contents as an HGLOBAL.
	/// </summary>
	private static unsafe HGLOBAL? ReadFileToHGlobal(PCWSTR filePath)
	{
		if (filePath.Value is null)
		{
			return null;
		}

		try
		{
			var path = filePath.ToString();
			if (string.IsNullOrEmpty(path) || !File.Exists(path))
			{
				typeof(Win32DragDropExtension).LogError()?.Error($"ReadFileToHGlobal: File does not exist: {path}");
				return null;
			}

			var fileBytes = File.ReadAllBytes(path);
			if (fileBytes.Length == 0)
			{
				return null;
			}

			// Allocate global memory
			var hGlobal = PInvoke.GlobalAlloc(Windows.Win32.System.Memory.GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (nuint)fileBytes.Length);
			if (hGlobal == IntPtr.Zero)
			{
				typeof(Win32DragDropExtension).LogError()?.Error($"GlobalAlloc failed: {Win32Helper.GetErrorMessage()}");
				return null;
			}

			var pBuffer = PInvoke.GlobalLock(hGlobal);
			if (pBuffer is null)
			{
				PInvoke.GlobalFree(hGlobal);
				typeof(Win32DragDropExtension).LogError()?.Error($"GlobalLock failed: {Win32Helper.GetErrorMessage()}");
				return null;
			}

			try
			{
				Marshal.Copy(fileBytes, 0, (IntPtr)pBuffer, fileBytes.Length);
				return (HGLOBAL)hGlobal;
			}
			finally
			{
				PInvoke.GlobalUnlock(hGlobal);
			}
		}
		catch (Exception ex)
		{
			typeof(Win32DragDropExtension).LogError()?.Error($"ReadFileToHGlobal failed: {ex.Message}");
			return null;
		}
	}
}
