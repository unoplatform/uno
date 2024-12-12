using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Windows.Win32.System.SystemServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using IDataObject = Windows.Win32.System.Com.IDataObject;

namespace Uno.UI.Runtime.Skia.Win32;

internal class Win32DragDropExtension : IDragDropExtension, IDropTarget
{
	private static readonly long _fakePointerId = Pointer.CreateUniqueIdForUnknownPointer();

	private readonly DragDropManager _manager;
	private readonly CoreDragDropManager _coreDragDropManager;
	private readonly HWND _hwnd;

	public Win32DragDropExtension(DragDropManager manager)
	{
		var host = Win32WindowWrapper.XamlRootMap.GetHostForRoot(manager.ContentRoot.GetOrCreateXamlRoot()) ?? throw new InvalidOperationException($"Couldn't find an {nameof(Win32WindowWrapper)} instance associated with this {nameof(XamlRoot)}.");
		_coreDragDropManager = XamlRoot.GetCoreDragDropManager(((IXamlRootHost)host).RootElement!.XamlRoot);
		_manager = manager;

		_hwnd = (HWND)host.NativeWindow;
		var hResult = PInvoke.RegisterDragDrop(_hwnd, this);
		if (hResult.Failed)
		{
			this.Log().Log(LogLevel.Error, hResult, static hResult => $"{nameof(PInvoke.RegisterDragDrop)} failed: {Win32Helper.GetErrorMessage(hResult)}");
		}
	}

	public unsafe void DragEnter(IDataObject pDataObj, MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		Debug.Assert(_manager is not null && _coreDragDropManager is not null);

		pDataObj.EnumFormatEtc((uint)DATADIR.DATADIR_GET, out var enumFormatEtc);
		if (enumFormatEtc is null)
		{
			this.Log().Log(LogLevel.Error, static () => $"{nameof(pDataObj.EnumFormatEtc)} returned null");
			return;
		}

		enumFormatEtc.Reset();
		var formatBuffer = stackalloc FORMATETC[100];
		uint fetchedFormatCount;
		var hResult = enumFormatEtc.Next(new Span<FORMATETC>(formatBuffer, 100), &fetchedFormatCount);
		if (hResult.Failed)
		{
			this.Log().Log(LogLevel.Error, hResult, static hResult => $"{nameof(PInvoke.RegisterDragDrop)} failed: {Win32Helper.GetErrorMessage(hResult)}");
			return;
		}

		var position = new System.Drawing.Point(pt.x, pt.y);
		_ = PInvoke.ScreenToClient(_hwnd, ref position) || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}");
		var src = new DragEventSource(position.X, position.Y, grfKeyState);

