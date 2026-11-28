#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Silk.NET.WebGPU;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// Every bind group the backend reuses, keyed by its layout and what each of its entries binds. Per-frame users (pass and bake
/// groups over pooled resources) simply look a group up each frame; a user that keeps a group past the frame (a site's
/// stamp, a recording's clip) pins it until it lets go. Unpinned groups are dropped once they go idle or a pool
/// destroys something they name.
/// </summary>
/// <remarks>
/// The clip layout's uniform binding takes a dynamic offset, so one clip group serves every slot of a slab buffer: an op
/// carries the shared group and its slot's offset (<see cref="ClipBinding"/>).
/// </remarks>
internal sealed unsafe class WebGpuGroupCache
{
	// One entry as the group binds it: the buffer, view or sampler, and for a buffer its range.
	internal readonly record struct Binding(uint Slot, nint Handle, ulong Offset, ulong Size);

	// The widest layout cached is the clip group's five entries.
	private const int MaxEntries = 5;

	internal readonly record struct Key(nint Layout, Binding E0, Binding E1, Binding E2, Binding E3, Binding E4)
	{
		public bool Names(HashSet<nint> handles)
			=> handles.Contains(E0.Handle) || handles.Contains(E1.Handle) || handles.Contains(E2.Handle)
				|| handles.Contains(E3.Handle) || handles.Contains(E4.Handle);
	}

	private static Key KeyOf(BindGroupDescriptor* desc)
	{
		if (desc->EntryCount > MaxEntries) { throw new ArgumentException($"A cached bind group has at most {MaxEntries} entries."); }
		Span<Binding> b = stackalloc Binding[MaxEntries];
		for (var i = 0; i < (int)desc->EntryCount; i++)
		{
			var e = desc->Entries[i];
			var handle = e.Buffer != null ? (nint)e.Buffer : e.TextureView != null ? (nint)e.TextureView : (nint)e.Sampler;
			b[i] = new Binding(e.Binding, handle, e.Offset, e.Size);
		}
		return new Key((nint)desc->Layout, b[0], b[1], b[2], b[3], b[4]);
	}

	internal sealed class Entry
	{
		public BindGroup* Bg;
		internal Key Key;
		public int Pins;
		public long LastUsed;
	}

	// Frames an unpinned group may go unused before it is dropped; checked every SweepEvery frames.
	private const int IdleFrames = 120;
	private const int SweepEvery = 60;

	private readonly WebGpuDevice _d;
	private readonly object _gate = new();
	private readonly Dictionary<Key, Entry> _groups = new();
	private readonly Dictionary<nint, Entry> _byGroup = new();
	private readonly HashSet<nint> _destroyed = new();
	private readonly ConcurrentQueue<Entry> _pendingUnpins = new();
	// Pinned groups naming a destroyed handle: no longer handed out (a new resource may reuse the handle), released
	// once their holders let go.
	private readonly List<Entry> _orphans = new();

	public WebGpuGroupCache(WebGpuDevice d) => _d = d;

	/// <summary>The group <paramref name="desc"/> describes, created on a miss.</summary>
	public BindGroup* Get(BindGroupDescriptor* desc) => Lookup(desc, pin: false).Bg;

	/// <summary>As <see cref="Get"/>, holding the group until <see cref="Unpin"/>.</summary>
	public Entry GetPinned(BindGroupDescriptor* desc) => Lookup(desc, pin: true);

	private Entry Lookup(BindGroupDescriptor* desc, bool pin)
	{
		var key = KeyOf(desc);
		lock (_gate)
		{
			if (!_groups.TryGetValue(key, out var e))
			{
				_groups[key] = e = new Entry { Bg = _d.Api.DeviceCreateBindGroup(_d.Dev, desc), Key = key };
				_byGroup[(nint)e.Bg] = e;
			}
			e.LastUsed = _d.FrameSeq;
			if (pin) { e.Pins++; }
			return e;
		}
	}

	public void Unpin(Entry e)
	{
		lock (_gate)
		{
			e.Pins--;
			e.LastUsed = _d.FrameSeq;
		}
	}

	/// <summary>Lets go of a group taken with <see cref="GetPinned"/>, by its handle.</summary>
	public void Unpin(BindGroup* bg)
	{
		lock (_gate)
		{
			if (_byGroup.TryGetValue((nint)bg, out var e)) { e.Pins--; e.LastUsed = _d.FrameSeq; }
		}
	}

	/// <summary>A pool or the atlas destroyed this handle: the unpinned groups naming it go at the next frame start.</summary>
	public void NoteDestroyed(nint handle)
	{
		lock (_gate) { _destroyed.Add(handle); }
	}

	private void Release(Entry e)
	{
		_byGroup.Remove((nint)e.Bg);
		_d.Api.BindGroupRelease(e.Bg);
	}

	/// <summary>Releases every group, pinned or not: the device is going away, and nothing will bind them again.</summary>
	public void Dispose()
	{
		lock (_gate)
		{
			foreach (var e in _groups.Values)
			{
				_d.Api.BindGroupRelease(e.Bg);
			}
			foreach (var o in _orphans)
			{
				_d.Api.BindGroupRelease(o.Bg);
			}
			_orphans.Clear();
			_groups.Clear();
			_byGroup.Clear();
			_destroyed.Clear();
		}
		while (_pendingUnpins.TryDequeue(out _)) { }
	}

	/// <summary>Lets go of a pinned group at the next frame start: the frame encoding now may still bind it.</summary>
	public void UnpinNextFrame(Entry e) => _pendingUnpins.Enqueue(e);

	/// <summary>Unpins the groups released last frame, then drops unpinned groups that name a destroyed handle or went idle.</summary>
	public void BeginFrame()
	{
		while (_pendingUnpins.TryDequeue(out var pending)) { Unpin(pending); }
		lock (_gate)
		{
			var sweep = _d.FrameSeq % SweepEvery == 0;
			if (_destroyed.Count == 0 && !sweep && _orphans.Count == 0) { return; }
			List<Entry>? stale = null;
			List<Entry>? orphaned = null;
			foreach (var e in _groups.Values)
			{
				if (_destroyed.Count > 0 && e.Key.Names(_destroyed))
				{
					if (e.Pins > 0) { (orphaned ??= new()).Add(e); }
					else { (stale ??= new()).Add(e); }
				}
				else if (sweep && e.Pins == 0 && _d.FrameSeq - e.LastUsed > IdleFrames)
				{
					(stale ??= new()).Add(e);
				}
			}
			_destroyed.Clear();
			if (orphaned is not null)
			{
				foreach (var o in orphaned) { _groups.Remove(o.Key); }
				_orphans.AddRange(orphaned);
			}
			for (var i = _orphans.Count - 1; i >= 0; i--)
			{
				if (_orphans[i].Pins > 0) { continue; }
				Release(_orphans[i]);
				_orphans.RemoveAt(i);
			}
			if (stale is null) { return; }
			foreach (var e in stale)
			{
				_groups.Remove(e.Key);
				Release(e);
			}
		}
	}
}
