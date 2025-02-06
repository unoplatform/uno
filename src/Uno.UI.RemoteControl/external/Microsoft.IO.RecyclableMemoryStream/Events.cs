// Imported from https://github.com/microsoft/Microsoft.IO.RecyclableMemoryStream/commit/7df2ef7c95979774f783d4870cb959f03c38aade

#pragma warning disable CA2211 // Disable to keep original file
#pragma warning disable IDE0051 // Disable to keep original file

// ---------------------------------------------------------------------
// Copyright (c) 2015 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------

namespace Microsoft.IO
{
	using System;
	using System.Diagnostics.Tracing;

	public sealed partial class RecyclableMemoryStreamManager
	{
		/// <summary>
		/// ETW events for RecyclableMemoryStream.
		/// </summary>
		[EventSource(Name = "Microsoft-IO-RecyclableMemoryStream", Guid = "{B80CD4E4-890E-468D-9CBA-90EB7C82DFC7}")]
		public sealed class Events : EventSource
		{
			/// <summary>
			/// Static log object, through which all events are written.
			/// </summary>
			public static Events Writer = new();

			/// <summary>
			/// Type of buffer.
			/// </summary>
			public enum MemoryStreamBufferType
			{
				/// <summary>
				/// Small block buffer.
				/// </summary>
				Small,
				/// <summary>
				/// Large pool buffer.
				/// </summary>
				Large
			}

			/// <summary>
			/// The possible reasons for discarding a buffer.
			/// </summary>
			public enum MemoryStreamDiscardReason
			{
				/// <summary>
				/// Buffer was too large to be re-pooled.
				/// </summary>
				TooLarge,
				/// <summary>
				/// There are enough free bytes in the pool.
				/// </summary>
				EnoughFree
			}

			/// <summary>
			/// Logged when a stream object is created.
			/// </summary>
			/// <param name="guid">A unique ID for this stream.</param>
			/// <param name="tag">A temporary ID for this stream, usually indicates current usage.</param>
			/// <param name="requestedSize">Requested size of the stream.</param>
			/// <param name="actualSize">Actual size given to the stream from the pool.</param>
			[Event(1, Level = EventLevel.Verbose, Version = 2)]
			public void MemoryStreamCreated(Guid guid, string? tag, long requestedSize, long actualSize)
			{
				if (this.IsEnabled(EventLevel.Verbose, EventKeywords.None))
				{
					this.WriteEvent(1, guid, tag ?? string.Empty, requestedSize, actualSize);
				}
			}

			/// <summary>
			/// Logged when the stream is disposed.
			/// </summary>
			/// <param name="guid">A unique ID for this stream.</param>
			/// <param name="tag">A temporary ID for this stream, usually indicates current usage.</param>
			/// <param name="lifetimeMs">Lifetime in milliseconds of the stream</param>
			/// <param name="allocationStack">Call stack of initial allocation.</param>
			/// <param name="disposeStack">Call stack of the dispose.</param>
			[Event(2, Level = EventLevel.Verbose, Version = 3)]
			public void MemoryStreamDisposed(Guid guid, string? tag, long lifetimeMs, string? allocationStack, string? disposeStack)
			{
				if (this.IsEnabled(EventLevel.Verbose, EventKeywords.None))
				{
					this.WriteEvent(2, guid, tag ?? string.Empty, lifetimeMs, allocationStack ?? string.Empty, disposeStack ?? string.Empty);
				}
			}

			/// <summary>
			/// Logged when the stream is disposed for the second time.
			/// </summary>
			/// <param name="guid">A unique ID for this stream.</param>
			/// <param name="tag">A temporary ID for this stream, usually indicates current usage.</param>
			/// <param name="allocationStack">Call stack of initial allocation.</param>
			/// <param name="disposeStack1">Call stack of the first dispose.</param>
			/// <param name="disposeStack2">Call stack of the second dispose.</param>
			/// <remarks>Note: Stacks will only be populated if RecyclableMemoryStreamManager.GenerateCallStacks is true.</remarks>
			[Event(3, Level = EventLevel.Critical)]
			public void MemoryStreamDoubleDispose(Guid guid, string? tag, string? allocationStack, string? disposeStack1,
												  string? disposeStack2)
			{
				if (this.IsEnabled())
				{
					this.WriteEvent(3, guid, tag ?? string.Empty, allocationStack ?? string.Empty,
									disposeStack1 ?? string.Empty, disposeStack2 ?? string.Empty);
				}
			}

			/// <summary>
			/// Logged when a stream is finalized.
			/// </summary>
			/// <param name="guid">A unique ID for this stream.</param>
			/// <param name="tag">A temporary ID for this stream, usually indicates current usage.</param>
			/// <param name="allocationStack">Call stack of initial allocation.</param>
			/// <remarks>Note: Stacks will only be populated if RecyclableMemoryStreamManager.GenerateCallStacks is true.</remarks>
			[Event(4, Level = EventLevel.Error)]
			public void MemoryStreamFinalized(Guid guid, string? tag, string? allocationStack)
			{
				if (this.IsEnabled())
				{
					this.WriteEvent(4, guid, tag ?? string.Empty, allocationStack ?? string.Empty);
				}
			}

			/// <summary>
			/// Logged when ToArray is called on a stream.
			/// </summary>
			/// <param name="guid">A unique ID for this stream.</param>
			/// <param name="tag">A temporary ID for this stream, usually indicates current usage.</param>
			/// <param name="stack">Call stack of the ToArray call.</param>
			/// <param name="size">Length of stream.</param>
			/// <remarks>Note: Stacks will only be populated if RecyclableMemoryStreamManager.GenerateCallStacks is true.</remarks>
			[Event(5, Level = EventLevel.Verbose, Version = 2)]
			public void MemoryStreamToArray(Guid guid, string? tag, string? stack, long size)
			{
				if (this.IsEnabled(EventLevel.Verbose, EventKeywords.None))
				{
					this.WriteEvent(5, guid, tag ?? string.Empty, stack ?? string.Empty, size);
				}
			}

