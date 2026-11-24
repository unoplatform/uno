#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Uno.UI.Composition.WebGpu.SilkCompat;

// wgpu-native allocates and frees a staging buffer for every queue write, ~5 µs (Vulkan) to ~30 µs (D3D12) per call.
// Writes are copied into a per-queue arena instead, and the next submit uploads the arena with one write and prepends
// a command buffer copying each range into place. Queue writes only promise to land before the next submit's
// commands, so that ordering is kept, and a write that bypasses the arena first submits the copies pending ahead of
// it. Only the renderer's table batches (see SilkCompatContext): a write deferred to the next submit would change
// what third-party code sees (a destroy or map between the write and its submit). One trade-off: a write that would
// have failed on its own (say, to a buffer destroyed before the submit) now fails the whole upload command buffer.
internal static unsafe partial class Xlate
{
	private sealed class PendingUploads
	{
		public byte[] Data = new byte[InitialArena];
		public int Used;
		public int QuietSubmits;
		public readonly List<(nint Buffer, ulong Offset, int Source, int Size)> Copies = new();
	}

	private const int InitialArena = 1 << 16;
	// Writes this large gain nothing from the arena; they go straight to the native call.
	private const ulong DirectWriteBytes = 1 << 20;
	// Submits an oversized arena may go below a quarter full before it shrinks back.
	private const int ShrinkAfterSubmits = 120;

	private static readonly ConcurrentDictionary<nint, nint> _queueDevice = new();
	private static readonly ConcurrentDictionary<nint, PendingUploads> _uploads = new();
	// The references the renderer holds on each queue: batching for a queue ends with the last one, not the first.
	private static readonly Dictionary<nint, int> _queueRefs = new();
	private static delegate* unmanaged[Cdecl]<nint, nint> _nativeGetQueue;
	private static delegate* unmanaged[Cdecl]<nint, nint, ulong, nint, nuint, void> _nativeWriteBuffer;
	private static delegate* unmanaged[Cdecl]<nint, void> _nativeQueueReference;
	private static delegate* unmanaged[Cdecl]<nint, void> _nativeQueueRelease;
	private static delegate* unmanaged[Cdecl]<nint, nuint, nint, void> _nativeSubmit;
	private static delegate* unmanaged[Cdecl]<nint, nuint, nint, ulong> _nativeSubmitForIndex;

