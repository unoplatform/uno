using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Ole;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
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
	private static readonly long _fakePointerId = Pointer.CreateUniqueIdForUnknownPointer();

	private readonly DragDropManager _manager;
	private readonly CoreDragDropManager _coreDragDropManager;
	private readonly HWND _hwnd;
	private readonly ComScope<IDropTarget> _dropTarget;

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
	}

	~Win32DragDropExtension()
	{
		_dropTarget.Dispose();
	}

	public void StartNativeDrag(CoreDragInfo info, Action<DataPackageOperation> action) => throw new NotImplementedException();
}

