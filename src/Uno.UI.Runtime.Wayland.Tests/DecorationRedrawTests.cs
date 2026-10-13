#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class DecorationRedrawTests
{
	[TestMethod]
	public void WithheldReleasesDoNotBlockTheReplacementParentCommit()
	{
		var state = new WaylandDecorationRedrawState();
		var busyBuffers = 0;
		void Draw()
		{
			if (state.BeginRedraw())
			{
				Assert.IsTrue(busyBuffers < 2, "Redraw exhausted the two-buffer pool.");
				busyBuffers++;
				state.CompleteRedraw(committed: true);
			}
		}
		Draw(); // A: compositor retains the current buffer.
		Assert.IsFalse(state.ParentCommitted());
		Draw(); // B: synchronized child state awaits the parent.
		for (var redraw = 0; redraw < 10; redraw++)
		{
			Draw(); // C and later changes coalesce without acquiring a third buffer.
		}
		Assert.AreEqual(2, busyBuffers);
		Assert.IsTrue(state.Dirty);
		Assert.IsTrue(state.PendingParentCommit);
		Assert.IsTrue(state.ParentCommitted()); // The parent must advance B even while C is dirty.
		busyBuffers--; // Only replacing A lets the compositor release it.
		Draw();
		Assert.IsFalse(state.Dirty);
		Assert.IsTrue(state.PendingParentCommit);
	}

	[TestMethod]
	public void BufferPressureRetriesTheLatestRedrawAfterRelease()
	{
		var state = new WaylandDecorationRedrawState();
		Assert.IsTrue(state.BeginRedraw());
		state.CompleteRedraw(committed: false);
		Assert.IsTrue(state.Dirty);
		Assert.IsFalse(state.PendingParentCommit);
		Assert.IsFalse(state.ParentCommitted());
		Assert.IsTrue(state.BeginRedraw());
		state.CompleteRedraw(committed: true);
		Assert.IsFalse(state.Dirty);
		Assert.IsFalse(state.ParentCommitted());
	}
}
