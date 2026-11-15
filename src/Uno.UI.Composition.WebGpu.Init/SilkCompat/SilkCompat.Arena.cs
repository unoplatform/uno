#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Uno.UI.Composition.WebGpu.SilkCompat;

/// <summary>
/// Native memory for one translated call's converted descriptors. Each thread reuses its blocks: renting marks where
/// the call's allocations start and disposing rolls back to the mark, so a callback that makes a call nests safely.
/// </summary>
internal unsafe struct Arena : IDisposable
{
	private const int BlockSize = 64 * 1024;

	[ThreadStatic]
	private static Pool? t_pool;

	private Pool _pool;
	private (int Block, int Offset) _mark;

	public static Arena Rent()
	{
		var pool = t_pool ??= new Pool();
		return new Arena { _pool = pool, _mark = (pool.Block, pool.Offset) };
	}

	/// <summary>Zeroed, 16-byte aligned memory valid until this rental is disposed.</summary>
	public void* Alloc(int size) => _pool.Alloc(size);

	public void Dispose() => (_pool.Block, _pool.Offset) = _mark;

	private sealed class Pool
	{
		private readonly List<(nint Memory, int Size)> _blocks = new();
		public int Block;
		public int Offset;

		public void* Alloc(int size)
		{
			size = Math.Max(1, size);
			var offset = (Offset + 15) & ~15;
			while (Block < _blocks.Count && offset + size > _blocks[Block].Size)
			{
				Block++;
				offset = 0;
			}

			if (Block == _blocks.Count)
			{
				var blockSize = Math.Max(BlockSize, size);
				_blocks.Add(((nint)NativeMemory.AlignedAlloc((nuint)blockSize, 16), blockSize));
				offset = 0;
			}

			var memory = (byte*)_blocks[Block].Memory + offset;
			Offset = offset + size;
			NativeMemory.Clear(memory, (nuint)size);
			return memory;
		}
	}
}
