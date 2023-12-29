// This implementation has a lot of the structure of Avalonia's implementation
// with the actual logic ported from xsel

// The MIT License (MIT)
//
// Copyright (c) .NET Foundation and Contributors
// All Rights Reserved
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// Copyright (C) 2001 Conrad Parker <conrad@vergenet.net>
//
// Permission to use, copy, modify, distribute, and sell this software and its
// documentation for any purpose is hereby granted without fee, provided that
// the above copyright notice appear in all copies and that both that copyright
// notice and this permission notice appear in supporting documentation. No
// representations are made about the suitability of this software for any
// purpose. It is provided "as is" without express or implied warranty.


#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Avalonia.X11;
using SkiaSharp;
using Uno.ApplicationModel.DataTransfer;
using Uno.Disposables;
using Uno.Foundation.Logging;
using EventMask = Avalonia.X11.EventMask;
using PropertyMode = Avalonia.X11.PropertyMode;

namespace Uno.WinUI.Runtime.Skia.X11;

// https://specifications.freedesktop.org/clipboards-spec/clipboards-latest.txt
// https://www.jwz.org/doc/x-cut-and-paste.html
// https://jameshunt.us/writings/managing-the-x11-clipboard/
internal class X11ClipboardExtension : IClipboardExtension
{
	private const EventMask EVENT_MASK = EventMask.PropertyChangeMask;

	private readonly ImmutableList<IntPtr> _supportedAtoms;

	// TODO: fill these as we encounter new formats
	// Order from most to least preferable as the first available format will be used.
	private readonly ImmutableList<string> _imageFormats = ImmutableList.Create(
		"image/png",
		"image/jpeg",
		"image/bmp",
		"image/tiff",
		"image/avif",
		"image/ico");

	private readonly ImmutableDictionary<string, Encoding> _textFormats = new Dictionary<string, Encoding>
	{
		{ "UTF8_STRING", Encoding.UTF8 },
		{ "text/plain;charset=utf-8", Encoding.UTF8 },
		{ "UTF16_STRING", Encoding.Unicode },
		{ "text/plain;charset=utf-16", Encoding.Unicode },
		{ "XA_STRING", Encoding.ASCII },
		{ "OEMTEXT", Encoding.ASCII },
	}.ToImmutableDictionary();

	// We can mostly get away with having a single x11 window for selection events because Clipboard methods are only
	// allowed to be called when on the UI Thread, so no synchronization issues should be raised. Some questions
	// remain about having 2 different threads call DataPackageView.GetXXXAsync on the return value of GetContent().
	// In that case, we will get a data race (or probably a deadlock). Therefore, we add a window mutex for safety.
	// If this scenario is confirmed to be non-existent, we can remove the mutex.
	// This will need to be evaluated if we decide to give each window its own UI thread.
	private readonly X11Window _x11Window;
	private readonly object _windowMutex = new object();

	private int _currentlyOwningClipboard;
	// there are only valid if CurrentlyOwningClipboard
	private IntPtr _ownershipTimestamp;
	private DataPackageView _clipboard;

#pragma warning disable CS0067 // Event is never used
	public event EventHandler<object> ContentChanged;
#pragma warning restore CS0067 // Event is never used

	public static X11ClipboardExtension Instance { get; } = new X11ClipboardExtension();

