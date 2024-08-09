// Copyright Edward Rosten 2006--2013.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE AUTHOR AND OTHER CONTRIBUTORS ``AS IS''
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED.  IN NO EVENT SHALL THE AUTHOR OR OTHER CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

// https://github.com/edrosten/x_clipboard/blob/164edd5d530cca12f65f99e22741d9b652e5bc43/paste.cc

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11DragDropExtension : IDragDropExtension
{
	private static readonly long _fakePointerId = Pointer.CreateUniqueIdForUnknownPointer();

	private readonly X11XamlRootHost _host;
	private readonly CoreDragDropManager _coreDragDropManager;
	private readonly DragDropManager _manager;
	private XdndSession? _currentSession;

	private IntPtr XdndSelection => X11Helper.GetAtom(_host.RootX11Window.Display, X11Helper.XdndSelection);

	public X11DragDropExtension(DragDropManager manager)
	{
		if (manager.ContentRoot.GetOrCreateXamlRoot().HostWindow is not { } window)
		{
			throw new InvalidOperationException($"Couldn't find a window associated with the {nameof(X11DragDropExtension)}");
		}
		_host = X11XamlRootHost.GetHostFromWindow(window) ?? throw new InvalidOperationException($"Couldn't find an {nameof(X11XamlRootHost)} associated with the {nameof(X11DragDropExtension)}");
		_manager = manager;
		_coreDragDropManager = XamlRoot.GetCoreDragDropManager(((IXamlRootHost)_host).RootElement!.XamlRoot);

		var display = _host.RootX11Window.Display;
		using var lockDiposable = X11Helper.XLock(display);
		_ = XLib.XChangeProperty(
			display,
			_host.RootX11Window.Window,
			X11Helper.GetAtom(display, X11Helper.XdndAware),
			X11Helper.GetAtom(display, X11Helper.XA_ATOM),
			32,
			PropertyMode.Replace,
			new byte[] { 5 }, // version 5
			1);
		_ = XLib.XFlush(display);

		_host.SetDragDropExtension(this);
	}

	public void ProcessXdndMessage(XClientMessageEvent ev)
	{
		using var lockDiposable = X11Helper.XLock(_host.RootX11Window.Display);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"XDnd EVENT for window {ev.window}: message_type={XLib.GetAtomName(_host.RootX11Window.Display, ev.message_type)} data: {ev.ptr1.ToString("X", CultureInfo.InvariantCulture)}, {ev.ptr2.ToString("X", CultureInfo.InvariantCulture)}, {ev.ptr3.ToString("X", CultureInfo.InvariantCulture)}, {ev.ptr4.ToString("X", CultureInfo.InvariantCulture)}, {ev.ptr5.ToString("X", CultureInfo.InvariantCulture)}");
		}

		if (ev.message_type == X11Helper.GetAtom(_host.RootX11Window.Display, X11Helper.XdndEnter))
		{
			ProcessXdndEnter(ev);
		}
		else if (ev.message_type == X11Helper.GetAtom(_host.RootX11Window.Display, X11Helper.XdndPosition))
		{
			ProcessXdndPosition(ev);
		}
		else if (ev.message_type == X11Helper.GetAtom(_host.RootX11Window.Display, X11Helper.XdndLeave))
		{
			ProcessXdndLeave(ev);
		}
		else if (ev.message_type == X11Helper.GetAtom(_host.RootX11Window.Display, X11Helper.XdndDrop))
		{
			ProcessXdndDrop(ev);
		}
		else
		{
			throw new ArgumentException($"{nameof(ProcessXdndMessage)} only accepts XDnD messages.");
		}
	}

	private void ProcessXdndEnter(XClientMessageEvent ev)
	{
		var sourceWindow = ev.ptr1;
		var version = ev.ptr2 >> 24;

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"XDndEnter: version={version}, sourceWindow={sourceWindow}, types in message: [{XLib.GetAtomName(_host.RootX11Window.Display, ev.ptr3)}, {XLib.GetAtomName(_host.RootX11Window.Display, ev.ptr4)}, {XLib.GetAtomName(_host.RootX11Window.Display, ev.ptr5)}]");
		}

		var moreThan3Types = ev.ptr2 & 1;

		var types = moreThan3Types == IntPtr.Zero ?
			new[] { ev.ptr3, ev.ptr4, ev.ptr5 } :
			X11ClipboardExtension.WaitForFormats(_host.RootX11Window, XdndSelection);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"XDndEnter: total types received {types.Select(t => XLib.GetAtomName(_host.RootX11Window.Display, t)).ToList()}");
		}

		_currentSession = new XdndSession(version, sourceWindow, types, false, null, null, null);
	}

	private void ProcessXdndPosition(XClientMessageEvent ev)
	{
		if (_currentSession is null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Received a XdndPosition message without a XdndEnter preceding it. Ignoring.");
			}

			return;
		}

		var display = _host.RootX11Window.Display;

		var sourceWindow = ev.ptr1;
		var rootX = (int)(ev.ptr3 >> 16);
		var rootY = (int)(ev.ptr3 & 0xffff);

		_ = XLib.XQueryTree(display, XLib.XDefaultRootWindow(display), out IntPtr root, out _, out _, out _);
		XWindowAttributes windowAttrs = default;
		_ = XLib.XGetWindowAttributes(display, _host.RootX11Window.Window, ref windowAttrs);
		XLib.XTranslateCoordinates(display, root, _host.RootX11Window.Window, rootX, rootY, out var x, out var y, out _);

		if (!_currentSession.Value.EnterFired)
		{
			// Note how we synchronously retrieve and cache the data, unlike copying/pasting from CLIPBOARD, which asynchronously gets the data only when used.
			var package = new DataPackage();
			var formats = _currentSession.Value.AvailableFormats;
			if (formats.FirstOrDefault(f => X11ClipboardExtension.TextFormats.ContainsKey(XLib.GetAtomName(display, f))) is var f2 && f2 != IntPtr.Zero)
			{
				package.SetText(X11ClipboardExtension.WaitForText(_host.RootX11Window, f2, XdndSelection));
			}

			// TODO: other operations
			var operations = DataPackageOperation.Copy;

			var src = new DragEventSource(x, y);
			var info = new CoreDragInfo(src, package.GetView(), operations);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"XDndPosition: first position event, firing DragStarted with available operations {operations}. Found available formats {formats.Select(f => XLib.GetAtomName(display, f)).ToList()}.");
			}

			_coreDragDropManager.DragStarted(info);
			// Note: No needs to _manager.ProcessMove, the DragStarted will actually have the same effect

			_currentSession = _currentSession.Value with { EnterFired = true, Package = package, Operations = operations, LastPosition = new Point(x, y) };
		}

		_currentSession = _currentSession.Value with { LastPosition = new Point(x, y) };

		var acceptedOperations = _manager.ProcessMoved(new DragEventSource(x, y));

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"XDndPosition: sent ProcessMoved({x}, {y}) to DragDropManager: acceptedOperations={acceptedOperations}.");
		}

		XClientMessageEvent m = default;
		m.type = XEventName.ClientMessage;
		m.display = display;
		m.window = sourceWindow;
		m.message_type = X11Helper.GetAtom(display, X11Helper.XdndStatus);
		m.format = 32;
		m.ptr1 = _host.RootX11Window.Window;
		m.ptr2 = acceptedOperations != DataPackageOperation.None ? 1 : 0;
		// This is an optimization mechanism that tells Xdnd not to send new XdndPosition events until the pointer exits the widget it's in.
		// We skip this with an empty rectangle.
		m.ptr3 = 0;
		m.ptr4 = 0;
		m.ptr5 = X11Helper.GetAtom(display, X11Helper.XdndActionCopy); // TODO: support other actions and choose action from acceptedOperations

		XEvent xev = default;
		xev.ClientMessageEvent = m;
		_ = XLib.XSendEvent(display, ev.ptr1, false, IntPtr.Zero /* NoEventMask */, ref xev);
		_ = XLib.XFlush(display);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"XDndPosition: responded with XdndStatus message.");
		}
	}

	private void ProcessXdndLeave(XClientMessageEvent ev)
	{
		var pos = _currentSession!.Value.LastPosition!.Value;
		_manager.ProcessAborted(_fakePointerId);
		_currentSession = null;

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"XDndLeave: aborted current dragging session.");
		}
	}

	private void ProcessXdndDrop(XClientMessageEvent ev)
	{
		var pos = _currentSession!.Value.LastPosition!.Value;
		var acceptedOperation = _manager.ProcessDropped(new DragEventSource((int)pos.X, (int)pos.Y));
		_currentSession = null;

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"XDndDrop: called DragDropManager.ProcessDropped and received acceptedOperation={acceptedOperation}.");
		}

		var sourceWindow = ev.ptr1;

		var display = _host.RootX11Window.Display;

		// We already cached the data, so no need to first retrieve it. We directly send XdndFinished
		XClientMessageEvent m = default;
		m.type = XEventName.ClientMessage;
		m.display = display;
		m.window = sourceWindow;
		m.message_type = X11Helper.GetAtom(display, X11Helper.XdndFinished);
		m.format = 32;
		m.ptr1 = _host.RootX11Window.Window;
		m.ptr2 = 0;
		// TODO: support other actions and read from acceptedOperation
		m.ptr3 = acceptedOperation is not DataPackageOperation.None ? X11Helper.GetAtom(display, X11Helper.XdndActionCopy) : X11Helper.None;

		XEvent xev = default;
		xev.ClientMessageEvent = m;
		_ = XLib.XSendEvent(display, ev.ptr1, false, IntPtr.Zero /* NoEventMask */, ref xev);
		_ = XLib.XFlush(display);

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"XDndDrop: responded with XdndFinished.");
		}
	}

	// TODO: uno-to-outside dragging
	public void StartNativeDrag(CoreDragInfo info) => throw new System.NotImplementedException();

	private class DragEventSource(int x, int y) : IDragEventSource
	{
		private static long _nextFrameId;
		private readonly Point _location = new Point(x, y);

		public long Id => _fakePointerId;

		public uint FrameId { get; } = (uint)Interlocked.Increment(ref _nextFrameId);

		/// <inheritdoc />
		public (Point location, DragDropModifiers modifier) GetState() => (_location, DragDropModifiers.None);

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

	// From the spec: "If (the target window) retrieved the data, it should cache it so it does not need to be retrieved again when the actual drop occurs.
	// XdndEnter doesn't provide pointer coords, so we fire DragEntered with the first XdndPosition that comes after XdndEnter
	// We store the last position because XdndLeave doesn't send coordinates
	private record struct XdndSession(IntPtr Version, IntPtr SourceWindow, IntPtr[] AvailableFormats, bool EnterFired, DataPackage? Package, DataPackageOperation? Operations, Point? LastPosition);
}
