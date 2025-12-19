using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;
using Uno.Disposables;
using Uno.Foundation.Logging;
using IDataObject = Windows.Win32.System.Com.IDataObject;

namespace Uno.UI.Runtime.Skia.Win32;

// IDropTarget implementation for handling drag-and-drop operations
internal partial class Win32DragDropExtension
{
	unsafe HRESULT IDropTarget.Interface.DragEnter(IDataObject* dataObject, MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		Debug.Assert(_manager is not null && _coreDragDropManager is not null);

		var position = new System.Drawing.Point(pt.x, pt.y);

		var success = PInvoke.ScreenToClient(_hwnd, ref position);
		if (!success)
		{
			this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}");
		}
		var scaledPosition = GetScaledPosition(position.X, position.Y);

		var src = new DragEventSource(scaledPosition, grfKeyState);

		var formats = EnumerateFormats(dataObject);
		if (formats is null)
		{
			return HRESULT.E_UNEXPECTED;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			var log = $"{nameof(IDropTarget.Interface.DragEnter)} @ {position}, formats: ";
			foreach (var format in formats)
			{
				log += $"'{GetClipboardFormatDisplayName(format.cfFormat)}',";
			}
			this.Log().Trace(log[..^1]);
		}

		var package = new DataPackage();

		var formatEtcList = formats.ToArray();
		var formatList =
			formatEtcList
			.Where(static formatetc =>
			{
				if (!Enum.IsDefined((CLIPBOARD_FORMAT)formatetc.cfFormat))
				{
					return false;
				}
				if (formatetc.tymed != (uint)TYMED.TYMED_HGLOBAL)
				{
					typeof(Win32DragDropExtension).LogWarn()?.Warn(
						$"{nameof(IDropTarget.Interface.DragEnter)} found {Enum.GetName((CLIPBOARD_FORMAT)formatetc.cfFormat)}, " +
						$"but {nameof(TYMED)} is not {nameof(TYMED.TYMED_HGLOBAL)} and is not yet supported.");
					return false;
				}

				return true;
			})
			.Select(f => (CLIPBOARD_FORMAT)f.cfFormat)
			.ToList();

		var mediumsToDispose = new List<STGMEDIUM>();
		using var mediumsDisposable = new DisposableStruct<List<STGMEDIUM>>(static list =>
		{
			foreach (var medium in list)
			{
				PInvoke.ReleaseStgMedium(&medium);
			}
		}, mediumsToDispose);

		var handled = TryhandleAsyncHDrop(dataObject, formatEtcList, package);
		Win32ClipboardExtension.ReadContentIntoPackage(package, formatList, format =>
		{
			var formatEtc = formatEtcList.First(f => f.cfFormat == (int)format);
			if (formatEtc.cfFormat == (int)CLIPBOARD_FORMAT.CF_HDROP && handled)
			{
				return null;
			}
			dataObject->GetData(formatEtc, out STGMEDIUM medium);
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
		if (!success)
		{
			this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}");
		}
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
		_lastAsyncHDropHandler = null;

		return HRESULT.S_OK;
	}

	unsafe HRESULT IDropTarget.Interface.Drop(IDataObject* dataObject, MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		var position = new System.Drawing.Point(pt.x, pt.y);
		var success = PInvoke.ScreenToClient(_hwnd, ref position);
		if (!success)
		{
			this.LogError()?.Error($"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}");
		}
		var scaledPosition = GetScaledPosition(position.X, position.Y);
		var src = new DragEventSource(scaledPosition, grfKeyState);

		this.LogTrace()?.Trace($"{nameof(IDropTarget.Interface.Drop)} @ {position}");

		_lastAsyncHDropHandler?.Drop(dataObject);
		*pdwEffect = (DROPEFFECT)_manager.ProcessReleased(src);
		if (_lastAsyncHDropHandler != null)
		{
			_lastAsyncHDropHandler.DropEffect = *pdwEffect;
			_lastAsyncHDropHandler = null;
		}

		return HRESULT.S_OK;
	}

	private Point GetScaledPosition(float x, float y)
	{
		var xamlRoot = _manager.ContentRoot.GetOrCreateXamlRoot();
		return new Point(x / xamlRoot.RasterizationScale, y / xamlRoot.RasterizationScale);
	}

	private static unsafe string GetClipboardFormatDisplayName(ushort formatId)
	{
		if (Enum.IsDefined(typeof(CLIPBOARD_FORMAT), (CLIPBOARD_FORMAT)formatId))
		{
			return Enum.GetName(typeof(CLIPBOARD_FORMAT), (CLIPBOARD_FORMAT)formatId)!;
		}

		Span<char> buffer = stackalloc char[256];
		fixed (char* bufferPtr = buffer)
		{
			var length = PInvoke.GetClipboardFormatName(formatId, bufferPtr, buffer.Length);
			if (length > 0)
			{
				return new string(buffer[..length]);
			}
		}

		return $"0x{formatId:X4}";
	}

	private unsafe FORMATETC[]? EnumerateFormats(IDataObject* dataObject)
	{
		IEnumFORMATETC* enumFormatEtc;
		var hResult = dataObject->EnumFormatEtc((uint)DATADIR.DATADIR_GET, &enumFormatEtc);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IDataObject.EnumFormatEtc)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return null;
		}

		using var enumFormatDisposable = new DisposableStruct<IntPtr>(static p => ((IEnumFORMATETC*)p)->Release(), (IntPtr)enumFormatEtc);

		enumFormatEtc->Reset();
		const int formatBufferLength = 100;
		var formatBuffer = stackalloc FORMATETC[formatBufferLength];
		uint fetchedFormatCount;
		hResult = enumFormatEtc->Next(formatBufferLength, formatBuffer, &fetchedFormatCount);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(IEnumFORMATETC.Next)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return null;
		}

		var formats = new Span<FORMATETC>(formatBuffer, (int)fetchedFormatCount);
		return formats.ToArray();
	}

	private unsafe bool TryhandleAsyncHDrop(IDataObject* dataObject, FORMATETC[] formatEtcList, DataPackage package)
	{
		var formatEtcNullable = formatEtcList.Cast<FORMATETC?>().FirstOrDefault(f => f!.Value.cfFormat == (int)CLIPBOARD_FORMAT.CF_HDROP, null);
		if (formatEtcNullable is null)
		{
			return false;
		}
		var formatEtc = formatEtcNullable.Value;

		using ComScope<IUnknown> asyncCapabilityScope = new(null);
		HRESULT hResult3;
		fixed (Guid* guidPtr = &_asyncCapabilityGuid)
		{
			hResult3 = dataObject->QueryInterface(guidPtr, asyncCapabilityScope);
		}
		if (!hResult3.Succeeded || !((IDataObjectAsyncCapability*)asyncCapabilityScope.Value)->GetAsyncMode(out var isAsync).Succeeded || !isAsync)
		{
			return false;
		}

		var asyncHDropHandler = new AsyncHDropHandler(formatEtc);
		_lastAsyncHDropHandler = asyncHDropHandler;
		package.SetDataProvider(StandardDataFormats.StorageItems, ct => DelayRenderer(ct, asyncHDropHandler));
		return true;
	}

	private async Task<object> DelayRenderer(CancellationToken ct, AsyncHDropHandler asyncHDropHandler)
	{
		if (asyncHDropHandler is null)
		{
			throw new InvalidOperationException("Async HDrop handler is called too late.");
		}

		return await asyncHDropHandler.Task;
	}
}
