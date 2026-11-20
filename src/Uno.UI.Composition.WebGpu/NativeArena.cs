// Native memory for what a recording or a frame keeps until it is released: allocated by bumping a pointer, freed
// all at once. The GC never traces or copies it, which is the point -- a recording lives until its visual repaints,
// so anything it holds on the managed heap survives a collection and is copied into an older generation.
#nullable disable
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Uno.UI.Composition.WebGpu;

/// <summary>
/// A bump allocator over native chunks, released together. One writer at a time; any thread may release it.
/// Refcounted: whatever reads its memory past its owner's lifetime (an arena entry built from a recording and shared
/// with others) takes a reference.
/// </summary>
internal sealed unsafe class NativeArena
{
	// Each chunk starts with a link to the previous one and its size, so the arena needs no managed list.
	private struct ChunkHeader
	{
		public ChunkHeader* Previous;
		public nuint Size;
	}

	private const int Align = 16;
	private static readonly int HeaderSize = (sizeof(ChunkHeader) + Align - 1) & ~(Align - 1);

	private ChunkHeader* _chunk;
	private byte* _cur;
	private byte* _end;
	private int _refs = 1;

	/// <summary>Native bytes the arena holds, all chunks included.</summary>
	internal nuint Reserved { get; private set; }

	public T* Alloc<T>(int count) where T : unmanaged => (T*)Alloc(sizeof(T) * count);

	public void* Alloc(int bytes)
	{
		var size = (bytes + Align - 1) & ~(Align - 1);
		if (_cur + size > _end)
		{
			NewChunk(size);
		}

		var p = _cur;
		_cur += size;
		return p;
	}

	private void NewChunk(int minimum)
	{
		// Small first (most recordings hold a handful of entries), growing for the ones that hold many.
		var size = NativeChunkPool.SizeFor(HeaderSize + minimum, _chunk is null ? 0 : _chunk->Size);
		var chunk = (ChunkHeader*)NativeChunkPool.Rent(size);
		chunk->Previous = _chunk;
		chunk->Size = size;
		_chunk = chunk;
		_cur = (byte*)chunk + HeaderSize;
		_end = (byte*)chunk + size;
		Reserved += size;
	}

	public void AddRef() => Interlocked.Increment(ref _refs);

	public void Release()
	{
		if (Interlocked.Decrement(ref _refs) == 0)
		{
			FreeChunks();
		}
	}

	/// <summary>Frees everything allocated so far and starts over, for an owner that rewrites its whole content.</summary>
	public void Reset()
	{
		FreeChunks();
	}

	private void FreeChunks()
	{
		var chunk = _chunk;
		_chunk = null;
		_cur = _end = null;
		Reserved = 0;
		while (chunk is not null)
		{
			var previous = chunk->Previous;
			NativeChunkPool.Return(chunk, chunk->Size);
			chunk = previous;
		}
	}

	/// <summary>Copies <paramref name="source"/> into this arena.</summary>
	public T* Copy<T>(ReadOnlySpan<T> source) where T : unmanaged
	{
		if (source.IsEmpty)
		{
			return null;
		}

		var p = Alloc<T>(source.Length);
		source.CopyTo(new Span<T>(p, source.Length));
		return p;
	}
}

/// <summary>Recycles the arenas' chunks across owners, so a recording made every frame does not reach the OS allocator.</summary>
internal static unsafe class NativeChunkPool
{
	// Powers of four from 1 KB to 256 KB; anything larger is allocated and freed directly.
	private const int MinShift = 10, MaxShift = 18, Classes = (MaxShift - MinShift) / 2 + 1;
	private static readonly Stack<nint>[] s_free = CreateClasses();
	private static readonly int[] s_maxPooled = { 8192, 2048, 512, 128, 32 };

	// Debug aid: freed memory is overwritten with NaNs, so a read through a stale pointer shows up as missing clips.
	private static readonly bool s_poison = Environment.GetEnvironmentVariable("UNO_WEBGPU_ARENA_POISON") == "1";

	private static Stack<nint>[] CreateClasses()
	{
		var classes = new Stack<nint>[Classes];
		for (var i = 0; i < classes.Length; i++)
		{
			classes[i] = new Stack<nint>();
		}
		return classes;
	}

	/// <summary>The chunk size to rent for <paramref name="minimum"/> bytes, after a chunk of <paramref name="previous"/>.</summary>
	internal static nuint SizeFor(int minimum, nuint previous)
	{
		var size = previous == 0 ? 1u << MinShift : Math.Min(previous << 2, 1u << MaxShift);
		while (size < (nuint)minimum)
		{
			size <<= 2;
		}
		return size;
	}

	private static int ClassOf(nuint size)
	{
		for (int i = 0, shift = MinShift; i < Classes; i++, shift += 2)
		{
			if (size == 1u << shift)
			{
				return i;
			}
		}
		return -1;
	}

	internal static void* Rent(nuint size)
	{
		var c = ClassOf(size);
		if (c >= 0)
		{
			var free = s_free[c];
			lock (free)
			{
				if (free.Count > 0)
				{
					return (void*)free.Pop();
				}
			}
		}

		return NativeMemory.Alloc(size);
	}

	internal static void Return(void* chunk, nuint size)
	{
		if (s_poison)
		{
			new Span<byte>(chunk, (int)size).Fill(0xFF);
		}

		var c = ClassOf(size);
		if (c >= 0)
		{
			var free = s_free[c];
			lock (free)
			{
				if (free.Count < s_maxPooled[c])
				{
					free.Push((nint)chunk);
					return;
				}
			}
		}

		NativeMemory.Free(chunk);
	}
}

/// <summary>A run of clip entries in native memory (see <see cref="NativeArena"/>); never written once built.</summary>
internal readonly unsafe struct ClipEntries
{
	private readonly ClipEntry* _ptr;

	public ClipEntries(ClipEntry* ptr, int length)
	{
		_ptr = length > 0 ? ptr : null;
		Length = length > 0 ? length : 0;
	}

	public int Length { get; }

	public bool IsEmpty => Length == 0;

	public ref readonly ClipEntry this[int index] => ref _ptr[index];

	public ReadOnlySpan<ClipEntry> Span => new(_ptr, Length);

	public ReadOnlySpan<ClipEntry>.Enumerator GetEnumerator() => Span.GetEnumerator();

	/// <summary>The same run of memory: what an array reference comparison used to answer.</summary>
	public bool SameAs(in ClipEntries other) => _ptr == other._ptr && Length == other.Length;

	public nint Address => (nint)_ptr;

	public static ClipEntries Copy(NativeArena arena, ReadOnlySpan<ClipEntry> source)
		=> source.IsEmpty ? default : new ClipEntries(arena.Copy(source), source.Length);

	/// <summary><paramref name="prefix"/> followed by <paramref name="count"/> unwritten entries, for the caller to fill.</summary>
	public static ClipEntries Extend(NativeArena arena, in ClipEntries prefix, int count, out Span<ClipEntry> tail)
	{
		var n = prefix.Length + count;
		var p = arena.Alloc<ClipEntry>(n);
		var all = new Span<ClipEntry>(p, n);
		prefix.Span.CopyTo(all);
		tail = all.Slice(prefix.Length);
		return new ClipEntries(p, n);
	}
}