		var formats = new Span<FORMATETC>(formatBuffer, (int)fetchedFormatCount);
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			var log = $"{nameof(DragEnter)} @ {position}, formats: ";
			foreach (var format in formats)
			{
				log += (CLIPBOARD_FORMAT)format.cfFormat + " ";
			}
			this.Log().Trace(log);
		}

		var package = new DataPackage();

		var formatEtcList = formats.ToArray();
		var formatList =
			formatEtcList
			.Where(static formatetc =>
			{
				if (!Enum.IsDefined(typeof(CLIPBOARD_FORMAT), formatetc.cfFormat))
				{
					return false;
				}
				if (formatetc.tymed != (uint)TYMED.TYMED_HGLOBAL)
				{
					typeof(Win32DragDropExtension).Log().Log(LogLevel.Error, formatetc, static formatetc => $"{nameof(DragEnter)} found {Enum.GetName(typeof(CLIPBOARD_FORMAT), formatetc.cfFormat)}, but {nameof(TYMED)} is not {nameof(TYMED.TYMED_HGLOBAL)}");
					return false;
				}

				return true;
			})
			.Select(f => (CLIPBOARD_FORMAT)f.cfFormat)
			.ToList();

		Win32ClipboardExtension.ReadContentIntoPackage(package, formatList, format =>
		{
			var formatEtc = formatEtcList.First(f => f.cfFormat == (int)format);
			pDataObj.GetData(formatEtc, out STGMEDIUM medium);
			return medium.u.hGlobal;
		});


		// DROPEFFECT and DataPackageOperation have the same binary representation
		var info = new CoreDragInfo(src, package.GetView(), (DataPackageOperation)(*pdwEffect));
		_coreDragDropManager.DragStarted(info);

		var resetEvent = new AutoResetEvent(false);
		NativeDispatcher.Main.Enqueue(() =>
		{
			*pdwEffect = (DROPEFFECT)_manager.ProcessMoved(src);
			resetEvent.Set();
		});
		resetEvent.WaitOne();
	}

	public unsafe void DragOver(MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		var position = new System.Drawing.Point(pt.x, pt.y);
		_ = PInvoke.ScreenToClient(_hwnd, ref position) || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}");
		var src = new DragEventSource(position.X, position.Y, grfKeyState);

		this.Log().Log(LogLevel.Trace, position, static position => $"{nameof(DragOver)} @ {position}");

		var resetEvent = new AutoResetEvent(false);
		NativeDispatcher.Main.Enqueue(() =>
		{
			*pdwEffect = (DROPEFFECT)_manager.ProcessMoved(src);
			resetEvent.Set();
		});
		resetEvent.WaitOne();
	}

	public void DragLeave()
	{
		this.Log().Log(LogLevel.Trace, static () => $"{nameof(DragLeave)}");

		var resetEvent = new AutoResetEvent(false);
		NativeDispatcher.Main.Enqueue(() =>
		{
			_manager.ProcessAborted(_fakePointerId);
			resetEvent.Set();
		});
		resetEvent.WaitOne();
	}

	public unsafe void Drop(IDataObject pDataObj, MODIFIERKEYS_FLAGS grfKeyState, POINTL pt, DROPEFFECT* pdwEffect)
	{
		var position = new System.Drawing.Point(pt.x, pt.y);
		_ = PInvoke.ScreenToClient(_hwnd, ref position) || this.Log().Log(LogLevel.Error, static () => $"{nameof(PInvoke.ScreenToClient)} failed: {Win32Helper.GetErrorMessage()}");
		var src = new DragEventSource(position.X, position.Y, grfKeyState);

		this.Log().Log(LogLevel.Trace, position, static position => $"{nameof(Drop)} @ {position}");

		var resetEvent = new AutoResetEvent(false);
		NativeDispatcher.Main.Enqueue(() =>
		{
			*pdwEffect = (DROPEFFECT)_manager.ProcessReleased(src);
			resetEvent.Set();
		});
		resetEvent.WaitOne();
	}

	public void StartNativeDrag(CoreDragInfo info) => throw new System.NotImplementedException();

	private readonly struct DragEventSource(int x, int y, MODIFIERKEYS_FLAGS modifierFlags) : IDragEventSource
	{
		private static long _nextFrameId;
		private readonly Point _location = new(x, y);

		public long Id => _fakePointerId;

		public uint FrameId { get; } = (uint)Interlocked.Increment(ref _nextFrameId);

		/// <inheritdoc />
		public (Point location, DragDropModifiers modifier) GetState()
		{
			var flags = DragDropModifiers.None;
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_SHIFT) != 0)
			{
				flags |= DragDropModifiers.Shift;
			}
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_CONTROL) != 0)
			{
				flags |= DragDropModifiers.Control;
			}
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_LBUTTON) != 0)
			{
				flags |= DragDropModifiers.LeftButton;
			}
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_RBUTTON) != 0)
			{
				flags |= DragDropModifiers.RightButton;
			}
			if ((modifierFlags & MODIFIERKEYS_FLAGS.MK_MBUTTON) != 0)
			{
				flags |= DragDropModifiers.MiddleButton;
			}
			return (_location, flags);
		}

		/// <inheritdoc />
		public Point GetPosition(object? relativeTo)
		{
			if (relativeTo is null)
			{
				return _location;
			}

			if (relativeTo is UIElement elt)
			{
				var eltToRoot = UIElement.GetTransform(elt, null);
				var rootToElt = eltToRoot.Inverse();

				return rootToElt.Transform(_location);
			}

			throw new InvalidOperationException("The relative to must be a UIElement.");
		}
	}
}