			/// <summary>
			/// Logged when the RecyclableMemoryStreamManager is initialized.
			/// </summary>
			/// <param name="blockSize">Size of blocks, in bytes.</param>
			/// <param name="largeBufferMultiple">Size of the large buffer multiple, in bytes.</param>
			/// <param name="maximumBufferSize">Maximum buffer size, in bytes.</param>
			[Event(6, Level = EventLevel.Informational)]
			public void MemoryStreamManagerInitialized(int blockSize, int largeBufferMultiple, int maximumBufferSize)
			{
				if (this.IsEnabled())
				{
					this.WriteEvent(6, blockSize, largeBufferMultiple, maximumBufferSize);
				}
			}

			/// <summary>
			/// Logged when a new block is created.
			/// </summary>
			/// <param name="smallPoolInUseBytes">Number of bytes in the small pool currently in use.</param>
			[Event(7, Level = EventLevel.Warning, Version = 2)]
			public void MemoryStreamNewBlockCreated(long smallPoolInUseBytes)
			{
				if (this.IsEnabled(EventLevel.Warning, EventKeywords.None))
				{
					this.WriteEvent(7, smallPoolInUseBytes);
				}
			}

			/// <summary>
			/// Logged when a new large buffer is created.
			/// </summary>
			/// <param name="requiredSize">Requested size.</param>
			/// <param name="largePoolInUseBytes">Number of bytes in the large pool in use.</param>
			[Event(8, Level = EventLevel.Warning, Version = 3)]
			public void MemoryStreamNewLargeBufferCreated(long requiredSize, long largePoolInUseBytes)
			{
				if (this.IsEnabled(EventLevel.Warning, EventKeywords.None))
				{
					this.WriteEvent(8, requiredSize, largePoolInUseBytes);
				}
			}

			/// <summary>
			/// Logged when a buffer is created that is too large to pool.
			/// </summary>
			/// <param name="guid">Unique stream ID.</param>
			/// <param name="tag">A temporary ID for this stream, usually indicates current usage.</param>
			/// <param name="requiredSize">Size requested by the caller.</param>
			/// <param name="allocationStack">Call stack of the requested stream.</param>
			/// <remarks>Note: Stacks will only be populated if RecyclableMemoryStreamManager.GenerateCallStacks is true.</remarks>
			[Event(9, Level = EventLevel.Verbose, Version = 3)]
			public void MemoryStreamNonPooledLargeBufferCreated(Guid guid, string? tag, long requiredSize, string? allocationStack)
			{
				if (this.IsEnabled(EventLevel.Verbose, EventKeywords.None))
				{
					this.WriteEvent(9, guid, tag ?? string.Empty, requiredSize, allocationStack ?? string.Empty);
				}
			}

			/// <summary>
			/// Logged when a buffer is discarded (not put back in the pool, but given to GC to clean up).
			/// </summary>
			/// <param name="guid">Unique stream ID.</param>
			/// <param name="tag">A temporary ID for this stream, usually indicates current usage.</param>
			/// <param name="bufferType">Type of the buffer being discarded.</param>
			/// <param name="reason">Reason for the discard.</param>
			/// <param name="smallBlocksFree">Number of free small pool blocks.</param>
			/// <param name="smallPoolBytesFree">Bytes free in the small pool.</param>
			/// <param name="smallPoolBytesInUse">Bytes in use from the small pool.</param>
			/// <param name="largeBlocksFree">Number of free large pool blocks.</param>
			/// <param name="largePoolBytesFree">Bytes free in the large pool.</param>
			/// <param name="largePoolBytesInUse">Bytes in use from the large pool.</param>
			[Event(10, Level = EventLevel.Warning, Version = 2)]
			public void MemoryStreamDiscardBuffer(Guid guid, string? tag, MemoryStreamBufferType bufferType,
												  MemoryStreamDiscardReason reason, long smallBlocksFree, long smallPoolBytesFree, long smallPoolBytesInUse, long largeBlocksFree, long largePoolBytesFree, long largePoolBytesInUse)
			{
				if (this.IsEnabled(EventLevel.Warning, EventKeywords.None))
				{
					this.WriteEvent(10, guid, tag ?? string.Empty, bufferType, reason, smallBlocksFree, smallPoolBytesFree, smallPoolBytesInUse, largeBlocksFree, largePoolBytesFree, largePoolBytesInUse);
				}
			}

			/// <summary>
			/// Logged when a stream grows beyond the maximum capacity.
			/// </summary>
			/// <param name="guid">Unique stream ID</param>
			/// <param name="requestedCapacity">The requested capacity.</param>
			/// <param name="maxCapacity">Maximum capacity, as configured by RecyclableMemoryStreamManager.</param>
			/// <param name="tag">A temporary ID for this stream, usually indicates current usage.</param>
			/// <param name="allocationStack">Call stack for the capacity request.</param>
			/// <remarks>Note: Stacks will only be populated if RecyclableMemoryStreamManager.GenerateCallStacks is true.</remarks>
			[Event(11, Level = EventLevel.Error, Version = 3)]
			public void MemoryStreamOverCapacity(Guid guid, string? tag, long requestedCapacity, long maxCapacity, string? allocationStack)
			{
				if (this.IsEnabled())
				{
					this.WriteEvent(11, guid, tag ?? string.Empty, requestedCapacity, maxCapacity, allocationStack ?? string.Empty);
				}
			}
		}
	}
}
