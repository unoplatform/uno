#nullable enable

using System;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Windowing;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.UI.ViewManagement;
using MUXWindowId = Microsoft.UI.WindowId;

namespace Uno.UI.Tests.Windows_UI_Xaml;

/// <summary>
/// The per-<see cref="MUXWindowId"/> registries on <see cref="AppWindow"/>,
/// <see cref="ApplicationView"/> and <see cref="CoreDragDropManager"/> have no removal path: a
/// closed window's entry — and every event subscriber reachable from it — is retained for the
/// process lifetime. For a secondary-app window in a collectible AssemblyLoadContext, this pins the
/// whole ALC. Each map now exposes <c>DestroyForWindowId</c> (called from ALC window close) that
/// removes the entry.
/// </summary>
[TestClass]
public class Given_WindowId_Maps_Alc
{
	[TestMethod]
	public void When_DestroyForWindowId_Then_Maps_Release_Entries()
	{
		// Constructing an AppWindow registers it in AppWindow._windowIdMap and creates the matching
		// ApplicationView (ApplicationView._windowIdMap). CoreDragDropManager is created on demand.
		MUXWindowId windowId = default;
		WeakReference? weakAppWindow = null;

		// try/finally: the pre-condition assertions can fail, and a failing assertion must not
		// leave a native-window-less AppWindow registered in the process-wide maps for sibling
		// tests in the same run — the destroy calls always execute.
		try
		{
			(windowId, weakAppWindow) = CreateRegisteredAppWindow();
		}
		finally
		{
			ApplicationView.DestroyForWindowId(windowId);
			CoreDragDropManager.DestroyForWindowId(windowId);
			AppWindow.DestroyForWindowId(windowId);
		}

		Assert.IsNull(
			AppWindow.GetFromWindowId(windowId),
			"AppWindow.DestroyForWindowId must remove the map entry; otherwise the closed window (and its subscribers) pins its ALC.");
		Assert.ThrowsExactly<InvalidOperationException>(
			() => ApplicationView.GetForWindowId(windowId),
			"ApplicationView.DestroyForWindowId must remove the map entry.");
		Assert.ThrowsExactly<InvalidOperationException>(
			() => CoreDragDropManager.GetForWindowId(windowId),
			"CoreDragDropManager.DestroyForWindowId must remove the map entry.");

		// The requirement is release-of-pin, not just map lookup failure: after all three destroys,
		// nothing static may still reference the AppWindow instance — for a secondary-app window
		// this is what releases the collectible ALC.
		Assert.IsTrue(
			TryWaitUntilCollected(weakAppWindow),
			"After DestroyForWindowId on all three registries, the AppWindow instance must be collectible; a surviving strong reference would pin a secondary app's AssemblyLoadContext.");
	}

	/// <summary>
	/// Every strong <see cref="AppWindow"/> reference — including the temporaries produced by the
	/// pre-condition lookups — must be confined to a separate, non-inlined frame: locals and
	/// evaluation-stack temporaries in the frame that runs the GC keep their objects alive
	/// (especially under Debug codegen, which extends lifetimes to the end of the method).
	/// </summary>
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (MUXWindowId WindowId, WeakReference WeakAppWindow) CreateRegisteredAppWindow()
	{
		var appWindow = new AppWindow();
		_ = CoreDragDropManager.GetOrCreateForWindowId(appWindow.Id);

		Assert.IsNotNull(AppWindow.GetFromWindowId(appWindow.Id), "Pre-condition: AppWindow must be registered.");
		Assert.IsNotNull(ApplicationView.GetForWindowId(appWindow.Id), "Pre-condition: ApplicationView must be registered.");
		Assert.IsNotNull(CoreDragDropManager.GetForWindowId(appWindow.Id), "Pre-condition: CoreDragDropManager must be registered.");

		return (appWindow.Id, new WeakReference(appWindow));
	}

	private static bool TryWaitUntilCollected(WeakReference reference)
	{
		for (var i = 0; i < 10 && reference.IsAlive; i++)
		{
			GC.Collect(2, GCCollectionMode.Forced, blocking: true);
			GC.WaitForPendingFinalizers();
		}

		return !reference.IsAlive;
	}
}
