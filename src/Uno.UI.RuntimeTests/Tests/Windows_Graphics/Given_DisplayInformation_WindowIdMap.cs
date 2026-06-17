// The per-WindowId DisplayInformation registry (and its CreateForWindowIdForTests seam) is
// compile-time excluded on Android (#if !ANDROID in DisplayInformation.cs) — Android's
// DisplayInformation is a process singleton, not per-window — so these tests cannot exist there.
#if HAS_UNO && !__ANDROID__
#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Graphics.Display;

namespace Uno.UI.RuntimeTests.Tests.Windows_Graphics;

/// <summary>
/// The per-WindowId <see cref="DisplayInformation"/> registry has no removal path: a closed
/// window's entry retains its native wrapper / window-implementation graph — and every
/// window-event subscriber reachable from it — for the process lifetime. For a secondary-app
/// window in a collectible AssemblyLoadContext this pins the whole ALC.
/// <see cref="DisplayInformation.DestroyForWindowId"/> (called from ALC window close) removes the
/// entry.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_DisplayInformation_WindowIdMap
{
	[TestMethod]
	public async Task When_DestroyForWindowId_Then_Entry_Is_Released()
	{
		// A synthetic id (no live window) is intentional: this exercises the registry lifetime
		// invariant only. We register via CreateForWindowIdForTests rather than GetOrCreateForWindowId
		// because the latter runs Initialize(), which on Skia resolves an IDisplayInformationExtension
		// for a real AppWindow and throws for an id with no window.
		var windowId = new WindowId(0xD15F0);

		try
		{
			var weakInstance = CreateEntryAndDropStrongReference(windowId);

			DisplayInformation.DestroyForWindowId(windowId);

			var collected = await TestHelper.TryWaitUntilCollected(weakInstance);

			Assert.IsTrue(
				collected,
				"DestroyForWindowId must remove the static map entry; otherwise the closed window's " +
				"DisplayInformation (and everything reachable from it) is retained for the process lifetime.");
		}
		finally
		{
			DisplayInformation.DestroyForWindowId(windowId);
		}
	}

	[TestMethod]
	public void When_DestroyForWindowId_Then_Subsequent_GetOrCreate_Returns_New_Instance()
	{
		// Synthetic id with no live window; see When_DestroyForWindowId_Then_Entry_Is_Released for
		// why CreateForWindowIdForTests is used instead of GetOrCreateForWindowId.
		var windowId = new WindowId(0xD15F1);

		try
		{
			var first = DisplayInformation.CreateForWindowIdForTests(windowId);
			Assert.AreSame(first, DisplayInformation.CreateForWindowIdForTests(windowId), "Pre-condition: the registry caches per WindowId");

			DisplayInformation.DestroyForWindowId(windowId);

			var second = DisplayInformation.CreateForWindowIdForTests(windowId);
			Assert.AreNotSame(first, second, "After DestroyForWindowId, a fresh instance must be created for the id");
		}
		finally
		{
			DisplayInformation.DestroyForWindowId(windowId);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateEntryAndDropStrongReference(WindowId windowId)
		=> new WeakReference(DisplayInformation.CreateForWindowIdForTests(windowId));
}
#endif
