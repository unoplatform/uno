#nullable enable

namespace Uno.UI.Runtime.Wayland;

// Dispatch thread: at most one synchronized child commit waits for its parent.
internal sealed class WaylandDecorationRedrawState
{
	internal bool Dirty { get; private set; }
	internal bool PendingParentCommit { get; private set; }

	internal bool BeginRedraw()
	{
		Dirty = true;
		return !PendingParentCommit;
	}

	internal void CompleteRedraw(bool committed)
	{
		Dirty = !committed;
		PendingParentCommit = committed;
	}

	internal bool ParentCommitted()
	{
		var redraw = PendingParentCommit && Dirty;
		PendingParentCommit = false;
		return redraw;
	}
}