	private static void AddUploads(Dictionary<string, nint> t)
	{
		if (OperatingSystem.IsBrowser())
		{
			return;
		}

		_nativeGetQueue = (delegate* unmanaged[Cdecl]<nint, nint>)t["wgpuDeviceGetQueue"];
		_nativeWriteBuffer = (delegate* unmanaged[Cdecl]<nint, nint, ulong, nint, nuint, void>)t["wgpuQueueWriteBuffer"];
		_nativeQueueRelease = (delegate* unmanaged[Cdecl]<nint, void>)t["wgpuQueueRelease"];
		_nativeSubmit = (delegate* unmanaged[Cdecl]<nint, nuint, nint, void>)t["wgpuQueueSubmit"];
		_nativeSubmitForIndex = (delegate* unmanaged[Cdecl]<nint, nuint, nint, ulong>)t["wgpuQueueSubmitForIndex"];
		t["wgpuDeviceGetQueue"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint>)&BatchedGetQueue;
		t["wgpuQueueWriteBuffer"] = (nint)(delegate* unmanaged[Cdecl]<nint, nint, ulong, nint, nuint, void>)&BatchedWriteBuffer;
		t["wgpuQueueRelease"] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&BatchedQueueRelease;
		t["wgpuQueueSubmit"] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nint, void>)&BatchedSubmit;
		if (t.TryGetValue("wgpuQueueReference", out var reference) && reference != 0)
		{
			_nativeQueueReference = (delegate* unmanaged[Cdecl]<nint, void>)reference;
			t["wgpuQueueReference"] = (nint)(delegate* unmanaged[Cdecl]<nint, void>)&BatchedQueueReference;
		}
		if (_nativeSubmitForIndex != null)
		{
			t["wgpuQueueSubmitForIndex"] = (nint)(delegate* unmanaged[Cdecl]<nint, nuint, nint, ulong>)&BatchedSubmitForIndex;
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static nint BatchedGetQueue(nint device)
	{
		var queue = _nativeGetQueue(device);
		lock (_queueRefs)
		{
			_queueRefs[queue] = _queueRefs.TryGetValue(queue, out var n) ? n + 1 : 1;
			_queueDevice[queue] = device;
		}
		return queue;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void BatchedQueueReference(nint queue)
	{
		lock (_queueRefs)
		{
			if (_queueRefs.TryGetValue(queue, out var n))
			{
				_queueRefs[queue] = n + 1;
			}
		}
		_nativeQueueReference(queue);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void BatchedWriteBuffer(nint queue, nint buffer, ulong offset, nint data, nuint size)
	{
		if (size == 0)
		{
			return;
		}
		// The arena needs the queue's device to flush, and copies move whole 4-byte words: anything else, and any
		// large write, keeps the native call (and its validation at the call).
		if ((size & 3) != 0 || size >= DirectWriteBytes || !_queueDevice.TryGetValue(queue, out var device))
		{
			WriteDirect(queue, buffer, offset, data, size);
			return;
		}
		var p = _uploads.GetOrAdd(queue, static _ => new PendingUploads());
		lock (p)
		{
			var n = (int)size;
			var at = p.Used;
			if (at + n > p.Data.Length)
			{
				Array.Resize(ref p.Data, Math.Max(p.Data.Length * 2, at + n));
			}
			new ReadOnlySpan<byte>((void*)data, n).CopyTo(p.Data.AsSpan(at, n));
			p.Used = at + n;
			// The caller may release the buffer before the copy is encoded.
			WGPU.wgpuBufferAddRef(buffer);
			p.Copies.Add((buffer, offset, at, n));
		}
	}

	// A native write lands when it is called, while pending copies only land at the next submit: submit them first, so
	// an earlier batched write to the same range cannot land after this one.
	private static void WriteDirect(nint queue, nint buffer, ulong offset, nint data, nuint size)
	{
		if (!_uploads.TryGetValue(queue, out var p) || !_queueDevice.TryGetValue(queue, out var device))
		{
			_nativeWriteBuffer(queue, buffer, offset, data, size);
			return;
		}
		lock (p)
		{
			if (p.Copies.Count > 0)
			{
				SubmitPendingLocked(queue, device, p);
			}
			_nativeWriteBuffer(queue, buffer, offset, data, size);
		}
	}

	// The renderer released its last reference: the pending writes are submitted rather than dropped, and the queue's
	// later writes and submits go straight to the native calls.
	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void BatchedQueueRelease(nint queue)
	{
		var last = false;
		lock (_queueRefs)
		{
			if (_queueRefs.TryGetValue(queue, out var n))
			{
				last = n <= 1;
				if (last) { _queueRefs.Remove(queue); } else { _queueRefs[queue] = n - 1; }
			}
		}
		if (last && _queueDevice.TryRemove(queue, out var device) && _uploads.TryRemove(queue, out var p))
		{
			lock (p)
			{
				if (p.Copies.Count > 0)
				{
					SubmitPendingLocked(queue, device, p);
				}
			}
		}
		_nativeQueueRelease(queue);
	}

	private static void SubmitPendingLocked(nint queue, nint device, PendingUploads p)
	{
		var upload = FlushLocked(queue, device, p);
		if (upload != 0)
		{
			_nativeSubmit(queue, 1, (nint)(&upload));
			WGPU.wgpuCommandBufferRelease(upload);
		}
	}

	// Encodes the pending writes as one staging upload plus copies, or returns 0 when there are none. Called under
	// lock (p), which the caller keeps until the result is submitted: another thread's flush cannot then take writes
	// whose own submit would run first.
	private static nint FlushLocked(nint queue, nint device, PendingUploads p)
	{
		if (p.Copies.Count == 0)
		{
			p.QuietSubmits++;
			ShrinkIfQuiet(p);
			return 0;
		}
		var size = (ulong)p.Used;
		var bd = new WGPUBufferDescriptor { Usage = WGPUBufferUsage.CopySrc | WGPUBufferUsage.CopyDst, Size = size };
		var staging = WGPU.wgpuDeviceCreateBuffer(device, &bd);
		if (staging == 0)
		{
			// No staging buffer (out of memory, or the device is lost): write each range directly instead.
			fixed (byte* src = p.Data)
			{
				foreach (var (buffer, offset, source, n) in p.Copies)
				{
					_nativeWriteBuffer(queue, buffer, offset, (nint)(src + source), (nuint)n);
					WGPU.wgpuBufferRelease(buffer);
				}
			}
			Reset(p);
			return 0;
		}
		fixed (byte* src = p.Data)
		{
			_nativeWriteBuffer(queue, staging, 0, (nint)src, (nuint)size);
		}
		var encoder = WGPU.wgpuDeviceCreateCommandEncoder(device, null);
		foreach (var (buffer, offset, source, n) in p.Copies)
		{
			WGPU.wgpuCommandEncoderCopyBufferToBuffer(encoder, staging, (ulong)source, buffer, offset, (ulong)n);
			WGPU.wgpuBufferRelease(buffer);
		}
		var commands = WGPU.wgpuCommandEncoderFinish(encoder, null);
		WGPU.wgpuCommandEncoderRelease(encoder);
		WGPU.wgpuBufferRelease(staging);
		Reset(p);
		return commands;
	}

	private static void Reset(PendingUploads p)
	{
		p.QuietSubmits = p.Used * 4 < p.Data.Length ? p.QuietSubmits + 1 : 0;
		p.Copies.Clear();
		p.Used = 0;
		ShrinkIfQuiet(p);
	}

	// One large frame grows the arena; give the memory back once frames stay small for a while.
	private static void ShrinkIfQuiet(PendingUploads p)
	{
		if (p.Data.Length > InitialArena && p.QuietSubmits >= ShrinkAfterSubmits)
		{
			p.Data = new byte[Math.Max(InitialArena, p.Data.Length / 2)];
			p.QuietSubmits = 0;
		}
	}

	private static nint* WithUploads(nint upload, nuint count, nint commands, nint* scratch)
	{
		scratch[0] = upload;
		for (nuint i = 0; i < count; i++)
		{
			scratch[i + 1] = ((nint*)commands)[i];
		}
		return scratch;
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static void BatchedSubmit(nint queue, nuint count, nint commands)
	{
		if (!_uploads.TryGetValue(queue, out var p) || !_queueDevice.TryGetValue(queue, out var device))
		{
			_nativeSubmit(queue, count, commands);
			return;
		}
		lock (p)
		{
			var upload = FlushLocked(queue, device, p);
			if (upload == 0)
			{
				_nativeSubmit(queue, count, commands);
				return;
			}
			Span<nint> all = count < 64 ? stackalloc nint[(int)count + 1] : new nint[(int)count + 1];
			fixed (nint* scratch = all)
			{
				_nativeSubmit(queue, count + 1, (nint)WithUploads(upload, count, commands, scratch));
			}
			WGPU.wgpuCommandBufferRelease(upload);
		}
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static ulong BatchedSubmitForIndex(nint queue, nuint count, nint commands)
	{
		if (!_uploads.TryGetValue(queue, out var p) || !_queueDevice.TryGetValue(queue, out var device))
		{
			return _nativeSubmitForIndex(queue, count, commands);
		}
		lock (p)
		{
			var upload = FlushLocked(queue, device, p);
			if (upload == 0)
			{
				return _nativeSubmitForIndex(queue, count, commands);
			}
			Span<nint> all = count < 64 ? stackalloc nint[(int)count + 1] : new nint[(int)count + 1];
			ulong index;
			fixed (nint* scratch = all)
			{
				index = _nativeSubmitForIndex(queue, count + 1, (nint)WithUploads(upload, count, commands, scratch));
			}
			WGPU.wgpuCommandBufferRelease(upload);
			return index;
		}
	}
}
