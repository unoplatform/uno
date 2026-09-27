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
		// NOTE: this test previously edited "Hello" and asserted a TextBlock named "tb1" —
		// neither ever existed in HR_Frame_Pages_Page1.xaml, so the server-side replace was a
		// no-op and the assertion could not succeed (the test landed after the last fully-green
		// master run). It now uses the same pattern as the other page-edit tests: a Frame host
		// (a reloadable-type update REPLACES the page instance, so the element must be
		// re-resolved from the live tree, not from the pre-update instance).
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;

		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		UnitTestsUIContentHelper.Content = frame;

		frame.Navigate(typeof(HR_Frame_Pages_Page1));

		await frame.ValidateTextOnChildTextBlock("First page", 0);

		// After hot-reload completes, the reload pipeline reported success and the UI must
		// reflect the edit end-to-end.
		await HotReloadHelper.UpdateServerFileAndRevert<HR_Frame_Pages_Page1>(
			"First page",
			"First page (reloaded)",
			() => frame.ValidateTextOnChildTextBlock("First page (reloaded)", 0),
			ct);

		// And the revert must restore the original text.
		await frame.ValidateTextOnChildTextBlock("First page", 0);
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

		var edited = false;
		try
		{
			using (UIUpdate.Pause(HotReloadUIPhases.VisualTree))
			{
				var completed = TestingUpdateHandler.WaitForReloadCompleted();

				// While the pause is held, the update is queued and the op is
				// reported as Ignored ("UI update paused by UpdateFile").
				await HotReloadHelper.UpdateServerFile<HR_Frame_Pages_Page1>(
					"First page", "Paused page", ct);
				edited = true;

				// ReloadCompleted has not fired yet — it fires when the drain
				// eventually applies the queued types after Dispose below.
				Assert.IsFalse(completed.IsCompleted, "ReloadCompleted should not fire while VisualTree is paused.");
			}

			// After dispose, the pending visual-tree types are drained and the
			// completion callback fires with uiUpdated=true.
			var drained = TestingUpdateHandler.WaitForReloadCompleted();
			var result = await drained.WaitAsync(ct);
			Assert.IsTrue(result, "ReloadCompleted should report uiUpdated=true after pause is released and drain runs.");
		}
		finally
		{
			// Undo the file change so subsequent tests start from a known state. The edit now also
			// gets undone when the assertion inside the pause fails, and is skipped when it never
			// landed — reverting an unmodified file trips the no-op guard and hides the real failure.
			if (edited)
			{
				await HotReloadHelper.UpdateServerFile<HR_Frame_Pages_Page1>(
					"Paused page", "First page", CancellationToken.None);
			}
		}
	}
}
