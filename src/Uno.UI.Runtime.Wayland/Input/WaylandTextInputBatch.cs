#nullable enable

namespace Uno.UI.Runtime.Wayland;

internal enum WaylandTextInputBatchDisposition
{
	Reject,
	Apply,
	ApplyWithoutDeletion,
}

// The caller holds the display lock; resync changes deletion validity, not focus ownership.
internal sealed class WaylandTextInputBatch
{
	private uint _sessionFirstSerial;
	private uint _surroundingFirstSerial;
	private bool _enabled;

	internal string? Preedit { get; set; }
	internal int PreeditCursor { get; set; }
	internal string? Commit { get; set; }
	internal uint DeleteBefore { get; set; }
	internal uint DeleteAfter { get; set; }

	internal readonly record struct Batch(WaylandTextInputBatchDisposition Disposition, string? Preedit, int PreeditCursor, string? Commit, uint DeleteBefore, uint DeleteAfter);

	internal void Begin(uint firstSerial)
	{
		Reset();
		_enabled = true;
		_sessionFirstSerial = _surroundingFirstSerial = firstSerial;
	}

	internal void Disable()
	{
		_enabled = false;
		Reset();
	}

	internal void Resynchronize(uint firstSerial) => _surroundingFirstSerial = firstSerial;

	internal WaylandTextInputBatchDisposition GetDisposition(uint serial, uint latestSerial)
	{
		if (!_enabled || !IsSerialInSession(serial, _sessionFirstSerial, latestSerial))
		{
			return WaylandTextInputBatchDisposition.Reject;
		}
		return IsSerialInSession(serial, _surroundingFirstSerial, latestSerial)
			? WaylandTextInputBatchDisposition.Apply
			: WaylandTextInputBatchDisposition.ApplyWithoutDeletion;
	}

	internal Batch Take(uint serial, uint latestSerial)
	{
		var disposition = GetDisposition(serial, latestSerial);
		var batch = disposition == WaylandTextInputBatchDisposition.Reject
			? new Batch(disposition, null, 0, null, 0, 0)
			: new Batch(disposition, Preedit, PreeditCursor, Commit,
				disposition == WaylandTextInputBatchDisposition.Apply ? DeleteBefore : 0,
				disposition == WaylandTextInputBatchDisposition.Apply ? DeleteAfter : 0);
		Reset();
		return batch;
	}

	internal static bool IsSerialInSession(uint serial, uint first, uint latest)
		=> first != unchecked(latest + 1) && unchecked(serial - first) <= unchecked(latest - first);

	private void Reset()
	{
		Preedit = Commit = null;
		PreeditCursor = 0;
		DeleteBefore = DeleteAfter = 0;
	}
}
