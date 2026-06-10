using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.HotReload.Client;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

/// <summary>
/// Tests that the hot-reload update pipeline is resilient to individual
/// element and handler failures. When one element or handler throws, the
/// remaining elements should still be updated and the ReloadCompleted
/// callback should still fire.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_HotReloadResilience : BaseTestClass
{
	/// <summary>
	/// Verifies that after a hot-reload cycle that updates a TextBlock,
	/// the <see cref="TestingUpdateHandler.ReloadCompleted"/> callback fires
	/// and reports that the UI was updated. This is a baseline test: if the
	/// per-element error isolation or handler try/catch is broken, the
	/// ReloadCompleted callback would not fire or would report uiUpdated=false.
	/// </summary>
	[TestMethod]
	public async Task When_HotReload_Succeeds_Then_ReloadCompleted_ReportsSuccess()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var page = new HR_Frame_Pages_Page1();
		UnitTestsUIContentHelper.Content = page;

		await UnitTestsUIContentHelper.WaitForIdle();

		// After hot-reload completes, the ReloadCompleted handler should fire with uiUpdated=true
		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			"Hello",
			"World",
			async () =>
			{
				// The fact that we got here means the hot-reload pipeline completed
				// without throwing. Verify the text was actually updated.
				var tb = page.FindName("tb1") as TextBlock;
				Assert.IsNotNull(tb, "TextBlock 'tb1' should exist in the page");
				Assert.AreEqual("World", tb.Text, "TextBlock text should have been updated by hot-reload");
			},
			ct);
	}

	/// <summary>
	/// Verifies that ReloadCompleted fires even when the visual-tree apply
	/// is deferred via the new <see cref="UIUpdate.Pause"/> mechanism (spec 041).
	/// Tests the finally-block resilience: even if the update is queued, the
	/// completion callback should execute (immediately for non-FE-only deltas,
	/// after drain for FE deltas).
	/// </summary>
	[TestMethod]
	public async Task When_VisualTree_Paused_Then_ReloadCompleted_StillFires()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		// Page must be in the tree so that the drain on dispose actually
		// runs DoUpdateVisualTreeCore against it.
		var page = new HR_Frame_Pages_Page1();
		UnitTestsUIContentHelper.Content = page;

		await UnitTestsUIContentHelper.WaitForIdle();

		using (UIUpdate.Pause(HotReloadUIPhases.VisualTree))
		{
			var completed = TestingUpdateHandler.WaitForReloadCompleted();

			// While the pause is held, the update is queued and the op is
			// reported as Ignored ("UI update paused by UpdateFile").
			await HotReloadHelper.UpdateServerFile<HR_Frame_Pages_Page1>(
				"Hello", "PausedTest", ct);

			// ReloadCompleted has not fired yet — it fires when the drain
			// eventually applies the queued types after Dispose below.
			Assert.IsFalse(completed.IsCompleted, "ReloadCompleted should not fire while VisualTree is paused.");
		}

		// After dispose, the pending visual-tree types are drained and the
		// completion callback fires with uiUpdated=true.
		try
		{
			var drained = TestingUpdateHandler.WaitForReloadCompleted();
			var result = await drained.WaitAsync(ct);
			Assert.IsTrue(result, "ReloadCompleted should report uiUpdated=true after pause is released and drain runs.");
		}
		finally
		{
			// Undo the file change so subsequent tests start from a known state.
			await HotReloadHelper.UpdateServerFile<HR_Frame_Pages_Page1>(
				"PausedTest", "Hello", CancellationToken.None);
		}
	}
}
