#nullable enable

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland.Input;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class KeyboardRepeatTests
{
	[TestMethod]
	public void ServerAndDuplicatePressRepeatsKeepComposedText()
	{
		WithKeyboard((keyboard, _) =>
		{
			var text = new List<char?>();
			keyboard.KeyDown += (_, args) => text.Add(GetText(args));
			keyboard.RepeatInfo(0, 0);
			ComposeAcuteE(keyboard);
			keyboard.Key(18, 2);
			keyboard.Key(18, 1);
			CollectionAssert.AreEqual(new char?[] { 'é', 'é', 'é' }, text);
			keyboard.Key(18, 0);
			keyboard.Key(18, 2);
			keyboard.Key(18, 1);
			CollectionAssert.AreEqual(new char?[] { 'é', 'é', 'é', 'e' }, text);
		});
	}

	[TestMethod]
	public void PlainRepeatFollowsCurrentModifiers()
	{
		WithKeyboard((keyboard, _) =>
		{
			var text = new List<char?>();
			keyboard.KeyDown += (_, args) => text.Add(GetText(args));
			keyboard.RepeatInfo(0, 0);
			keyboard.Key(30, 1); // a
			keyboard.UpdateModifiers(1, 0, 0, 0); // Shift
			keyboard.Key(30, 2);
			CollectionAssert.AreEqual(new char?[] { 'a', 'A' }, text);
		});
	}

	[TestMethod]
	public void TimerRepeatKeepsComposedTextAndLeaveCancelsQueuedRepeat()
	{
		WithKeyboard((keyboard, connection) =>
		{
			var text = new List<char?>();
			keyboard.KeyDown += (_, args) => text.Add(GetText(args));
			keyboard.RepeatInfo(100, 1);
			ComposeAcuteE(keyboard);
			Assert.IsTrue(SpinWait.SpinUntil(() => !connection.Pending.IsEmpty, TimeSpan.FromSeconds(5)));
			Assert.IsTrue(connection.Pending.TryDequeue(out var repeat));
			var callback = repeat!;
			callback();
			CollectionAssert.AreEqual(new char?[] { 'é', 'é' }, text);
			keyboard.Leave();
			callback();
			CollectionAssert.AreEqual(new char?[] { 'é', 'é' }, text);
		});
	}

	[TestMethod]
	public void RepeatKeepsTheDeliveryModeOfThePress()
	{
		WithKeyboard((keyboard, connection) =>
		{
			var events = new List<string>();
			keyboard.KeyDown += (_, args) => events.Add($"down {args.VirtualKey}");
			keyboard.KeyUp += (_, args) => events.Add($"up {args.VirtualKey}");
			keyboard.RepeatInfo(0, 0);

			// A Compose string press only ever inserts text, even once Control is held.
			ComposeAcute(keyboard, 36); // j́
			keyboard.Key(36, 2);
			keyboard.UpdateModifiers(4, 0, 0, 0); // Control
			keyboard.Key(36, 2);
			keyboard.Key(36, 0);
			keyboard.UpdateModifiers(0, 0, 0, 0);
			CollectionAssert.AreEqual(Array.Empty<string>(), events);
			CollectionAssert.AreEqual(new[] { "j\u0301", "j\u0301" }, connection.Committed);

			// A key event press keeps raising key events, even once Control is released.
			keyboard.UpdateModifiers(4, 0, 0, 0);
			ComposeAcute(keyboard, 36);
			keyboard.UpdateModifiers(0, 0, 0, 0);
			keyboard.Key(36, 2);
			keyboard.Key(36, 1);
			keyboard.Key(36, 0);
			CollectionAssert.AreEqual(new[] { "down J", "down J", "down J", "up J" }, events);
			CollectionAssert.AreEqual(new[] { "j\u0301", "j\u0301" }, connection.Committed);
		});
	}

	[TestMethod]
	public void TimerStringRepeatSuppressesShortcutsAndLeaveHasNoKeyUp()
	{
		WithKeyboard((keyboard, connection) =>
		{
			var events = new List<string>();
			keyboard.KeyDown += (_, args) => events.Add($"down {args.VirtualKey}");
			keyboard.KeyUp += (_, args) => events.Add($"up {args.VirtualKey}");
			keyboard.RepeatInfo(100, 1);
			ComposeAcute(keyboard, 36);
			var repeat = TakeRepeat(connection);
			repeat();
			keyboard.UpdateModifiers(4, 0, 0, 0);
			repeat();
			keyboard.UpdateModifiers(0, 0, 0, 0);
			repeat();
			keyboard.Leave();
			repeat();
			keyboard.Key(36, 0);
			CollectionAssert.AreEqual(new[] { "j\u0301", "j\u0301", "j\u0301" }, connection.Committed);
			CollectionAssert.AreEqual(Array.Empty<string>(), events);
		});
	}

	[TestMethod]
	public void TimerKeyRepeatFollowsModifiersAndLeavePairsKeyUp()
	{
		WithKeyboard((keyboard, connection) =>
		{
			var text = new List<char?>();
			var events = new List<string>();
			keyboard.KeyDown += (_, args) =>
			{
				text.Add(GetText(args));
				events.Add($"down {args.VirtualKey}");
			};
			keyboard.KeyUp += (_, args) => events.Add($"up {args.VirtualKey}");
			keyboard.RepeatInfo(100, 1);
			keyboard.Key(30, 1);
			var repeat = TakeRepeat(connection);
			keyboard.UpdateModifiers(1, 0, 0, 0);
			repeat();
			keyboard.UpdateModifiers(4, 0, 0, 0);
			repeat();
			keyboard.Leave();
			repeat();
			keyboard.Key(30, 0);
			CollectionAssert.AreEqual(new char?[] { 'a', 'A', null }, text);
			CollectionAssert.AreEqual(new[] { "down A", "down A", "down A", "up A" }, events);
			Assert.AreEqual(0, connection.Committed.Count);
		});
	}

	private static Action TakeRepeat(InputConnection connection)
	{
		Assert.IsTrue(SpinWait.SpinUntil(() => !connection.Pending.IsEmpty, TimeSpan.FromSeconds(5)));
		Assert.IsTrue(connection.Pending.TryDequeue(out var repeat));
		return repeat!;
	}

	private static char? GetText(KeyEventArgs args)
		=> (char?)typeof(KeyEventArgs).GetProperty("UnicodeKey", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(args);

	private static void ComposeAcuteE(WaylandKeyboardInputSource keyboard)
		=> ComposeAcute(keyboard, 18); // e

	private static void ComposeAcute(WaylandKeyboardInputSource keyboard, uint key)
	{
		keyboard.Key(40, 1); // us(intl) apostrophe: dead_acute
		keyboard.Key(40, 0);
		keyboard.Key(key, 1);
	}

	private static void WithKeyboard(Action<WaylandKeyboardInputSource, InputConnection> test)
	{
		if (!OperatingSystem.IsLinux() || !NativeLibrary.TryLoad("libxkbcommon.so.0", out var library))
		{
			Assert.Inconclusive("libxkbcommon and its keymap/compose tables are required.");
			return;
		}
		NativeLibrary.Free(library);
		var connection = new InputConnection();
		using var keyboard = new WaylandKeyboardInputSource(connection, action => action(), _ => { }, connection.Committed.Add);
		var path = Path.GetTempFileName();
		try
		{
			File.WriteAllBytes(path, Encoding.UTF8.GetBytes("""
				xkb_keymap {
				  xkb_keycodes { include "evdev+aliases(qwerty)" };
				  xkb_types { include "complete" };
				  xkb_compatibility { include "complete" };
				  xkb_symbols { include "pc+us(intl)+inet(evdev)" };
				};
				""" + "\0"));
			using var handle = File.OpenHandle(path);
			var fd = checked((int)handle.DangerousGetHandle());
			handle.SetHandleAsInvalid(); // Keymap owns the protocol file descriptor.
			keyboard.Keymap(1, fd, checked((uint)new FileInfo(path).Length));
			keyboard.Enter([]);
			test(keyboard, connection);
		}
		finally
		{
			File.Delete(path);
		}
	}

	private sealed class InputConnection : IWaylandInputConnection
	{
		internal ConcurrentQueue<Action> Pending { get; } = new();
		internal List<string> Committed { get; } = new();
		public void Post(Action action) => Pending.Enqueue(action);
		public void SetCursor(uint serial, CoreCursor? cursor) { }
		public IDisposable GetPointer(WaylandPointerInputSource sink) => throw new NotSupportedException();
		public IDisposable GetKeyboard(WaylandKeyboardInputSource sink) => throw new NotSupportedException();
		public IDisposable GetTouch(WaylandPointerInputSource sink) => throw new NotSupportedException();
	}
}
