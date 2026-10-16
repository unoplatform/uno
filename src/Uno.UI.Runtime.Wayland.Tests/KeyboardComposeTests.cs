#nullable enable

using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland.Input;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class KeyboardComposeTests
{
	[TestMethod]
	public void DeadKeyIsConsumedAndComposes()
	{
		WithCompose(compose =>
		{
			Assert.IsNull(compose.Feed(0xfe51, 0)); // dead_acute
			Assert.AreEqual(0xe9u, compose.Feed('e', 'e'));
			Assert.AreEqual((uint)'a', compose.Feed('a', 'a'));
		});
	}

	[TestMethod]
	public void ModifiersPassThroughAComposeSequence()
	{
		WithCompose(compose =>
		{
			Assert.IsNull(compose.Feed(0xff20, 0)); // Multi_key
			Assert.AreEqual(0u, compose.Feed(0xffe1, 0)); // Shift_L
			Assert.IsNull(compose.Feed('o', 'o'));
			Assert.AreEqual(0xa9u, compose.Feed('c', 'c'));
		});
	}

	[TestMethod]
	public void CancelledSequenceConsumesTheKeyAndResetClearsIt()
	{
		WithCompose(compose =>
		{
			Assert.IsNull(compose.Feed(0xff20, 0)); // Multi_key
			Assert.IsNull(compose.Feed(0xfe51, 0)); // dead_acute: no such sequence
			Assert.AreEqual((uint)'e', compose.Feed('e', 'e'));
			Assert.IsNull(compose.Feed(0xfe51, 0));
			compose.Reset();
			Assert.AreEqual((uint)'e', compose.Feed('e', 'e'));
		});
	}

	[TestMethod]
	public void ComposeReturnsSupplementaryUnicodeAndMultipleCharacters()
	{
		WithCompose(compose =>
		{
			Assert.IsNull(compose.Feed(0xff20, 0));
			Assert.IsNull(compose.Feed('p', 'p'));
			Assert.IsNull(compose.Feed('o', 'o'));
			Assert.AreEqual(0u, compose.Feed('o', 'o', out var supplementary));
			Assert.AreEqual("💩", supplementary);
			Assert.IsNull(compose.Feed(0xfe51, 0));
			Assert.AreEqual(0u, compose.Feed('j', 'j', out var multiple));
			Assert.AreEqual("j\u0301", multiple);
			Assert.AreEqual((uint)'a', compose.Feed('a', 'a', out var plain));
			Assert.IsNull(plain);
		});
	}

	[TestInitialize]
	public void RequireXkbcommon()
	{
		if (!OperatingSystem.IsLinux() || !NativeLibrary.TryLoad("libxkbcommon.so.0", out var library))
		{
			Assert.Inconclusive("libxkbcommon and its compose tables are required.");
			return;
		}
		NativeLibrary.Free(library);
	}

	private static void WithCompose(Action<WaylandKeyboardCompose> test)
	{
		var context = WaylandXkb.ContextNew(0);
		Assert.AreNotEqual((nint)0, context);
		try
		{
			using var compose = new WaylandKeyboardCompose(context, "en_US.UTF-8");
			test(compose);
		}
		finally
		{
			WaylandXkb.ContextUnref(context);
		}
	}
}
