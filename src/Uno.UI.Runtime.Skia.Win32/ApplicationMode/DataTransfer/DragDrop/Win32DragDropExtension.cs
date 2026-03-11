using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Windows.Win32.UI.Shell;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.NativeElementHosting;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Win32 implementation of drag-and-drop functionality.
/// This partial class is split across multiple files:
/// - Win32DragDropExtension.cs (this file): Core class, constructor, and infrastructure
/// - Win32DragDropExtension.DropTarget.cs: IDropTarget interface implementation
/// - Win32DragDropExtension.DragUI.cs: DragUI creation for external drags
/// - Win32DragDropExtension.ImageHelpers.cs: Image/icon extraction and conversion
/// - Win32DragDropExtension.PngHelpers.cs: PNG encoding utilities
/// - Win32DragDropExtension.DragEventSource.cs: DragEventSource struct
/// </summary>
internal partial class Win32DragDropExtension : IDragDropExtension, IDropTarget.Interface
{
	private static readonly Guid _asyncCapabilityGuid = new Guid(0x3D8B0590, 0xF691, 0x11D2, 0x8E, 0xA9, 0x00, 0x60, 0x97, 0xDF, 0x5B, 0xD4);
	private static readonly long _fakePointerId = Pointer.CreateUniqueIdForUnknownPointer();

	private readonly DragDropManager _manager;
	private readonly CoreDragDropManager _coreDragDropManager;
	private readonly HWND _hwnd;
	private readonly ComScope<IDropTarget> _dropTarget;

	private AsyncHDropHandler? _lastAsyncHDropHandler;

	private readonly uint CFSTR_FILEDESCRIPTOR;
	private readonly uint CFSTR_FILECONTENTS;

	public unsafe Win32DragDropExtension(DragDropManager manager)
	{
		var host = XamlRootMap.GetHostForRoot(manager.ContentRoot.GetOrCreateXamlRoot()) as Win32WindowWrapper ?? throw new InvalidOperationException($"Couldn't find an {nameof(Win32WindowWrapper)} instance associated with this {nameof(XamlRoot)}.");
		_coreDragDropManager = XamlRoot.GetCoreDragDropManager(((IXamlRootHost)host).RootElement!.XamlRoot);
		_manager = manager;
		_hwnd = (HWND)((Win32NativeWindow)host.NativeWindow).Hwnd;

		// Note: we're deliberately not disposing the ComScope (which calls ReleaseRef()) here because the IDragDropExtension instance
		// should last as long as the window that created it.
		_dropTarget = ComHelpers.TryGetComScope<IDropTarget>(this, out HRESULT hResult);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(ComHelpers.TryGetComScope)}<{nameof(IDropTarget)}> failed: {Win32Helper.GetErrorMessage(hResult)}");
			return;
		}

		// RegisterDragDrop calls AddRef()
		hResult = PInvoke.RegisterDragDrop(_hwnd, _dropTarget);
		if (hResult.Failed)
		{
			this.LogError()?.Error($"{nameof(PInvoke.RegisterDragDrop)} failed: {Win32Helper.GetErrorMessage(hResult)}");
		}

		if (CFSTR_FILEDESCRIPTOR is 0)
		{
			CFSTR_FILEDESCRIPTOR = PInvoke.RegisterClipboardFormat("FileGroupDescriptorW");
			if (CFSTR_FILEDESCRIPTOR is 0) { this.LogError()?.Error($"{nameof(PInvoke.RegisterClipboardFormat)} failed to register {nameof(CFSTR_FILEDESCRIPTOR)}: {Win32Helper.GetErrorMessage()}"); }
			CFSTR_FILECONTENTS = PInvoke.RegisterClipboardFormat("FileContents");
			if (CFSTR_FILECONTENTS is 0) { this.LogError()?.Error($"{nameof(PInvoke.RegisterClipboardFormat)} failed to register {nameof(CFSTR_FILECONTENTS)}: {Win32Helper.GetErrorMessage()}"); }
		}
	}

	~Win32DragDropExtension()
	{
		_dropTarget.Dispose();
	}

	public void StartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> action) => throw new NotImplementedException();

	private class AsyncHDropHandler(FORMATETC hdropFormat)
	{
		private readonly TaskCompletionSource<List<IStorageItem>> _tcs = new();

		public Task<List<IStorageItem>> Task => _tcs.Task;

		public DROPEFFECT DropEffect { private get; set; } = DROPEFFECT.DROPEFFECT_NONE;

		public void Leave() => _tcs.SetException(new InvalidOperationException("Attempt to get file drop list after the dragging operation left the window."));

		public unsafe void Drop(IDataObject* dataObject)
		{
			ComScope<IDataObjectAsyncCapability> asyncCapabilityScope = new(null);
			var localGuid = _asyncCapabilityGuid;
			var hResult = dataObject->QueryInterface(&localGuid, asyncCapabilityScope);
			if (hResult.Failed)
			{
				_tcs.SetException(Marshal.GetExceptionForHR(hResult.Value) ?? new InvalidOperationException($"{nameof(IDataObject)}::{nameof(IDataObject.QueryInterface)} failed."));
			}
			else
			{
				var success = false;
				STGMEDIUM hdropMedium = default;
				var asyncCapability = asyncCapabilityScope.Value;
				var hResult2 = asyncCapability->StartOperation();
				if (hResult2.Succeeded)
				{
					var dispose = true;
					using var _ = Disposable.Create(() =>
					{
						if (dispose)
						{
							asyncCapability->EndOperation(HRESULT.S_OK, null, (uint)DropEffect);
							asyncCapabilityScope.Dispose();
						}
					});

					const int attempts = 100;
					for (int i = 0; i < attempts; i++)
					{
						if (dataObject->GetData(hdropFormat, out hdropMedium).Succeeded)
						{
							success = true;
							break;
						}
						Thread.Sleep(TimeSpan.FromMilliseconds(10));
					}

					if (!success)
					{
						_tcs.SetException(new InvalidOperationException($"Failed to retrieve HDROP data from IDataObject."));
					}
					else
					{
						dispose = false;
						new Thread(() =>
						{
							using var _2 = Disposable.Create(() =>
							{
								PInvoke.ReleaseStgMedium(ref hdropMedium);
								asyncCapability->EndOperation(HRESULT.S_OK, null, (uint)DropEffect);
								asyncCapabilityScope.Dispose();
							});

							var files = Win32ClipboardExtension.GetFileDropList(hdropMedium.u.hGlobal);
							if (files is null)
							{
								_tcs.SetException(new InvalidOperationException("Failed to retrieve file drop list from HDROP."));
							}
							else
							{
								_tcs.SetResult(files);
							}
						}).Start();
					}
				}
				else
				{
					_tcs.SetException(Marshal.GetExceptionForHR(hResult2.Value) ?? new InvalidOperationException($"{nameof(IDataObjectAsyncCapability)}::{nameof(IDataObjectAsyncCapability.StartOperation)} failed."));
				}
			}
		}
	}
}

