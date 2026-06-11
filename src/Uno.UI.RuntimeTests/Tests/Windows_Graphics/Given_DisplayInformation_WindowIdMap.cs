#if HAS_UNO
#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI;
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
	public void When_DestroyForWindowId_Then_Entry_Is_Released()
	{
		var windowId = new WindowId(0xD15F0); // synthetic id, never used by a real window

		try
		{
			var weakInstance = CreateEntryAndDropStrongReference(windowId);

			DisplayInformation.DestroyForWindowId(windowId);

			for (var i = 0; i < 10 && weakInstance.IsAlive; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}

			Assert.IsFalse(
				weakInstance.IsAlive,
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
		var windowId = new WindowId(0xD15F1);

		try
		{
			var first = DisplayInformation.GetOrCreateForWindowId(windowId);
			Assert.AreSame(first, DisplayInformation.GetOrCreateForWindowId(windowId), "Pre-condition: the registry caches per WindowId");

			DisplayInformation.DestroyForWindowId(windowId);

			var second = DisplayInformation.GetOrCreateForWindowId(windowId);
			Assert.AreNotSame(first, second, "After DestroyForWindowId, a fresh instance must be created for the id");
		}
		finally
		{
			DisplayInformation.DestroyForWindowId(windowId);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateEntryAndDropStrongReference(WindowId windowId)
		=> new WeakReference(DisplayInformation.GetOrCreateForWindowId(windowId));
}
#endif
