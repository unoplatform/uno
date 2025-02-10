// Imported from https://github.com/microsoft/Microsoft.IO.RecyclableMemoryStream/commit/7df2ef7c95979774f783d4870cb959f03c38aade

namespace Microsoft.IO
{
	using System;

	public sealed partial class RecyclableMemoryStreamManager
	{
		/// <summary>
		/// Arguments for the <see cref="StreamCreated"/> event.
		/// </summary>
		/// <remarks>
		/// Initializes a new instance of the <see cref="StreamCreatedEventArgs"/> class.
		/// </remarks>
		/// <param name="guid">Unique ID of the stream.</param>
		/// <param name="tag">Tag of the stream.</param>
		/// <param name="requestedSize">The requested stream size.</param>
		/// <param name="actualSize">The actual stream size.</param>
		public sealed class StreamCreatedEventArgs(Guid guid, string? tag, long requestedSize, long actualSize) : EventArgs
		{
			/// <summary>
			/// Unique ID for the stream.
			/// </summary>
			public Guid Id { get; } = guid;

			/// <summary>
			/// Optional Tag for the event.
			/// </summary>
			public string? Tag { get; } = tag;

			/// <summary>
			/// Requested stream size.
			/// </summary>
			public long RequestedSize { get; } = requestedSize;

			/// <summary>
			/// Actual stream size.
			/// </summary>
			public long ActualSize { get; } = actualSize;
		}

		/// <summary>
		/// Arguments for the <see cref="StreamDisposed"/> event.
		/// </summary>
		/// <remarks>
		/// Initializes a new instance of the <see cref="StreamDisposedEventArgs"/> class.
		/// </remarks>
		/// <param name="guid">Unique ID of the stream.</param>
		/// <param name="tag">Tag of the stream.</param>
		/// <param name="lifetime">Lifetime of the stream</param>
		/// <param name="allocationStack">Stack of original allocation.</param>
		/// <param name="disposeStack">Dispose stack.</param>
		public sealed class StreamDisposedEventArgs(Guid guid, string? tag, TimeSpan lifetime, string? allocationStack, string? disposeStack) : EventArgs
		{
			/// <summary>
			/// Unique ID for the stream.
			/// </summary>
			public Guid Id { get; } = guid;

			/// <summary>
			/// Optional Tag for the event.
			/// </summary>
			public string? Tag { get; } = tag;

			/// <summary>
			/// Stack where the stream was allocated.
			/// </summary>
			public string? AllocationStack { get; } = allocationStack;

			/// <summary>
			/// Stack where stream was disposed.
			/// </summary>
			public string? DisposeStack { get; } = disposeStack;

			/// <summary>
			/// Lifetime of the stream.
			/// </summary>
			public TimeSpan Lifetime { get; } = lifetime;
		}

		/// <summary>
		/// Arguments for the <see cref="StreamDoubleDisposed"/> event.
		/// </summary>
		/// <remarks>
		/// Initializes a new instance of the <see cref="StreamDoubleDisposedEventArgs"/> class.
		/// </remarks>
		/// <param name="guid">Unique ID of the stream.</param>
		/// <param name="tag">Tag of the stream.</param>
		/// <param name="allocationStack">Stack of original allocation.</param>
		/// <param name="disposeStack1">First dispose stack.</param>
		/// <param name="disposeStack2">Second dispose stack.</param>
		public sealed class StreamDoubleDisposedEventArgs(Guid guid, string? tag, string? allocationStack, string? disposeStack1, string? disposeStack2) : EventArgs
		{
			/// <summary>
			/// Unique ID for the stream.
			/// </summary>
			public Guid Id { get; } = guid;

			/// <summary>
			/// Optional Tag for the event.
			/// </summary>
			public string? Tag { get; } = tag;

			/// <summary>
			/// Stack where the stream was allocated.
			/// </summary>
			public string? AllocationStack { get; } = allocationStack;

			/// <summary>
			/// First dispose stack.
			/// </summary>
			public string? DisposeStack1 { get; } = disposeStack1;

			/// <summary>
			/// Second dispose stack.
			/// </summary>
			public string? DisposeStack2 { get; } = disposeStack2;
		}

		/// <summary>
		/// Arguments for the <see cref="StreamFinalized"/> event.
		/// </summary>
		/// <remarks>
		/// Initializes a new instance of the <see cref="StreamFinalizedEventArgs"/> class.
		/// </remarks>
		/// <param name="guid">Unique ID of the stream.</param>
		/// <param name="tag">Tag of the stream.</param>
		/// <param name="allocationStack">Stack of original allocation.</param>
		public sealed class StreamFinalizedEventArgs(Guid guid, string? tag, string? allocationStack) : EventArgs
		{
			/// <summary>
			/// Unique ID for the stream.
			/// </summary>
			public Guid Id { get; } = guid;

