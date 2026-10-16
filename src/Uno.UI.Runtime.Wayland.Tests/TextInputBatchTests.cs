#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class TextInputBatchTests
{
	[TestMethod]
	public void PendingCommitSurvivesRepeatedResyncAndIsConsumedOnce()
	{
		var pending = new WaylandTextInputBatch();
		pending.Begin(10);
		pending.Commit = "é";
		pending.DeleteBefore = 1;
		pending.Resynchronize(12);
		pending.Resynchronize(14);
		var inserted = string.Empty;
		var batch = pending.Take(10, 14);
		Assert.AreEqual(WaylandTextInputBatchDisposition.ApplyWithoutDeletion, batch.Disposition);
		Assert.AreEqual(0u, batch.DeleteBefore);
		inserted += batch.Commit;
		inserted += pending.Take(10, 14).Commit;
		Assert.AreEqual("é", inserted);
	}

	[TestMethod]
	public void LateBatchPreservesCommitAndPreeditButSuppressesDeletion()
	{
		var pending = new WaylandTextInputBatch();
		pending.Begin(10);
		pending.Resynchronize(12);
		pending.Commit = "commit";
		pending.Preedit = "preedit";
		pending.PreeditCursor = 3;
		pending.DeleteBefore = 2;
		pending.DeleteAfter = 4;
		var batch = pending.Take(11, 12);
		Assert.AreEqual(WaylandTextInputBatchDisposition.ApplyWithoutDeletion, batch.Disposition);
		Assert.AreEqual("commit", batch.Commit);
		Assert.AreEqual("preedit", batch.Preedit);
		Assert.AreEqual(3, batch.PreeditCursor);
		Assert.AreEqual(0u, batch.DeleteBefore);
		Assert.AreEqual(0u, batch.DeleteAfter);
		Assert.IsNull(pending.Take(12, 12).Commit);
	}

	[TestMethod]
	public void FocusChangeRejectsOldBatchAndClearsItsPayload()
	{
		var pending = new WaylandTextInputBatch();
		pending.Begin(10);
		pending.Commit = "pending";
		pending.Disable();
		Assert.AreEqual(WaylandTextInputBatchDisposition.Reject, pending.Take(10, 11).Disposition);
		pending.Begin(12);
		pending.Commit = "late";
		pending.Preedit = "old";
		pending.DeleteBefore = 1;
		var batch = pending.Take(10, 12);
		Assert.AreEqual(WaylandTextInputBatchDisposition.Reject, batch.Disposition);
		Assert.IsNull(batch.Commit);
		Assert.IsNull(batch.Preedit);
		Assert.IsNull(pending.Take(12, 12).Commit);
	}

	[TestMethod]
	public void CurrentBatchKeepsDeletionAndQueuedBatchBecomesOldAfterResync()
	{
		var pending = new WaylandTextInputBatch();
		pending.Begin(uint.MaxValue);
		pending.Commit = "text";
		pending.DeleteBefore = 2;
		pending.DeleteAfter = 3;
		var batch = pending.Take(uint.MaxValue, 0);
		Assert.AreEqual(WaylandTextInputBatchDisposition.Apply, batch.Disposition);
		Assert.AreEqual(2u, batch.DeleteBefore);
		Assert.AreEqual(3u, batch.DeleteAfter);
		pending.Resynchronize(1);
		Assert.AreEqual(WaylandTextInputBatchDisposition.ApplyWithoutDeletion, pending.GetDisposition(uint.MaxValue, 0));
		Assert.AreEqual(WaylandTextInputBatchDisposition.Apply, pending.GetDisposition(1, 1));
		Assert.AreEqual(WaylandTextInputBatchDisposition.Reject, pending.GetDisposition(uint.MaxValue - 1, 1));
	}
}