	private X11ClipboardExtension()
	{
		IntPtr display = XLib.XOpenDisplay(IntPtr.Zero);
		if (display == IntPtr.Zero)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("XLIB ERROR: Cannot connect to X server");
			}
		}

		int screen = XLib.XDefaultScreen(display);

		IntPtr window = XLib.XCreateSimpleWindow(display, XLib.XRootWindow(display, screen), 0, 0, 1, 1, 0,
			XLib.XBlackPixel(display, screen), XLib.XWhitePixel(display, screen));

		XLib.XFlush(display); // unnecessary on most Xlib implementations

		// Note how we do NOT map the window, since we're only using it for events, not actually showing anything
		_x11Window = new X11Window(display, window);

		_supportedAtoms = ImmutableList.Create(
			X11Helper.GetAtom(_x11Window.Display, X11Helper.TIMESTAMP),
			X11Helper.GetAtom(_x11Window.Display, X11Helper.MULTIPLE),
			X11Helper.GetAtom(_x11Window.Display, X11Helper.TARGETS)
		);

		var selThread = new Thread(Run)
		{
			Name = "X11 Selection Ownership",
			IsBackground = true
		};

		selThread.Start();
	}

	private bool CurrentlyOwningClipboard
	{
		get => Interlocked.CompareExchange(ref _currentlyOwningClipboard, 1, 1) == 1;
		set
		{
			if (value)
			{
				Interlocked.CompareExchange(ref _currentlyOwningClipboard, 1, 0);
			}
			else
			{
				Interlocked.CompareExchange(ref _currentlyOwningClipboard, 0, 1);
			}
		}
	}

	public void StartContentChanged() => throw new NotImplementedException();
	public void StopContentChanged() => throw new NotImplementedException();

	public void Clear() => SetContent(new DataPackage());

	// TODO: do we need to do anything specific here?
	// X11 has the bizzare concept of a clipboard owner, where if the clipboard owner goes away,
	// the clipboard vanishes. In practice, the DE persists the clipboard even if the x11 client
	// that set it goes away. Most likely, the DE does something similar to xclipd, where it continuously
	// takes the ownership of the clipboard whenever it changes: https://github.com/jhunt/xclipd/blob/master/src/xclipd.c
	public void Flush() { }

	public void SetContent(DataPackage content)
	{
		lock (_windowMutex)
		{
			_ownershipTimestamp = /* CurrentTime */ IntPtr.Zero;
			// _ownershipTimestamp = GetTimestamp();
			XLib.XSetSelectionOwner(_x11Window.Display, X11Helper.GetAtom(_x11Window.Display, X11Helper.CLIPBOARD), _x11Window.Window, _ownershipTimestamp);

			if (XLib.XGetSelectionOwner(_x11Window.Display, X11Helper.GetAtom(_x11Window.Display, X11Helper.CLIPBOARD)) == _x11Window.Window)
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().LogInfo($"Successfully acquired x11 selection ownership");
				}

				// TODO: expand this
				_clipboard = content.GetView();
				CurrentlyOwningClipboard = true;
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Failed to acquire x11 selection");
				}

				CurrentlyOwningClipboard = false;
			}
		}
	}

	[DoesNotReturn]
	private void Run()
	{
		while (true)
		{
			SpinWait.SpinUntil(() =>
			{
				if (CurrentlyOwningClipboard)
				{
					return true;
				}
				lock (_windowMutex)
				{
					return X11Helper.XPending(_x11Window.Display) > 0;
				}
			});

			lock (_windowMutex)
			{
				XLib.XNextEvent(_x11Window.Display, out var event_);

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"XSEL EVENT: {event_.type}");
				}

				switch (event_.type)
				{
					case XEventName.SelectionClear:
						if (event_.SelectionEvent.selection == X11Helper.GetAtom(_x11Window.Display, X11Helper.CLIPBOARD))
						{
							if (this.Log().IsEnabled(LogLevel.Information))
							{
								this.Log().LogInfo($"Lost X11 selection ownership");
							}

							CurrentlyOwningClipboard = false;
						}
						else
						{
							if (this.Log().IsEnabled(LogLevel.Error))
							{
								this.Log().Trace($"Somehow losing X11 selection {XLib.GetAtomName(_x11Window.Display, event_.SelectionEvent.selection)}, even though we only acquire CLIPBOARD");
							}
						}
						break;
					case XEventName.SelectionRequest:
						if (event_.SelectionRequestEvent.selection != X11Helper.GetAtom(_x11Window.Display, X11Helper.CLIPBOARD))
						{
							if (this.Log().IsEnabled(LogLevel.Error))
							{
								this.Log().Trace($"Somehow getting a SelectionRequest for {XLib.GetAtomName(_x11Window.Display, event_.SelectionEvent.selection)}, even though we only acquire CLIPBOARD");
							}
							break;
						}

						var xsr = event_.SelectionRequestEvent;
						XSelectionEvent ev = default; // reply

						ev.type = XEventName.SelectionNotify;
						ev.display = xsr.display;
						ev.requestor = xsr.requestor;
						ev.selection = xsr.selection;
						ev.time = xsr.time;
						ev.target = xsr.target;

						if (this.Log().IsEnabled(LogLevel.Information))
						{
							this.Log().LogInfo($"Received SelectionRequest with target {XLib.GetAtomName(_x11Window.Display, ev.target)} and requestor {ev.requestor.ToString("X")}");
						}

						if (xsr.property == /* None */ IntPtr.Zero && ev.target != X11Helper.GetAtom(_x11Window.Display, X11Helper.MULTIPLE))
						{
							/* Obsolete requestor */
							xsr.property = xsr.target;
						}

						// To be ICCCM-compliant, we need to support TARGETS and MULTIPLE at the very least.

						if (ev.time != /* CurrentTime */ IntPtr.Zero && ev.time < _ownershipTimestamp)
						{
							/* If the time is outside the period we have owned the selection,
							 * which is any time later than timestamp, or if the requested target
							 * is not a string, then refuse the SelectionRequest. NB. Some broken
							 * clients don't set a valid timestamp, so we have to check against
							 * CurrentTime here. */
							ev.property = /* None */ IntPtr.Zero;
						}
						else if (ev.target == X11Helper.GetAtom(_x11Window.Display, X11Helper.TIMESTAMP))
						{
							/* Return timestamp used to acquire ownership if target is TIMESTAMP */
							ev.property = xsr.property;

							XLib.XChangeProperty(ev.display, ev.requestor, ev.property, X11Helper.GetAtom(_x11Window.Display, X11Helper.XA_INTEGER), 32, PropertyMode.Replace,
								_ownershipTimestamp, 1);
						}
						else if (ev.target == X11Helper.GetAtom(_x11Window.Display, X11Helper.TARGETS))
						{
							ev.property = xsr.property;

							unsafe
							{
								var atoms = _supportedAtoms.Concat(_textFormats.Keys.Select(name => X11Helper.GetAtom(_x11Window.Display, name))).ToArray();
								var size = atoms.Length * sizeof(IntPtr);
								var targetsCpy = Marshal.AllocHGlobal(size);
								var span = new ReadOnlySpan<IntPtr>(atoms);
								span.CopyTo(new Span<IntPtr>(targetsCpy.ToPointer(), size));

								XLib.XChangeProperty(
									ev.display,
									ev.requestor,
									ev.property,
									X11Helper.GetAtom(_x11Window.Display, X11Helper.XA_ATOM),
									32,
									PropertyMode.Replace,
									targetsCpy,
									size);
								XLib.XFlush(_x11Window.Display);

								Marshal.FreeHGlobal(targetsCpy);
							}
						}
						else if (ev.target == X11Helper.GetAtom(_x11Window.Display, X11Helper.MULTIPLE))
						{
							// TODO: refactor the implementation so that we can implemenent MULTIPLE sanely
							// if (xsr.property == /* None */ IntPtr.Zero)
							// { /* Invalid MULTIPLE request */
							// 	ev.property = /* None */ IntPtr.Zero;
							// }
							// else
							// {
							// 	ev.property = xsr.property;
							//
							// 	// hr = handle_multiple (ev.display, ev.requestor, ev.property, sel,
							// 	// 	ev.selection, ev.time, NULL);
							//
							// 	// static HandleResult
							// 	// 	handle_multiple (Display * display, Window requestor, Atom property,
							// 	// 		unsigned char * sel, Atom selection, Time time,
							// 	// 	MultTrack * mparent);
							//
							//
							// 	XLib.XGetWindowProperty(ev.display, ev.requestor, ev.property, 0, new IntPtr(0x7fffffff),
							// 		false, /* AnyPropertyType */ IntPtr.Zero, out IntPtr type,
							// 		out int format, out var length, out IntPtr bytesafter,
							// 		out IntPtr atomsArray);
							//
							// 	/* Make sure we got the Atom list we want */
							// 	if (format != 32)
							// 	{
							// 		break;
							// 	}
							//
							// 	unsafe
							// 	{
							// 		var atomsSpan = new Span<IntPtr>(atomsArray.ToPointer(), (int)length);
							// 		for (var i = 0; i < length; i+= 2)
							// 		{
							// 			var atom = atomsSpan[i];
							// 			var prop = atomsSpan[i + 1];
							//
							// 			// TODO: this is kind of redundant, maybe refactor this out like xsel does
							// 			if (atom == X11Helper.GetAtom(_x11Window.Display, X11Helper.TIMESTAMP))
							// 			{
							// 				XLib.XChangeProperty(ev.display, ev.requestor, prop, X11Helper.GetAtom(_x11Window.Display, X11Helper.XA_INTEGER), 32, PropertyMode.Replace,
							// 					_ownershipTimestamp, 1);
							// 			}
							// 			else if (atom == X11Helper.GetAtom(_x11Window.Display, X11Helper.TARGETS))
							// 			{
							// 				var atoms = _supportedAtoms.Concat(_textFormats.Keys.Select(name => X11Helper.GetAtom(_x11Window.Display, name))).ToArray();
							// 				var size = atoms.Length * sizeof(IntPtr);
							// 				var targetsCpy = Marshal.AllocHGlobal(size);
							// 				var span = new ReadOnlySpan<IntPtr>(atoms);
							// 				span.CopyTo(new Span<IntPtr>(targetsCpy.ToPointer(), size));
							//
							// 				XLib.XChangeProperty(
							// 					ev.display,
							// 					ev.requestor,
							// 					ev.property,
							// 					X11Helper.GetAtom(_x11Window.Display, X11Helper.XA_ATOM),
							// 					32,
							// 					PropertyMode.Replace,
							// 					targetsCpy,
							// 					size);
							// 				XLib.XFlush(_x11Window.Display);
							//
							// 				Marshal.FreeHGlobal(targetsCpy);
							// 			}
							// 			else if (atom == X11Helper.GetAtom(_x11Window.Display, X11Helper.MULTIPLE))
							// 			{
							//
							// 			}
							// 		}
							// 	}
							// }
						}
						// TODO: some applications refuse to paste the clipboard we set with Ctrl-V. In firefox, only Ctrl-Shift-V works. Why?
						// Other apps like Thunar work as expected.
						else if (_textFormats.TryGetValue(XLib.GetAtomName(_x11Window.Display, ev.target), out var encoding))
						{
							ev.property = xsr.property;

							if (_clipboard.AvailableFormats.Contains(XLib.GetAtomName(_x11Window.Display, ev.target)))
							{
								if (_clipboard.FindRawData(StandardDataFormats.Text) is string s)
								{
									var bytes = encoding.GetBytes(s);
									XLib.XChangeProperty(ev.display, ev.requestor, ev.property, ev.target, 8, PropertyMode.Replace,
										bytes, bytes.Length);
								}
								else if (_clipboard.FindRawData(StandardDataFormats.Text) is byte[] bytes)
								{
									XLib.XChangeProperty(ev.display, ev.requestor, ev.property, ev.target, 8, PropertyMode.Replace,
										bytes, bytes.Length);
								}
							}
							else if (_clipboard.AvailableFormats.Contains(StandardDataFormats.Text))
							{
								var s = _clipboard.GetTextAsync().GetResults(); // TODO: is this deadlock-proof?
								var bytes = encoding.GetBytes(s);
								XLib.XChangeProperty(ev.display, ev.requestor, ev.property, ev.target, 8, PropertyMode.Replace,
									bytes, bytes.Length);
							}
							else
							{
								XLib.XChangeProperty(ev.display, ev.requestor, ev.property, X11Helper.GetAtom(_x11Window.Display, X11Helper.UTF8_STRING), 8, PropertyMode.Replace,
									IntPtr.Zero, 0);
							}
						}
						// TODO: support image copying.
						// Image conversion is broken in SkiaSharp on Linux as of 29/12/2023, so no need to
						// implement this right now.
						else
						{
							ev.property = /* None */ IntPtr.Zero;
						}

						if (ev.property != /* None */ IntPtr.Zero)
						{
							XEvent xev = default;
							xev.SelectionEvent = ev;
							XLib.XSendEvent(_x11Window.Display, ev.requestor, false,
								IntPtr.Zero, ref xev);
						}
						break;
					// TODO: implement INCR
				}
			}
		}
		// ReSharper disable once FunctionNeverReturns
	}

	public DataPackageView GetContent()
	{
		if (CurrentlyOwningClipboard)
		{
			return _clipboard;
		}

		var dataPackage = new DataPackage();

		if (!HasOwner)
		{
			return dataPackage.GetView();
		}

		var formats = (WaitForFormats() ?? Array.Empty<IntPtr>())
			.Select(a => (name: XLib.GetAtomName(_x11Window.Display, a), atom: a))
			.ToList();

		// Supported formats will use a StandardDataFormats string, so we filter them out here.
		foreach (var format in formats)
		{
			dataPackage.SetDataProvider(format.name, async ct => await Task.Run(() => WaitForBytes(format.atom), ct));
		}

		if (formats.FirstOrDefault(f => _imageFormats.Contains(f.name)) is var f1 && f1.atom != IntPtr.Zero)
		{
			dataPackage.SetDataProvider(StandardDataFormats.Bitmap,
				async _ => await Task.FromResult(WaitForBmp(f1.atom)));
		}

		if (formats.FirstOrDefault(f => _textFormats.Keys.Contains(f.name)) is var f2 && f2.atom != IntPtr.Zero)
		{
			dataPackage.SetDataProvider(StandardDataFormats.Text,
				async ct => await Task.Run(() => WaitForText(f2.atom), ct));
		}

		return dataPackage.GetView();

		// For easier deadlock debugging, replace Task.Run with
		// return await Task.Factory.StartNew(() =>
		// {
		//    Thread.CurrentThread.Name = $"XSEL {DateTime.Now}";
		//    return <your method>();
		// }, TaskCreationOptions.LongRunning);
	}

	/// <summary>checks if a clipboard owner exists (i.e. this is a clipboard)</summary>
	private bool HasOwner
	{
		get
		{
			lock (_windowMutex)
			{
				return XLib.XGetSelectionOwner(_x11Window.Display, X11Helper.GetAtom(_x11Window.Display, X11Helper.CLIPBOARD)) != IntPtr.Zero;
			}
		}
	}

	private IntPtr[] WaitForFormats()
	{
		lock (_windowMutex)
		{
			if (!HasOwner)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Found the X11 CLIPBOARD clipboard to be owner-less.");
				}

				return null;
			}

			var clipboardAtom = X11Helper.GetAtom(_x11Window.Display, X11Helper.CLIPBOARD);
			var targetsAtom = X11Helper.GetAtom(_x11Window.Display, X11Helper.TARGETS);
			XLib.XConvertSelection(_x11Window.Display, clipboardAtom, targetsAtom, targetsAtom,
				_x11Window.Window, /* CurrentTime */ IntPtr.Zero);

			// We don't need an event mask here as Selection events aren't maskable.

			// can probably be optimized with epoll but at the cost of thread preemption
			SpinWait.SpinUntil(() => X11Helper.XPending(_x11Window.Display) > 0);

			XLib.XNextEvent(_x11Window.Display, out var event_);

			if (event_.type != XEventName.SelectionNotify)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"UNEXPECTED XSELECTION EVENT: {event_.type} {event_.SelectionEvent}");
				}

				return null;
			}

			var sel = event_.SelectionEvent;
			if (sel.property != targetsAtom)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"EXPECTED XSELECTION PROPERTY {targetsAtom}, INSTEAD FOUND {sel.property}");
				}

				return null;
			}

			if (sel.selection != clipboardAtom)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"EXPECTED SelectionEvent.selection TO BE {clipboardAtom}, INSTEAD FOUND {event_.SelectionEvent.selection}");
				}
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"XSELELECTION EVENT: {event_.type} {event_.SelectionEvent}");
			}

			XLib.XGetWindowProperty(_x11Window.Display, _x11Window.Window, sel.property, IntPtr.Zero, new IntPtr(0x7fffffff), true, /* AnyPropertyType */ IntPtr.Zero,
				out var actualTypeAtom, out var actualFormat, out var nitems, out var bytes_after, out var prop);
			using var _ = Disposable.Create(() => XLib.XFree(prop));

			// TODO: do we need this check?
			// if (nitems == IntPtr.Zero)
			// {
			// 	return null;
			// }

			if (actualFormat != 32)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"EXPECTED XSELECTION actual_format TO BE 32 (IntPtr), INSTEAD FOUND {actualFormat}");
				}

				return null;
			}

			var formats = new IntPtr[nitems.ToInt32()];
			Marshal.Copy(prop, formats, 0, formats.Length);
			return formats;
		}
	}

	private byte[] WaitForBytes(IntPtr format)
	{
		lock (_windowMutex)
		{
			if (!HasOwner)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Found the X11 CLIPBOARD clipboard to be owner-less.");
				}

				return null;
			}

			var clipboardAtom = X11Helper.GetAtom(_x11Window.Display, X11Helper.CLIPBOARD);
			XLib.XConvertSelection(_x11Window.Display, clipboardAtom, format, format,
				_x11Window.Window, /* CurrentTime */ IntPtr.Zero);

			// We don't need an event mask here as Selection events aren't maskable.

			// can probably be optimized with epoll but at the cost of thread preemption
			SpinWait.SpinUntil(() => X11Helper.XPending(_x11Window.Display) > 0);

			XLib.XNextEvent(_x11Window.Display, out var event_);

			if (event_.type != XEventName.SelectionNotify)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"UNEXPECTED XSELECTION EVENT: {event_.type} {event_.SelectionEvent}");
				}

				return null;
			}

			var sel = event_.SelectionEvent;
			if (sel.property != format)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"EXPECTED XSELECTION PROPERTY {format}, INSTEAD FOUND {sel.property}");
				}

				return null;
			}

			if (sel.selection != clipboardAtom)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"EXPECTED SelectionEvent.selection TO BE {clipboardAtom}, INSTEAD FOUND {event_.SelectionEvent.selection}");
				}
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"XSELELECTION EVENT: {event_.type} {event_.SelectionEvent}");
			}

			XLib.XGetWindowProperty(_x11Window.Display, _x11Window.Window, sel.property, IntPtr.Zero, new IntPtr(0x7fffffff), true, /* AnyPropertyType */ IntPtr.Zero,
				out var actualTypeAtom, out var actualFormat, out var nitems, out var bytes_after, out var prop);
			var readonlyProp = prop;
			using var propDisposable = Disposable.Create(() => XLib.XFree(readonlyProp));

			var incrAtom = X11Helper.GetAtom(_x11Window.Display, X11Helper.INCR);
			if (actualTypeAtom == incrAtom)
			{
				// INCR state as of 28/12/2023: xsel implements INCR with an MIT License
				// xclip implements INCR with nice comments but is GPL2. Avalonia doesn't implement it.

				// This is particularly important for copying and pasting images.
				// We follow https://tronche.com/gui/x/icccm/sec-2.html#s-2.7.2 and xsel

				// prop contains a lower bound on the number of bytes, maybe we should use that to set the initial capacity somehow?
				var stream = new MemoryStream();

				XLib.XDeleteProperty(_x11Window.Display, _x11Window.Window, sel.property);
				XLib.XFlush(_x11Window.Display); // just a precaution

				XLib.XSelectInput(_x11Window.Display, _x11Window.Window, (IntPtr)EventMask.PropertyChangeMask);
				using var maskDisposable = Disposable.Create(() => XLib.XSelectInput(_x11Window.Display, _x11Window.Window, (IntPtr)EVENT_MASK));

				while (true)
				{
					SpinWait.SpinUntil(() => X11Helper.XPending(_x11Window.Display) > 0);
					XLib.XNextEvent(_x11Window.Display, out event_);

					if (event_.type != XEventName.PropertyNotify || event_.PropertyEvent.state != /* PropertyNewValue */ IntPtr.Zero)
					{
						continue;
					}

					XLib.XGetWindowProperty(_x11Window.Display, _x11Window.Window, sel.property, IntPtr.Zero, new IntPtr(0x7fffffff), true, /* AnyPropertyType */ IntPtr.Zero,
						out actualTypeAtom, out actualFormat, out nitems, out bytes_after, out prop);
					var readonlyProp2 = prop;
					using var propDisposable2 = Disposable.Create(() => XLib.XFree(readonlyProp2));

					if (bytes_after == 0)
					{
						// end of transfer
						break;
					}

					unsafe
					{
						var span = new ReadOnlySpan<byte>(prop.ToPointer(), (int)nitems * (actualFormat / 8));
						stream.Write(span);
					}
				}

				return stream.ToArray();
			}

			if (actualTypeAtom != format)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"EXPECTED XSELECTION FORMAT {format}, INSTEAD FOUND {actualTypeAtom}");
				}

				return null;
			}

			// TODO: do we need this check?
			// if (nitems == IntPtr.Zero)
			// {
			// 	return null;
			// }

			var data = new byte[(int)nitems * (actualFormat / 8)];
			Marshal.Copy(prop, data, 0, data.Length);
			return data;
		}
	}

	private string WaitForText(IntPtr format)
	{
		var bytes = WaitForBytes(format);
		if (_textFormats.TryGetValue(XLib.GetAtomName(_x11Window.Display, format), out var enc))
		{
			return enc.GetString(bytes);
		}

		return null;
	}

	private byte[] WaitForBmp(IntPtr format)
	{
		// Note: this will most likely crash on Linux due to a bug in skia/skiasharp.
		// Either Decode or Encode will return null
		var bytes = WaitForBytes(format);
		var bitmap = SKBitmap.Decode(bytes);
		return bitmap.Encode(SKEncodedImageFormat.Bmp, 100).ToArray();
	}

	/*
	 * get_timestamp ()
	 *
	 * Get the current X server time.
	 *
	 * This is done by doing a zero-length append to a random property of the
	 * window, and checking the time on the subsequent PropertyNotify event.
	 *
	 * PRECONDITION: the window must have PropertyChangeMask set.
	 */
	// TODO: currently hangs
	private IntPtr GetTimestamp()
	{
		lock (_windowMutex)
		{
			XLib.XChangeProperty(
				_x11Window.Display,
				_x11Window.Window,
				X11Helper.GetAtom(_x11Window.Display, "_NET_WM_NAME"),
				X11Helper.GetAtom(_x11Window.Display, X11Helper.XA_STRING),
				8,
				PropertyMode.Append,
				IntPtr.Zero,
				0);

			while (true)
			{
				XLib.XNextEvent(_x11Window.Display, out var event_);

				if (event_.type == XEventName.PropertyNotify)
				{
					return event_.PropertyEvent.time;
				}
			}
		}
	}
}