			/// <summary>
			/// Optional Tag for the event.
			/// </summary>
			public string? Tag { get; } = tag;

			/// <summary>
			/// Stack where the stream was allocated.
			/// </summary>
			public string? AllocationStack { get; } = allocationStack;
		}

		/// <summary>
		/// Arguments for the <see cref="StreamConvertedToArray"/> event.
		/// </summary>
		/// <remarks>
		/// Initializes a new instance of the <see cref="StreamConvertedToArrayEventArgs"/> class.
		/// </remarks>
		/// <param name="guid">Unique ID of the stream.</param>
		/// <param name="tag">Tag of the stream.</param>
		/// <param name="stack">Stack of ToArray call.</param>
		/// <param name="length">Length of stream.</param>
		public sealed class StreamConvertedToArrayEventArgs(Guid guid, string? tag, string? stack, long length) : EventArgs
		{
			/// <summary>
			/// Unique ID for the stream.
			/// </summary>
			public Guid Id { get; } = guid;

			/// <summary>
			/// Optional Tag for the event.
			/// </summary>
			public string? Tag { get; } = tag;

			/// <summary>
			/// Stack where ToArray was called.
			/// </summary>
			public string? Stack { get; } = stack;

			/// <summary>
			/// Length of stack.
			/// </summary>
			public long Length { get; } = length;
		}

		/// <summary>
		/// Arguments for the <see cref="StreamOverCapacity"/> event.
		/// </summary>
		public sealed class StreamOverCapacityEventArgs : EventArgs
		{
			/// <summary>
			/// Unique ID for the stream.
			/// </summary>
			public Guid Id { get; }

			/// <summary>
			/// Optional Tag for the event.
			/// </summary>
			public string? Tag { get; }

			/// <summary>
			/// Original allocation stack.
			/// </summary>
			public string? AllocationStack { get; }

			/// <summary>
			/// Requested capacity.
			/// </summary>
			public long RequestedCapacity { get; }

			/// <summary>
			/// Maximum capacity.
			/// </summary>
			public long MaximumCapacity { get; }

			/// <summary>
			/// Initializes a new instance of the <see cref="StreamOverCapacityEventArgs"/> class.
			/// </summary>
			/// <param name="guid">Unique ID of the stream.</param>
			/// <param name="tag">Tag of the stream.</param>
			/// <param name="requestedCapacity">Requested capacity.</param>
			/// <param name="maximumCapacity">Maximum stream capacity of the manager.</param>
			/// <param name="allocationStack">Original allocation stack.</param>
			internal StreamOverCapacityEventArgs(Guid guid, string? tag, long requestedCapacity, long maximumCapacity, string? allocationStack)
			{
				this.Id = guid;
				this.Tag = tag;
				this.RequestedCapacity = requestedCapacity;
				this.MaximumCapacity = maximumCapacity;
				this.AllocationStack = allocationStack;
			}
		}

		/// <summary>
		/// Arguments for the <see cref="BlockCreated"/> event.
		/// </summary>
		public sealed class BlockCreatedEventArgs : EventArgs
		{
			/// <summary>
			/// How many bytes are currently in use from the small pool.
			/// </summary>
			public long SmallPoolInUse { get; }

			/// <summary>
			/// Initializes a new instance of the <see cref="BlockCreatedEventArgs"/> class.
			/// </summary>
			/// <param name="smallPoolInUse">Number of bytes currently in use from the small pool.</param>
			internal BlockCreatedEventArgs(long smallPoolInUse)
			{
				this.SmallPoolInUse = smallPoolInUse;
			}
		}

		/// <summary>
		/// Arguments for the <see cref="LargeBufferCreated"/> events.
		/// </summary>
		public sealed class LargeBufferCreatedEventArgs : EventArgs
		{
			/// <summary>
			/// Unique ID for the stream.
			/// </summary>
			public Guid Id { get; }

			/// <summary>
			/// Optional Tag for the event.
			/// </summary>
			public string? Tag { get; }

			/// <summary>
			/// Whether the buffer was satisfied from the pool or not.
			/// </summary>
			public bool Pooled { get; }

			/// <summary>
			/// Required buffer size.
			/// </summary>
			public long RequiredSize { get; }

			/// <summary>
			/// How many bytes are in use from the large pool.
			/// </summary>
			public long LargePoolInUse { get; }

