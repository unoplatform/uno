#nullable enable

using System;
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
		var appWindow = new AppWindow();
		var windowId = appWindow.Id;

		_ = CoreDragDropManager.GetOrCreateForWindowId(windowId);

		Assert.IsNotNull(AppWindow.GetFromWindowId(windowId), "Pre-condition: AppWindow must be registered.");
		Assert.IsNotNull(ApplicationView.GetForWindowId(windowId), "Pre-condition: ApplicationView must be registered.");
		Assert.IsNotNull(CoreDragDropManager.GetForWindowId(windowId), "Pre-condition: CoreDragDropManager must be registered.");

		ApplicationView.DestroyForWindowId(windowId);
		CoreDragDropManager.DestroyForWindowId(windowId);
		AppWindow.DestroyForWindowId(windowId);

		Assert.IsNull(
			AppWindow.GetFromWindowId(windowId),
			"AppWindow.DestroyForWindowId must remove the map entry; otherwise the closed window (and its subscribers) pins its ALC.");
		Assert.ThrowsExactly<InvalidOperationException>(
			() => ApplicationView.GetForWindowId(windowId),
			"ApplicationView.DestroyForWindowId must remove the map entry.");
		Assert.ThrowsExactly<InvalidOperationException>(
			() => CoreDragDropManager.GetForWindowId(windowId),
			"CoreDragDropManager.DestroyForWindowId must remove the map entry.");
	}
}
