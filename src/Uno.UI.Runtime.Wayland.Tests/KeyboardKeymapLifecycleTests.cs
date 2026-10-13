#nullable enable

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland.Input;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class KeyboardKeymapLifecycleTests
{
	private static void RequireXkbcommon()
	{
		if (!OperatingSystem.IsLinux() || !NativeLibrary.TryLoad("libxkbcommon.so.0", out var library))
		{
			Assert.Inconclusive("libxkbcommon and its keymap tables are required.");
			return;
		}
		NativeLibrary.Free(library);
	}

	[TestMethod]
	public void NoKeyboardDeviceNeedsNoLocalKeymapOrXkbContext()
	{
		using var keyboard = CreateKeyboard();
		AssertXkbHandles(keyboard, allocated: false);
		var events = 0;
		keyboard.KeyDown += (_, _) => events++;
		keyboard.Enter([]);
		keyboard.Key(57, 1);
		keyboard.Key(57, 0);
		Assert.AreEqual(0, events);
		AssertXkbHandles(keyboard, allocated: false);
	}

	[TestMethod]
	public void CompositorKeymapReplacementAndUnsupportedMapReleaseState()
	{
		RequireXkbcommon();
		using var keyboard = CreateKeyboard();
		var text = new List<char?>();
		keyboard.KeyDown += (_, args) => text.Add(GetText(args));
		keyboard.RepeatInfo(0, 0);
		keyboard.Enter([]);
		var path = Path.GetTempFileName();
		try
		{
			File.WriteAllBytes(path, Encoding.UTF8.GetBytes("""
				xkb_keymap {
				  xkb_keycodes { include "evdev+aliases(qwerty)" };
				  xkb_types { include "complete" };
				  xkb_compatibility { include "complete" };
				  xkb_symbols { include "pc+us+inet(evdev)" key <SPCE> { [ a, A ] }; };
				};
				""" + "\0"));
			using var handle = File.OpenHandle(path);
			var fd = checked((int)handle.DangerousGetHandle());
			handle.SetHandleAsInvalid();
			keyboard.Keymap(1, fd, checked((uint)new FileInfo(path).Length));
			TapSpace(keyboard);
			keyboard.UpdateModifiers(1, 0, 0, 0);
			TapSpace(keyboard);
			AssertXkbHandles(keyboard, allocated: true);
			using var replacementHandle = File.OpenHandle(path);
			var replacementFd = checked((int)replacementHandle.DangerousGetHandle());
			replacementHandle.SetHandleAsInvalid();
			keyboard.Keymap(1, replacementFd, checked((uint)new FileInfo(path).Length));
			keyboard.UpdateModifiers(0, 0, 0, 0);
			TapSpace(keyboard);
			using var unsupportedHandle = File.OpenHandle(path);
			var unsupportedFd = checked((int)unsupportedHandle.DangerousGetHandle());
			unsupportedHandle.SetHandleAsInvalid();
			keyboard.Keymap(0, unsupportedFd, 0);
			Assert.AreEqual(-1, Uno.UI.Runtime.Wayland.Native.Posix.fcntl(unsupportedFd, 3, 0));
			AssertXkbHandles(keyboard, allocated: false);
			TapSpace(keyboard);
			CollectionAssert.AreEqual(new char?[] { 'a', 'A', 'a' }, text);
			keyboard.Dispose();
			AssertXkbHandles(keyboard, allocated: false);
		}
		finally
		{
			File.Delete(path);
		}
	}

	[TestMethod]
	public void NoKeymapDoesNotInventNativeKeyboardFocus()
	{
		using var keyboard = CreateKeyboard();
		var events = 0;
		keyboard.KeyDown += (_, _) => events++;
		keyboard.Key(57, 1);
		Assert.AreEqual(0, events);
	}

	private static void AssertXkbHandles(WaylandKeyboardInputSource keyboard, bool allocated)
	{
		foreach (var field in new[] { "_context", "_keymap", "_state" })
		{
			var handle = (nint)typeof(WaylandKeyboardInputSource).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(keyboard)!;
			Assert.AreEqual(allocated, handle != 0, field);
		}
	}

	private static WaylandKeyboardInputSource CreateKeyboard() => new(new NoKeyboardConnection(), action => action(), _ => { });

	private static char? GetText(KeyEventArgs args)
		=> (char?)typeof(KeyEventArgs).GetProperty("UnicodeKey", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(args);

	private static void TapSpace(WaylandKeyboardInputSource keyboard)
	{
		keyboard.Key(57, 1);
		keyboard.Key(57, 0);
	}

	private sealed class NoKeyboardConnection : IWaylandInputConnection
	{
		public void Post(Action action) => action();
		public void SetCursor(uint enterSerial, CoreCursor? cursor) { }
		public IDisposable GetPointer(WaylandPointerInputSource sink) => throw new NotSupportedException();
		public IDisposable GetKeyboard(WaylandKeyboardInputSource sink) => throw new NotSupportedException();
		public IDisposable GetTouch(WaylandPointerInputSource sink) => throw new NotSupportedException();
	}
}
