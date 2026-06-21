#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Uno.HotReload.Client;

/// <summary>
/// Disposable scope returned by <see cref="UIUpdate.Pause"/>. While the handle is alive the
/// phases it owns are paused — incoming hot-reload types are queued rather than applied
/// directly. Disposing the handle (or waiting for GC to collect it) releases the pause; when
/// <em>all</em> handles are released the queued types are drained and applied.
/// </summary>
public sealed class HotReloadUIPauseHandle : IDisposable
{
	private readonly string? _acquiredByMember;
	private readonly string? _acquiredByFile;
	private readonly int _acquiredByLine;
	private readonly string? _reason;
	private int _disposed;

	internal HotReloadUIPauseHandle(
		HotReloadUIPhases phases,
		string? acquiredByMember,
		string? acquiredByFile,
		int acquiredByLine,
		string? reason)
	{
		Phases = phases;
		_acquiredByMember = acquiredByMember;
		_acquiredByFile = acquiredByFile;
		_acquiredByLine = acquiredByLine;
		_reason = reason;
	}

	/// <summary>The phases this handle pauses.</summary>
	public HotReloadUIPhases Phases { get; }

	/// <summary>Optional human-readable reason the pause was acquired.</summary>
	public string? Reason => _reason;

	/// <summary>
	/// Removes <paramref name="types"/> from the pending list for every phase this handle
	/// pauses. Idempotent and safe to call from any thread.
	/// </summary>
	public void Drop(params Type[] types)
		=> DropCore(types, caller: null, line: 0);

	/// <summary>
	/// Variant of <see cref="Drop(Type[])"/> that captures additional caller info for the
	/// diagnostic log entry.
	/// </summary>
	public void Drop(
		Type[] types,
		[CallerMemberName] string? caller = null,
		[CallerLineNumber] int line = 0)
		=> DropCore(types, caller, line);

	private void DropCore(Type[] types, string? caller, int line)
	{
		if (Volatile.Read(ref _disposed) != 0)
		{
			return;
		}

		if (types is null || types.Length == 0)
		{
			return;
		}

		PendingUIUpdates.Drop(Phases, types, acquiredBy: ToString(), reason: _reason, caller: caller, line: line);
	}

	/// <summary>
	/// Releases the pause for the phases this handle owns. Idempotent. Safe to call from any
	/// thread. Also invoked by the finalizer so a leaked handle doesn't permanently block the
	/// drain.
	/// </summary>
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}

		GC.SuppressFinalize(this);
		PendingUIUpdates.Release(this);
	}

	/// <summary>
	/// Fallback: releases the pause if the caller forgot to call <see cref="Dispose"/>.
	/// </summary>
	~HotReloadUIPauseHandle()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			PendingUIUpdates.Release(this);
		}
	}

	/// <summary>Diagnostic representation including acquisition site.</summary>
	public override string ToString()
	{
		var reasonPart = _reason is { Length: > 0 } ? $" reason='{_reason}'" : string.Empty;
		return $"HotReloadUIPauseHandle(Phases={Phases}{reasonPart}, acquiredAt={_acquiredByMember}@{_acquiredByFile}:{_acquiredByLine})";
	}
}