			/// <summary>
			/// If the buffer was not satisfied from the pool, and <see cref="Options.GenerateCallStacks"/> is turned on, then.
			/// this will contain the call stack of the allocation request.
			/// </summary>
			public string? CallStack { get; }

			/// <summary>
			/// Initializes a new instance of the <see cref="LargeBufferCreatedEventArgs"/> class.
			/// </summary>
			/// <param name="guid">Unique ID of the stream.</param>
			/// <param name="tag">Tag of the stream.</param>
			/// <param name="requiredSize">Required size of the new buffer.</param>
			/// <param name="largePoolInUse">How many bytes from the large pool are currently in use.</param>
			/// <param name="pooled">Whether the buffer was satisfied from the pool or not.</param>
			/// <param name="callStack">Call stack of the allocation, if it wasn't pooled.</param>
			internal LargeBufferCreatedEventArgs(Guid guid, string? tag, long requiredSize, long largePoolInUse, bool pooled, string? callStack)
			{
				this.RequiredSize = requiredSize;
				this.LargePoolInUse = largePoolInUse;
				this.Pooled = pooled;
				this.Id = guid;
				this.Tag = tag;
				this.CallStack = callStack;
			}
		}

		/// <summary>
		/// Arguments for the <see cref="BufferDiscarded"/> event.
		/// </summary>
		public sealed class BufferDiscardedEventArgs : EventArgs
		{
			/// <summary>
			/// Unique ID for the stream.
			/// </summary>
			public Guid Id { get; }

			/// <summary>
			/// Optional Tag for the event.
			/// </summary>
			public string? Tag { get; }

			/// <summary>
			/// Type of the buffer.
			/// </summary>
			public Events.MemoryStreamBufferType BufferType { get; }

			/// <summary>
			/// The reason this buffer was discarded.
			/// </summary>
			public Events.MemoryStreamDiscardReason Reason { get; }

			/// <summary>
			/// Initializes a new instance of the <see cref="BufferDiscardedEventArgs"/> class.
			/// </summary>
			/// <param name="guid">Unique ID of the stream.</param>
			/// <param name="tag">Tag of the stream.</param>
			/// <param name="bufferType">Type of buffer being discarded.</param>
			/// <param name="reason">The reason for the discard.</param>
			internal BufferDiscardedEventArgs(Guid guid, string? tag, Events.MemoryStreamBufferType bufferType, Events.MemoryStreamDiscardReason reason)
			{
				this.Id = guid;
				this.Tag = tag;
				this.BufferType = bufferType;
				this.Reason = reason;
			}
		}

		/// <summary>
		/// Arguments for the <see cref="StreamLength"/> event.
		/// </summary>
		/// <remarks>
		/// Initializes a new instance of the <see cref="StreamLengthEventArgs"/> class.
		/// </remarks>
		/// <param name="length">Length of the strength.</param>
		public sealed class StreamLengthEventArgs(long length) : EventArgs
		{
			/// <summary>
			/// Length of the stream.
			/// </summary>
			public long Length { get; } = length;
		}

		/// <summary>
		/// Arguments for the <see cref="UsageReport"/> event.
		/// </summary>
		/// <remarks>
		/// Initializes a new instance of the <see cref="UsageReportEventArgs"/> class.
		/// </remarks>
		/// <param name="smallPoolInUseBytes">Bytes from the small pool currently in use.</param>
		/// <param name="smallPoolFreeBytes">Bytes from the small pool currently available.</param>
		/// <param name="largePoolInUseBytes">Bytes from the large pool currently in use.</param>
		/// <param name="largePoolFreeBytes">Bytes from the large pool currently available.</param>
		public sealed class UsageReportEventArgs(
			long smallPoolInUseBytes,
			long smallPoolFreeBytes,
			long largePoolInUseBytes,
			long largePoolFreeBytes) : EventArgs
		{
			/// <summary>
			/// Bytes from the small pool currently in use.
			/// </summary>
			public long SmallPoolInUseBytes { get; } = smallPoolInUseBytes;

			/// <summary>
			/// Bytes from the small pool currently available.
			/// </summary>
			public long SmallPoolFreeBytes { get; } = smallPoolFreeBytes;

			/// <summary>
			/// Bytes from the large pool currently in use.
			/// </summary>
			public long LargePoolInUseBytes { get; } = largePoolInUseBytes;

			/// <summary>
			/// Bytes from the large pool currently available.
			/// </summary>
			public long LargePoolFreeBytes { get; } = largePoolFreeBytes;
		}
	}
}
