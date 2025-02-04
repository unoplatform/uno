// Imported from https://github.com/microsoft/Microsoft.IO.RecyclableMemoryStream/commit/7df2ef7c95979774f783d4870cb959f03c38aade

// The MIT License (MIT)
//
// Copyright (c) 2015-2016 Microsoft
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace Microsoft.IO
{
	using System;
	using System.Buffers;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using System.Threading.Tasks;

	/// <summary>
	/// MemoryStream implementation that deals with pooling and managing memory streams which use potentially large
	/// buffers.
	/// </summary>
	/// <remarks>
	/// This class works in tandem with the <see cref="RecyclableMemoryStreamManager"/> to supply <c>MemoryStream</c>-derived
	/// objects to callers, while avoiding these specific problems:
	/// <list type="number">
	/// <item>
	/// <term>LOH allocations</term>
	/// <description>Since all large buffers are pooled, they will never incur a Gen2 GC</description>
	/// </item>
	/// <item>
	/// <term>Memory waste</term><description>A standard memory stream doubles its size when it runs out of room. This
	/// leads to continual memory growth as each stream approaches the maximum allowed size.</description>
	/// </item>
	/// <item>
	/// <term>Memory copying</term>
	/// <description>Each time a <c>MemoryStream</c> grows, all the bytes are copied into new buffers.
	/// This implementation only copies the bytes when <see cref="GetBuffer"/> is called.</description>
	/// </item>
	/// <item>
	/// <term>Memory fragmentation</term>
	/// <description>By using homogeneous buffer sizes, it ensures that blocks of memory
	/// can be easily reused.
	/// </description>
	/// </item>
	/// </list>
	/// <para>
	/// The stream is implemented on top of a series of uniformly-sized blocks. As the stream's length grows,
	/// additional blocks are retrieved from the memory manager. It is these blocks that are pooled, not the stream
	/// object itself.
	/// </para>
	/// <para>
	/// The biggest wrinkle in this implementation is when <see cref="GetBuffer"/> is called. This requires a single
	/// contiguous buffer. If only a single block is in use, then that block is returned. If multiple blocks
	/// are in use, we retrieve a larger buffer from the memory manager. These large buffers are also pooled,
	/// split by size--they are multiples/exponentials of a chunk size (1 MB by default).
	/// </para>
	/// <para>
	/// Once a large buffer is assigned to the stream the small blocks are NEVER again used for this stream. All operations take place on the
	/// large buffer. The large buffer can be replaced by a larger buffer from the pool as needed. All blocks and large buffers
	/// are maintained in the stream until the stream is disposed (unless AggressiveBufferReturn is enabled in the stream manager).
	/// </para>
	/// <para>
	/// A further wrinkle is what happens when the stream is longer than the maximum allowable array length under .NET. This is allowed
	/// when only blocks are in use, and only the Read/Write APIs are used. Once a stream grows to this size, any attempt to convert it
	/// to a single buffer will result in an exception. Similarly, if a stream is already converted to use a single larger buffer, then
	/// it cannot grow beyond the limits of the maximum allowable array size.
	/// </para>
	/// <para>
	/// Any method that modifies the stream has the potential to throw an <c>OutOfMemoryException</c>, either because
	/// the stream is beyond the limits set in <c>RecyclableStreamManager</c>, or it would result in a buffer larger than
	/// the maximum array size supported by .NET.
	/// </para>
	/// </remarks>
	public sealed class RecyclableMemoryStream : MemoryStream, IBufferWriter<byte>
	{
		/// <summary>
		/// All of these blocks must be the same size.
		/// </summary>
		private readonly List<byte[]> blocks;

		private readonly Guid id;

		private readonly RecyclableMemoryStreamManager memoryManager;

		private readonly string? tag;

		private readonly long creationTimestamp;

		/// <summary>
		/// This list is used to store buffers once they're replaced by something larger.
		/// This is for the cases where you have users of this class that may hold onto the buffers longer
		/// than they should and you want to prevent race conditions which could corrupt the data.
		/// </summary>
		private List<byte[]>? dirtyBuffers;

		private bool disposed;

		/// <summary>
		/// This is only set by GetBuffer() if the necessary buffer is larger than a single block size, or on
		/// construction if the caller immediately requests a single large buffer.
		/// </summary>
		/// <remarks>If this field is non-null, it contains the concatenation of the bytes found in the individual
		/// blocks. Once it is created, this (or a larger) largeBuffer will be used for the life of the stream.
		/// </remarks>
		private byte[]? largeBuffer;

		/// <summary>
		/// Unique identifier for this stream across its entire lifetime.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		internal Guid Id
		{
			get
			{
				this.CheckDisposed();
				return this.id;
			}
		}

		/// <summary>
		/// A temporary identifier for the current usage of this stream.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		internal string? Tag
		{
			get
			{
				this.CheckDisposed();
				return this.tag;
			}
		}

		/// <summary>
		/// Gets the memory manager being used by this stream.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		internal RecyclableMemoryStreamManager MemoryManager
		{
			get
			{
				this.CheckDisposed();
				return this.memoryManager;
			}
		}

		/// <summary>
		/// Call stack of the constructor. It is only set if <see cref="RecyclableMemoryStreamManager.Options.GenerateCallStacks"/> is true,
		/// which should only be in debugging situations.
		/// </summary>
		internal string? AllocationStack { get; }

		/// <summary>
		/// Call stack of the <see cref="Dispose(bool)"/> call. It is only set if <see cref="RecyclableMemoryStreamManager.Options.GenerateCallStacks"/> is true,
		/// which should only be in debugging situations.
		/// </summary>
		internal string? DisposeStack { get; private set; }

		#region Constructors
		/// <summary>
		/// Initializes a new instance of the <see cref="RecyclableMemoryStream"/> class.
		/// </summary>
		/// <param name="memoryManager">The memory manager.</param>
		public RecyclableMemoryStream(RecyclableMemoryStreamManager memoryManager)
			: this(memoryManager, Guid.NewGuid(), null, 0, null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RecyclableMemoryStream"/> class.
		/// </summary>
		/// <param name="memoryManager">The memory manager.</param>
		/// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
		public RecyclableMemoryStream(RecyclableMemoryStreamManager memoryManager, Guid id)
			: this(memoryManager, id, null, 0, null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RecyclableMemoryStream"/> class.
		/// </summary>
		/// <param name="memoryManager">The memory manager.</param>
		/// <param name="tag">A string identifying this stream for logging and debugging purposes.</param>
		public RecyclableMemoryStream(RecyclableMemoryStreamManager memoryManager, string? tag)
			: this(memoryManager, Guid.NewGuid(), tag, 0, null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RecyclableMemoryStream"/> class.
		/// </summary>
		/// <param name="memoryManager">The memory manager.</param>
		/// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
		/// <param name="tag">A string identifying this stream for logging and debugging purposes.</param>
		public RecyclableMemoryStream(RecyclableMemoryStreamManager memoryManager, Guid id, string? tag)
			: this(memoryManager, id, tag, 0, null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RecyclableMemoryStream"/> class.
		/// </summary>
		/// <param name="memoryManager">The memory manager.</param>
		/// <param name="tag">A string identifying this stream for logging and debugging purposes.</param>
		/// <param name="requestedSize">The initial requested size to prevent future allocations.</param>
		public RecyclableMemoryStream(RecyclableMemoryStreamManager memoryManager, string? tag, long requestedSize)
			: this(memoryManager, Guid.NewGuid(), tag, requestedSize, null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RecyclableMemoryStream"/> class.
		/// </summary>
		/// <param name="memoryManager">The memory manager</param>
		/// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
		/// <param name="tag">A string identifying this stream for logging and debugging purposes.</param>
		/// <param name="requestedSize">The initial requested size to prevent future allocations.</param>
		public RecyclableMemoryStream(RecyclableMemoryStreamManager memoryManager, Guid id, string? tag, long requestedSize)
			: this(memoryManager, id, tag, requestedSize, null) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="RecyclableMemoryStream"/> class.
		/// </summary>
		/// <param name="memoryManager">The memory manager.</param>
		/// <param name="id">A unique identifier which can be used to trace usages of the stream.</param>
		/// <param name="tag">A string identifying this stream for logging and debugging purposes.</param>
		/// <param name="requestedSize">The initial requested size to prevent future allocations.</param>
		/// <param name="initialLargeBuffer">An initial buffer to use. This buffer will be owned by the stream and returned to the memory manager upon Dispose.</param>
		internal RecyclableMemoryStream(RecyclableMemoryStreamManager memoryManager, Guid id, string? tag, long requestedSize, byte[]? initialLargeBuffer)
			: base([])
		{
			this.memoryManager = memoryManager;
			this.id = id;
			this.tag = tag;
			this.blocks = [];
			this.creationTimestamp = Stopwatch.GetTimestamp();

			var actualRequestedSize = Math.Max(requestedSize, this.memoryManager.options.BlockSize);

			if (initialLargeBuffer == null)
			{
				this.EnsureCapacity(actualRequestedSize);
			}
			else
			{
				this.largeBuffer = initialLargeBuffer;
			}

			if (this.memoryManager.options.GenerateCallStacks)
			{
				this.AllocationStack = Environment.StackTrace;
			}

			this.memoryManager.ReportStreamCreated(this.id, this.tag, requestedSize, actualRequestedSize);
			this.memoryManager.ReportUsageReport();
		}
		#endregion

		#region Dispose and Finalize
		/// <summary>
		/// The finalizer will be called when a stream is not disposed properly.
		/// </summary>
		/// <remarks>Failing to dispose indicates a bug in the code using streams. Care should be taken to properly account for stream lifetime.</remarks>
		~RecyclableMemoryStream()
		{
			this.Dispose(false);
		}

		/// <summary>
		/// Returns the memory used by this stream back to the pool.
		/// </summary>
		/// <param name="disposing">Whether we're disposing (true), or being called by the finalizer (false).</param>
		protected override void Dispose(bool disposing)
		{
			if (this.disposed)
			{
				string? doubleDisposeStack = null;
				if (this.memoryManager.options.GenerateCallStacks)
				{
					doubleDisposeStack = Environment.StackTrace;
				}

				this.memoryManager.ReportStreamDoubleDisposed(this.id, this.tag, this.AllocationStack, this.DisposeStack, doubleDisposeStack);
				return;
			}

			this.disposed = true;
			var lifetime = TimeSpan.FromTicks((Stopwatch.GetTimestamp() - this.creationTimestamp) * TimeSpan.TicksPerSecond / Stopwatch.Frequency);

			if (this.memoryManager.options.GenerateCallStacks)
			{
				this.DisposeStack = Environment.StackTrace;
			}

			this.memoryManager.ReportStreamDisposed(this.id, this.tag, lifetime, this.AllocationStack, this.DisposeStack);

			if (disposing)
			{
				GC.SuppressFinalize(this);
			}
			else
			{
				// We're being finalized.
				this.memoryManager.ReportStreamFinalized(this.id, this.tag, this.AllocationStack);

				if (AppDomain.CurrentDomain.IsFinalizingForUnload())
				{
					// If we're being finalized because of a shutdown, don't go any further.
					// We have no idea what's already been cleaned up. Triggering events may cause
					// a crash.
					base.Dispose(disposing);
					return;
				}
			}

			this.memoryManager.ReportStreamLength(this.length);

			if (this.largeBuffer != null)
			{
				this.memoryManager.ReturnLargeBuffer(this.largeBuffer, this.id, this.tag);
			}

			if (this.dirtyBuffers != null)
			{
				foreach (var buffer in this.dirtyBuffers)
				{
					this.memoryManager.ReturnLargeBuffer(buffer, this.id, this.tag);
				}
			}

			this.memoryManager.ReturnBlocks(this.blocks, this.id, this.tag);
			this.memoryManager.ReportUsageReport();
			this.blocks.Clear();

			base.Dispose(disposing);
		}

		/// <summary>
		/// Equivalent to <c>Dispose</c>.
		/// </summary>
		public override void Close()
		{
			this.Dispose(true);
		}
		#endregion

		#region MemoryStream overrides
		/// <summary>
		/// Gets or sets the capacity.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Capacity is always in multiples of the memory manager's block size, unless
		/// the large buffer is in use. Capacity never decreases during a stream's lifetime.
		/// Explicitly setting the capacity to a lower value than the current value will have no effect.
		/// This is because the buffers are all pooled by chunks and there's little reason to
		/// allow stream truncation.
		/// </para>
		/// <para>
		/// Writing past the current capacity will cause <see cref="Capacity"/> to automatically increase, until MaximumStreamCapacity is reached.
		/// </para>
		/// <para>
		/// If the capacity is larger than <c>int.MaxValue</c>, then <c>InvalidOperationException</c> will be thrown. If you anticipate using
		/// larger streams, use the <see cref="Capacity64"/> property instead.
		/// </para>
		/// </remarks>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		/// <exception cref="InvalidOperationException">Capacity is larger than int.MaxValue.</exception>
		public override int Capacity
		{
			get
			{
				this.CheckDisposed();
				if (this.largeBuffer != null)
				{
					return this.largeBuffer.Length;
				}

				long size = (long)this.blocks.Count * this.memoryManager.options.BlockSize;
				if (size > int.MaxValue)
				{
					throw new InvalidOperationException($"{nameof(this.Capacity)} is larger than int.MaxValue. Use {nameof(this.Capacity64)} instead.");
				}
				return (int)size;
			}
			set
			{
				this.Capacity64 = value;
			}
		}

		/// <summary>
		/// Returns a 64-bit version of capacity, for streams larger than <c>int.MaxValue</c> in length.
		/// </summary>
		public long Capacity64
		{
			get
			{
				this.CheckDisposed();
				if (this.largeBuffer != null)
				{
					return this.largeBuffer.Length;
				}

				long size = (long)this.blocks.Count * this.memoryManager.options.BlockSize;
				return size;
			}
			set
			{
				this.CheckDisposed();
				this.EnsureCapacity(value);
			}
		}

		private long length;

		/// <summary>
		/// Gets the number of bytes written to this stream.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		/// <remarks>If the buffer has already been converted to a large buffer, then the maximum length is limited by the maximum allowed array length in .NET.</remarks>
		public override long Length
		{
			get
			{
				this.CheckDisposed();
				return this.length;
			}
		}

		private long position;

		/// <summary>
		/// Gets the current position in the stream.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		/// <exception cref="ArgumentOutOfRangeException">A negative value was passed.</exception>
		/// <exception cref="InvalidOperationException">Stream is in large-buffer mode, but an attempt was made to set the position past the maximum allowed array length.</exception>
		/// <remarks>If the buffer has already been converted to a large buffer, then the maximum length (and thus position) is limited by the maximum allowed array length in .NET.</remarks>
		public override long Position
		{
			get
			{
				this.CheckDisposed();
				return this.position;
			}
			set
			{
				this.CheckDisposed();
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(value)} must be non-negative.");
				}

				if (this.largeBuffer != null && value > RecyclableMemoryStreamManager.MaxArrayLength)
				{
					throw new InvalidOperationException($"Once the stream is converted to a single large buffer, position cannot be set past {RecyclableMemoryStreamManager.MaxArrayLength}.");
				}
				this.position = value;
			}
		}

		/// <summary>
		/// Whether the stream can currently read.
		/// </summary>
		public override bool CanRead => !this.Disposed;

		/// <summary>
		/// Whether the stream can currently seek.
		/// </summary>
		public override bool CanSeek => !this.Disposed;

		/// <summary>
		/// Always false.
		/// </summary>
		public override bool CanTimeout => false;

		/// <summary>
		/// Whether the stream can currently write.
		/// </summary>
		public override bool CanWrite => !this.Disposed;

		/// <summary>
		/// Returns a single buffer containing the contents of the stream.
		/// The buffer may be longer than the stream length.
		/// </summary>
		/// <returns>A byte[] buffer.</returns>
		/// <remarks>IMPORTANT: Doing a <see cref="Write(byte[], int, int)"/> after calling <c>GetBuffer</c> invalidates the buffer. The old buffer is held onto
		/// until <see cref="Dispose(bool)"/> is called, but the next time <c>GetBuffer</c> is called, a new buffer from the pool will be required.</remarks>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		/// <exception cref="OutOfMemoryException">stream is too large for a contiguous buffer.</exception>
		public override byte[] GetBuffer()
		{
			this.CheckDisposed();

			if (this.largeBuffer != null)
			{
				return this.largeBuffer;
			}

			if (this.blocks.Count == 1)
			{
				return this.blocks[0];
			}

			// Buffer needs to reflect the capacity, not the length, because
			// it's possible that people will manipulate the buffer directly
			// and set the length afterward. Capacity sets the expectation
			// for the size of the buffer.

			var newBuffer = this.memoryManager.GetLargeBuffer(this.Capacity64, this.id, this.tag);

			// InternalRead will check for existence of largeBuffer, so make sure we
			// don't set it until after we've copied the data.
			this.AssertLengthIsSmall();
			this.InternalRead(newBuffer, 0, (int)this.length, 0);
			this.largeBuffer = newBuffer;

			if (this.blocks.Count > 0 && this.memoryManager.options.AggressiveBufferReturn)
			{
				this.memoryManager.ReturnBlocks(this.blocks, this.id, this.tag);
				this.blocks.Clear();
			}

			return this.largeBuffer;
		}

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
		/// <inheritdoc/>
		public override void CopyTo(Stream destination, int bufferSize)
		{
			this.WriteTo(destination, this.position, this.length - this.position);
		}
#endif

		/// <summary>Asynchronously reads all the bytes from the current position in this stream and writes them to another stream.</summary>
		/// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
		/// <param name="bufferSize">This parameter is ignored.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous copy operation.</returns>
		/// <exception cref="T:System.ArgumentNullException">
		///   <paramref name="destination"/> is <see langword="null"/>.</exception>
		/// <exception cref="T:System.ObjectDisposedException">Either the current stream or the destination stream is disposed.</exception>
		/// <exception cref="T:System.NotSupportedException">The current stream does not support reading, or the destination stream does not support writing.</exception>
		/// <remarks>Similarly to <c>MemoryStream</c>'s behavior, <c>CopyToAsync</c> will adjust the source stream's position by the number of bytes written to the destination stream, as a Read would do.</remarks>
		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			this.CheckDisposed();

			if (this.length == 0)
			{
				return Task.CompletedTask;
			}

			long startPos = this.position;
			var count = this.length - startPos;
			this.position += count;

			if (destination is MemoryStream destinationRMS)
			{
				this.WriteTo(destinationRMS, startPos, count);
				return Task.CompletedTask;
			}
			else
			{
				if (this.largeBuffer == null)
				{
					if (this.blocks.Count == 1)
					{
						this.AssertLengthIsSmall();
						return destination.WriteAsync(this.blocks[0], (int)startPos, (int)count, cancellationToken);
					}
					else
					{
						return CopyToAsyncImpl(destination, this.GetBlockAndRelativeOffset(startPos), count, this.blocks, cancellationToken);
					}
				}
				else
				{
					this.AssertLengthIsSmall();
					return destination.WriteAsync(this.largeBuffer, (int)startPos, (int)count, cancellationToken);
				}
			}

			static async Task CopyToAsyncImpl(Stream destination, BlockAndOffset blockAndOffset, long count, List<byte[]> blocks, CancellationToken cancellationToken)
			{
				var bytesRemaining = count;
				int currentBlock = blockAndOffset.Block;
				var currentOffset = blockAndOffset.Offset;
				while (bytesRemaining > 0)
				{
					byte[] block = blocks[currentBlock];
					int amountToCopy = (int)Math.Min(block.Length - currentOffset, bytesRemaining);
					await destination.WriteAsync(block, currentOffset, amountToCopy, cancellationToken);
					bytesRemaining -= amountToCopy;
					++currentBlock;
					currentOffset = 0;
				}
			}
		}

		private byte[]? bufferWriterTempBuffer;

		/// <summary>
		/// Notifies the stream that <paramref name="count"/> bytes were written to the buffer returned by <see cref="GetMemory(int)"/> or <see cref="GetSpan(int)"/>.
		/// Seeks forward by <paramref name="count"/> bytes.
		/// </summary>
		/// <remarks>
		/// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
		/// </remarks>
		/// <param name="count">How many bytes to advance.</param>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
		/// <exception cref="InvalidOperationException"><paramref name="count"/> is larger than the size of the previously requested buffer.</exception>
		public void Advance(int count)
		{
			this.CheckDisposed();
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count), $"{nameof(count)} must be non-negative.");
			}

			byte[]? buffer = this.bufferWriterTempBuffer;
			if (buffer != null)
			{
				if (count > buffer.Length)
				{
					throw new InvalidOperationException($"Cannot advance past the end of the buffer, which has a size of {buffer.Length}.");
				}

				this.Write(buffer, 0, count);
				this.ReturnTempBuffer(buffer);
				this.bufferWriterTempBuffer = null;
			}
			else
			{
				long bufferSize = this.largeBuffer == null
					? this.memoryManager.options.BlockSize - this.GetBlockAndRelativeOffset(this.position).Offset
					: this.largeBuffer.Length - this.position;

				if (count > bufferSize)
				{
					throw new InvalidOperationException($"Cannot advance past the end of the buffer, which has a size of {bufferSize}.");
				}

				this.position += count;
				this.length = Math.Max(this.position, this.length);
			}
		}

		private void ReturnTempBuffer(byte[] buffer)
		{
			if (buffer.Length == this.memoryManager.options.BlockSize)
			{
				this.memoryManager.ReturnBlock(buffer, this.id, this.tag);
			}
			else
			{
				this.memoryManager.ReturnLargeBuffer(buffer, this.id, this.tag);
			}
		}

		/// <inheritdoc/>
		/// <remarks>
		/// IMPORTANT: Calling Write(), GetBuffer(), TryGetBuffer(), Seek(), GetLength(), Advance(),
		/// or setting Position after calling GetMemory() invalidates the memory.
		/// </remarks>
		public Memory<byte> GetMemory(int sizeHint = 0) => this.GetWritableBuffer(sizeHint);

		/// <inheritdoc/>
		/// <remarks>
		/// IMPORTANT: Calling Write(), GetBuffer(), TryGetBuffer(), Seek(), GetLength(), Advance(),
		/// or setting Position after calling GetSpan() invalidates the span.
		/// </remarks>
		public Span<byte> GetSpan(int sizeHint = 0) => this.GetWritableBuffer(sizeHint);

		/// <summary>
		/// When callers to GetSpan() or GetMemory() request a buffer that is larger than the remaining size of the current block
		/// this method return a temp buffer. When Advance() is called, that temp buffer is then copied into the stream.
		/// </summary>
		private ArraySegment<byte> GetWritableBuffer(int sizeHint)
		{
			this.CheckDisposed();
			if (sizeHint < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(sizeHint), $"{nameof(sizeHint)} must be non-negative.");
			}

			var minimumBufferSize = Math.Max(sizeHint, 1);

			this.EnsureCapacity(this.position + minimumBufferSize);
			if (this.bufferWriterTempBuffer != null)
			{
				this.ReturnTempBuffer(this.bufferWriterTempBuffer);
				this.bufferWriterTempBuffer = null;
			}

			if (this.largeBuffer != null)
			{
				return new ArraySegment<byte>(this.largeBuffer, (int)this.position, this.largeBuffer.Length - (int)this.position);
			}

			BlockAndOffset blockAndOffset = this.GetBlockAndRelativeOffset(this.position);
			int remainingBytesInBlock = this.MemoryManager.options.BlockSize - blockAndOffset.Offset;
			if (remainingBytesInBlock >= minimumBufferSize)
			{
				return new ArraySegment<byte>(this.blocks[blockAndOffset.Block], blockAndOffset.Offset, this.MemoryManager.options.BlockSize - blockAndOffset.Offset);
			}

			this.bufferWriterTempBuffer = minimumBufferSize > this.memoryManager.options.BlockSize ?
				this.memoryManager.GetLargeBuffer(minimumBufferSize, this.id, this.tag) :
				this.memoryManager.GetBlock();

			return new ArraySegment<byte>(this.bufferWriterTempBuffer);
		}

		/// <summary>
		/// Returns a sequence containing the contents of the stream.
		/// </summary>
		/// <returns>A ReadOnlySequence of bytes.</returns>
		/// <remarks>IMPORTANT: Calling Write(), GetMemory(), GetSpan(), Dispose(), or Close() after calling GetReadOnlySequence() invalidates the sequence.</remarks>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public ReadOnlySequence<byte> GetReadOnlySequence()
		{
			this.CheckDisposed();

			if (this.largeBuffer != null)
			{
				this.AssertLengthIsSmall();
				return new ReadOnlySequence<byte>(this.largeBuffer, 0, (int)this.length);
			}

			if (this.blocks.Count == 1)
			{
				this.AssertLengthIsSmall();
				return new ReadOnlySequence<byte>(this.blocks[0], 0, (int)this.length);
			}

			var first = new BlockSegment(this.blocks[0]);
			var last = first;

			for (int blockIdx = 1; last.RunningIndex + last.Memory.Length < this.length; blockIdx++)
			{
				last = last.Append(this.blocks[blockIdx]);
			}

			return new ReadOnlySequence<byte>(first, 0, last, (int)(this.length - last.RunningIndex));
		}

		private sealed class BlockSegment : ReadOnlySequenceSegment<byte>
		{
			public BlockSegment(Memory<byte> memory) => this.Memory = memory;

			public BlockSegment Append(Memory<byte> memory)
			{
				var nextSegment = new BlockSegment(memory) { RunningIndex = this.RunningIndex + this.Memory.Length };
				this.Next = nextSegment;
				return nextSegment;
			}
		}

		/// <summary>
		/// Returns an <c>ArraySegment</c> that wraps a single buffer containing the contents of the stream.
		/// </summary>
		/// <param name="buffer">An <c>ArraySegment</c> containing a reference to the underlying bytes.</param>
		/// <returns>Returns <see langword="true"/> if a buffer can be returned; otherwise, <see langword="false"/>.</returns>
		public override bool TryGetBuffer(out ArraySegment<byte> buffer)
		{
			this.CheckDisposed();

			try
			{
				if (this.length <= RecyclableMemoryStreamManager.MaxArrayLength)
				{
					buffer = new ArraySegment<byte>(this.GetBuffer(), 0, (int)this.Length);
					return true;
				}
			}
			catch (OutOfMemoryException)
			{
			}

#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
			buffer = ArraySegment<byte>.Empty;
#else
            buffer = new ArraySegment<byte>();
#endif
			return false;
		}

		/// <summary>
		/// Returns a new array with a copy of the buffer's contents. You should almost certainly be using <see cref="GetBuffer"/> combined with the <see cref="Length"/> to
		/// access the bytes in this stream. Calling <c>ToArray</c> will destroy the benefits of pooled buffers, but it is included
		/// for the sake of completeness.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		/// <exception cref="NotSupportedException">The current <see cref="RecyclableMemoryStreamManager"/>object disallows <c>ToArray</c> calls.</exception>
		/// <exception cref="OutOfMemoryException">The length of the stream is too long for a contiguous array.</exception>
#pragma warning disable CS0809
		[Obsolete("This method has degraded performance vs. GetBuffer and should be avoided.")]
		public override byte[] ToArray()
		{
			this.CheckDisposed();

			string? stack = this.memoryManager.options.GenerateCallStacks ? Environment.StackTrace : null;
			this.memoryManager.ReportStreamToArray(this.id, this.tag, stack, this.length);

			if (this.memoryManager.options.ThrowExceptionOnToArray)
			{
				throw new NotSupportedException("The underlying RecyclableMemoryStreamManager is configured to not allow calls to ToArray.");
			}

			var newBuffer = new byte[this.Length];

			Debug.Assert(this.length <= int.MaxValue);
			this.InternalRead(newBuffer, 0, (int)this.length, 0);

			return newBuffer;
		}
#pragma warning restore CS0809

		/// <summary>
		/// Reads from the current position into the provided buffer.
		/// </summary>
		/// <param name="buffer">Destination buffer.</param>
		/// <param name="offset">Offset into buffer at which to start placing the read bytes.</param>
		/// <param name="count">Number of bytes to read.</param>
		/// <returns>The number of bytes read.</returns>
		/// <exception cref="ArgumentNullException">buffer is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">offset or count is less than 0.</exception>
		/// <exception cref="ArgumentException">offset subtracted from the buffer length is less than count.</exception>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public override int Read(byte[] buffer, int offset, int count)
		{
			return this.SafeRead(buffer, offset, count, ref this.position);
		}

		/// <summary>
		/// Reads from the specified position into the provided buffer.
		/// </summary>
		/// <param name="buffer">Destination buffer.</param>
		/// <param name="offset">Offset into buffer at which to start placing the read bytes.</param>
		/// <param name="count">Number of bytes to read.</param>
		/// <param name="streamPosition">Position in the stream to start reading from.</param>
		/// <returns>The number of bytes read.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is less than 0.</exception>
		/// <exception cref="ArgumentException"><paramref name="offset"/> subtracted from the buffer length is less than <paramref name="count"/>.</exception>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public int SafeRead(byte[] buffer, int offset, int count, ref long streamPosition)
		{
			this.CheckDisposed();
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), $"{nameof(offset)} cannot be negative.");
			}

			if (count < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count), $"{nameof(count)} cannot be negative.");
			}

			if (offset + count > buffer.Length)
			{
				throw new ArgumentException($"{nameof(buffer)} length must be at least {nameof(offset)} + {nameof(count)}.");
			}

			int amountRead = this.InternalRead(buffer, offset, count, streamPosition);
			streamPosition += amountRead;
			return amountRead;
		}

		/// <summary>
		/// Reads from the current position into the provided buffer.
		/// </summary>
		/// <param name="buffer">Destination buffer.</param>
		/// <returns>The number of bytes read.</returns>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
#if NETSTANDARD2_0
        public int Read(Span<byte> buffer)
#else
		public override int Read(Span<byte> buffer)
#endif
		{
			return this.SafeRead(buffer, ref this.position);
		}

		/// <summary>
		/// Reads from the specified position into the provided buffer.
		/// </summary>
		/// <param name="buffer">Destination buffer.</param>
		/// <param name="streamPosition">Position in the stream to start reading from.</param>
		/// <returns>The number of bytes read.</returns>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public int SafeRead(Span<byte> buffer, ref long streamPosition)
		{
			this.CheckDisposed();

			int amountRead = this.InternalRead(buffer, streamPosition);
			streamPosition += amountRead;
			return amountRead;
		}

		/// <summary>
		/// Writes the buffer to the stream.
		/// </summary>
		/// <param name="buffer">Source buffer.</param>
		/// <param name="offset">Start position.</param>
		/// <param name="count">Number of bytes to write.</param>
		/// <exception cref="ArgumentNullException">buffer is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">offset or count is negative.</exception>
		/// <exception cref="ArgumentException">buffer.Length - offset is not less than count.</exception>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public override void Write(byte[] buffer, int offset, int count)
		{
			this.CheckDisposed();
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), offset,
					$"{nameof(offset)} must be in the range of 0 - {nameof(buffer)}.{nameof(buffer.Length)}-1.");
			}

			if (count < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(count), count, $"{nameof(count)} must be non-negative.");
			}

			if (count + offset > buffer.Length)
			{
				throw new ArgumentException($"{nameof(count)} must be greater than {nameof(buffer)}.{nameof(buffer.Length)} - {nameof(offset)}.");
			}

			int blockSize = this.memoryManager.options.BlockSize;
			long end = this.position + count;

			this.EnsureCapacity(end);

			if (this.largeBuffer == null)
			{
				int bytesRemaining = count;
				int bytesWritten = 0;
				var blockAndOffset = this.GetBlockAndRelativeOffset(this.position);

				while (bytesRemaining > 0)
				{
					byte[] currentBlock = this.blocks[blockAndOffset.Block];
					int remainingInBlock = blockSize - blockAndOffset.Offset;
					int amountToWriteInBlock = Math.Min(remainingInBlock, bytesRemaining);

					Buffer.BlockCopy(buffer, offset + bytesWritten, currentBlock, blockAndOffset.Offset,
									 amountToWriteInBlock);

					bytesRemaining -= amountToWriteInBlock;
					bytesWritten += amountToWriteInBlock;

					++blockAndOffset.Block;
					blockAndOffset.Offset = 0;
				}
			}
			else
			{
				Buffer.BlockCopy(buffer, offset, this.largeBuffer, (int)this.position, count);
			}
			this.position = end;
			this.length = Math.Max(this.position, this.length);
		}

		/// <summary>
		/// Writes the buffer to the stream.
		/// </summary>
		/// <param name="source">Source buffer.</param>
		/// <exception cref="ArgumentNullException">buffer is null.</exception>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
#if NETSTANDARD2_0
        public void Write(ReadOnlySpan<byte> source)
#else
		public override void Write(ReadOnlySpan<byte> source)
#endif

		{
			this.CheckDisposed();

			int blockSize = this.memoryManager.options.BlockSize;
			long end = this.position + source.Length;

			this.EnsureCapacity(end);

			if (this.largeBuffer == null)
			{
				var blockAndOffset = this.GetBlockAndRelativeOffset(this.position);

				while (source.Length > 0)
				{
					byte[] currentBlock = this.blocks[blockAndOffset.Block];
					int remainingInBlock = blockSize - blockAndOffset.Offset;
					int amountToWriteInBlock = Math.Min(remainingInBlock, source.Length);

					source.Slice(0, amountToWriteInBlock)
						.CopyTo(currentBlock.AsSpan(blockAndOffset.Offset));

					source = source.Slice(amountToWriteInBlock);

					++blockAndOffset.Block;
					blockAndOffset.Offset = 0;
				}
			}
			else
			{
				source.CopyTo(this.largeBuffer.AsSpan((int)this.position));
			}
			this.position = end;
			this.length = Math.Max(this.position, this.length);
		}

		/// <summary>
		/// Returns a useful string for debugging. This should not normally be called in actual production code.
		/// </summary>
		public override string ToString()
		{
			if (!this.disposed)
			{
				return $"Id = {this.Id}, Tag = {this.Tag}, Length = {this.Length:N0} bytes";
			}
			else
			{
				// Avoid properties because of the dispose check, but the fields themselves are not cleared.
				return $"Disposed: Id = {this.id}, Tag = {this.tag}, Final Length: {this.length:N0} bytes";
			}
		}

		/// <summary>
		/// Writes a single byte to the current position in the stream.
		/// </summary>
		/// <param name="value">byte value to write.</param>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public override void WriteByte(byte value)
		{
			this.CheckDisposed();

			long end = this.position + 1;

			if (this.largeBuffer == null)
			{
				var blockSize = this.memoryManager.options.BlockSize;

				var block = (int)Math.DivRem(this.position, blockSize, out var index);

				if (block >= this.blocks.Count)
				{
					this.EnsureCapacity(end);
				}

				this.blocks[block][index] = value;
			}
			else
			{
				if (this.position >= this.largeBuffer.Length)
				{
					this.EnsureCapacity(end);
				}

				this.largeBuffer[this.position] = value;
			}

			this.position = end;

			if (this.position > this.length)
			{
				this.length = this.position;
			}
		}

		/// <summary>
		/// Reads a single byte from the current position in the stream.
		/// </summary>
		/// <returns>The byte at the current position, or -1 if the position is at the end of the stream.</returns>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public override int ReadByte()
		{
			return this.SafeReadByte(ref this.position);
		}

		/// <summary>
		/// Reads a single byte from the specified position in the stream.
		/// </summary>
		/// <param name="streamPosition">The position in the stream to read from.</param>
		/// <returns>The byte at the current position, or -1 if the position is at the end of the stream.</returns>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public int SafeReadByte(ref long streamPosition)
		{
			this.CheckDisposed();
			if (streamPosition == this.length)
			{
				return -1;
			}
			byte value;
			if (this.largeBuffer == null)
			{
				var blockAndOffset = this.GetBlockAndRelativeOffset(streamPosition);
				value = this.blocks[blockAndOffset.Block][blockAndOffset.Offset];
			}
			else
			{
				value = this.largeBuffer[streamPosition];
			}
			streamPosition++;
			return value;
		}

		/// <summary>
		/// Sets the length of the stream.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">value is negative or larger than <see cref="RecyclableMemoryStreamManager.Options.MaximumStreamCapacity"/>.</exception>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public override void SetLength(long value)
		{
			this.CheckDisposed();
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(value)} must be non-negative.");
			}

			this.EnsureCapacity(value);

			this.length = value;
			if (this.position > value)
			{
				this.position = value;
			}
		}

		/// <summary>
		/// Sets the position to the offset from the seek location.
		/// </summary>
		/// <param name="offset">How many bytes to move.</param>
		/// <param name="loc">From where.</param>
		/// <returns>The new position.</returns>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="offset"/> is larger than <see cref="RecyclableMemoryStreamManager.Options.MaximumStreamCapacity"/>.</exception>
		/// <exception cref="ArgumentException">Invalid seek origin.</exception>
		/// <exception cref="IOException">Attempt to set negative position.</exception>
		public override long Seek(long offset, SeekOrigin loc)
		{
			this.CheckDisposed();
			long newPosition = loc switch
			{
				SeekOrigin.Begin => offset,
				SeekOrigin.Current => offset + this.position,
				SeekOrigin.End => offset + this.length,
				_ => throw new ArgumentException("Invalid seek origin.", nameof(loc)),
			};
			if (newPosition < 0)
			{
				throw new IOException("Seek before beginning.");
			}
			this.position = newPosition;
			return this.position;
		}

		/// <summary>
		/// Synchronously writes this stream's bytes to the argument stream.
		/// </summary>
		/// <param name="stream">Destination stream.</param>
		/// <remarks>Important: This does a synchronous write, which may not be desired in some situations.</remarks>
		/// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public override void WriteTo(Stream stream)
		{
			this.WriteTo(stream, 0, this.length);
		}

		/// <summary>
		/// Synchronously writes this stream's bytes, starting at offset, for count bytes, to the argument stream.
		/// </summary>
		/// <param name="stream">Destination stream.</param>
		/// <param name="offset">Offset in source.</param>
		/// <param name="count">Number of bytes to write.</param>
		/// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="offset"/> is less than 0, or <paramref name="offset"/> + <paramref name="count"/> is beyond  this <paramref name="stream"/>'s length.
		/// </exception>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public void WriteTo(Stream stream, long offset, long count)
		{
			this.CheckDisposed();
			if (stream == null)
			{
				throw new ArgumentNullException(nameof(stream));
			}

			if (offset < 0 || offset + count > this.length)
			{
				throw new ArgumentOutOfRangeException(
					message: $"{nameof(offset)} must not be negative and {nameof(offset)} + {nameof(count)} must not exceed the length of the {nameof(stream)}.",
					innerException: null);
			}

			if (this.largeBuffer == null)
			{
				var blockAndOffset = this.GetBlockAndRelativeOffset(offset);
				long bytesRemaining = count;
				int currentBlock = blockAndOffset.Block;
				int currentOffset = blockAndOffset.Offset;

				while (bytesRemaining > 0)
				{
					byte[] block = this.blocks[currentBlock];
					int amountToCopy = (int)Math.Min((long)block.Length - currentOffset, bytesRemaining);
					stream.Write(block, currentOffset, amountToCopy);

					bytesRemaining -= amountToCopy;

					++currentBlock;
					currentOffset = 0;
				}
			}
			else
			{
				stream.Write(this.largeBuffer, (int)offset, (int)count);
			}
		}

		/// <summary>
		/// Writes bytes from the current stream to a destination <c>byte</c> array.
		/// </summary>
		/// <param name="buffer">Target buffer.</param>
		/// <remarks>The entire stream is written to the target array.</remarks>
		/// <exception cref="ArgumentNullException"><paramref name="buffer"/>> is null.</exception>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public void WriteTo(byte[] buffer)
		{
			this.WriteTo(buffer, 0, this.Length);
		}

		/// <summary>
		/// Writes bytes from the current stream to a destination <c>byte</c> array.
		/// </summary>
		/// <param name="buffer">Target buffer.</param>
		/// <param name="offset">Offset in the source stream, from which to start.</param>
		/// <param name="count">Number of bytes to write.</param>
		/// <exception cref="ArgumentNullException"><paramref name="buffer"/>> is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="offset"/> is less than 0, or <paramref name="offset"/> + <paramref name="count"/> is beyond this stream's length.
		/// </exception>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public void WriteTo(byte[] buffer, long offset, long count)
		{
			this.WriteTo(buffer, offset, count, 0);
		}

		/// <summary>
		/// Writes bytes from the current stream to a destination <c>byte</c> array.
		/// </summary>
		/// <param name="buffer">Target buffer.</param>
		/// <param name="offset">Offset in the source stream, from which to start.</param>
		/// <param name="count">Number of bytes to write.</param>
		/// <param name="targetOffset">Offset in the target byte array to start writing</param>
		/// <exception cref="ArgumentNullException"><c>buffer</c> is null</exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="offset"/> is less than 0, or <paramref name="offset"/> + <paramref name="count"/> is beyond this stream's length.
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		/// <paramref name="targetOffset"/> is less than 0, or <paramref name="targetOffset"/> + <paramref name="count"/> is beyond the target <paramref name="buffer"/>'s length.
		/// </exception>
		/// <exception cref="ObjectDisposedException">Object has been disposed.</exception>
		public void WriteTo(byte[] buffer, long offset, long count, int targetOffset)
		{
			this.CheckDisposed();
			if (buffer == null)
			{
				throw new ArgumentNullException(nameof(buffer));
			}

			if (offset < 0 || offset + count > this.length)
			{
				throw new ArgumentOutOfRangeException(
					message: $"{nameof(offset)} must not be negative and {nameof(offset)} + {nameof(count)} must not exceed the length of the stream.",
					innerException: null);
			}

			if (targetOffset < 0 || count + targetOffset > buffer.Length)
			{
				throw new ArgumentOutOfRangeException(
					message: $"{nameof(targetOffset)} must not be negative and {nameof(targetOffset)} + {nameof(count)} must not exceed the length of the target {nameof(buffer)}.",
					innerException: null);
			}

			if (this.largeBuffer == null)
			{
				var blockAndOffset = this.GetBlockAndRelativeOffset(offset);
				long bytesRemaining = count;
				int currentBlock = blockAndOffset.Block;
				int currentOffset = blockAndOffset.Offset;
				int currentTargetOffset = targetOffset;

				while (bytesRemaining > 0)
				{
					byte[] block = this.blocks[currentBlock];
					int amountToCopy = (int)Math.Min((long)block.Length - currentOffset, bytesRemaining);
					Buffer.BlockCopy(block, currentOffset, buffer, currentTargetOffset, amountToCopy);

					bytesRemaining -= amountToCopy;

					++currentBlock;
					currentOffset = 0;
					currentTargetOffset += amountToCopy;
				}
			}
			else
			{
				this.AssertLengthIsSmall();
				Buffer.BlockCopy(this.largeBuffer, (int)offset, buffer, targetOffset, (int)count);
			}
		}
		#endregion

		#region Helper Methods
		private bool Disposed => this.disposed;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CheckDisposed()
		{
			if (this.Disposed)
			{
				this.ThrowDisposedException();
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private void ThrowDisposedException()
		{
			throw new ObjectDisposedException($"The stream with Id {this.id} and Tag {this.tag} is disposed.");
		}

		private int InternalRead(byte[] buffer, int offset, int count, long fromPosition)
		{
			if (this.length - fromPosition <= 0)
			{
				return 0;
			}

			int amountToCopy;

			if (this.largeBuffer == null)
			{
				var blockAndOffset = this.GetBlockAndRelativeOffset(fromPosition);
				int bytesWritten = 0;
				int bytesRemaining = (int)Math.Min(count, this.length - fromPosition);

				while (bytesRemaining > 0)
				{
					byte[] block = this.blocks[blockAndOffset.Block];
					amountToCopy = Math.Min(block.Length - blockAndOffset.Offset,
												bytesRemaining);
					Buffer.BlockCopy(block, blockAndOffset.Offset, buffer,
									 bytesWritten + offset, amountToCopy);

					bytesWritten += amountToCopy;
					bytesRemaining -= amountToCopy;

					++blockAndOffset.Block;
					blockAndOffset.Offset = 0;
				}
				return bytesWritten;
			}
			amountToCopy = (int)Math.Min(count, this.length - fromPosition);
			Buffer.BlockCopy(this.largeBuffer, (int)fromPosition, buffer, offset, amountToCopy);
			return amountToCopy;
		}

		private int InternalRead(Span<byte> buffer, long fromPosition)
		{
			if (this.length - fromPosition <= 0)
			{
				return 0;
			}

			int amountToCopy;

			if (this.largeBuffer == null)
			{
				var blockAndOffset = this.GetBlockAndRelativeOffset(fromPosition);
				int bytesWritten = 0;
				int bytesRemaining = (int)Math.Min(buffer.Length, this.length - fromPosition);

				while (bytesRemaining > 0)
				{
					byte[] block = this.blocks[blockAndOffset.Block];
					amountToCopy = Math.Min(block.Length - blockAndOffset.Offset,
											bytesRemaining);
					block.AsSpan(blockAndOffset.Offset, amountToCopy)
						.CopyTo(buffer.Slice(bytesWritten));

					bytesWritten += amountToCopy;
					bytesRemaining -= amountToCopy;

					++blockAndOffset.Block;
					blockAndOffset.Offset = 0;
				}
				return bytesWritten;
			}
			amountToCopy = (int)Math.Min(buffer.Length, this.length - fromPosition);
			this.largeBuffer.AsSpan((int)fromPosition, amountToCopy).CopyTo(buffer);
			return amountToCopy;
		}

		private struct BlockAndOffset(int block, int offset)
		{
			public int Block = block;
			public int Offset = offset;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private BlockAndOffset GetBlockAndRelativeOffset(long offset)
		{
			var blockSize = this.memoryManager.options.BlockSize;
			int blockIndex = (int)Math.DivRem(offset, blockSize, out long offsetIndex);
			return new BlockAndOffset(blockIndex, (int)offsetIndex);
		}

		private void EnsureCapacity(long newCapacity)
		{
			if (newCapacity > this.memoryManager.options.MaximumStreamCapacity && this.memoryManager.options.MaximumStreamCapacity > 0)
			{
				this.memoryManager.ReportStreamOverCapacity(this.id, this.tag, newCapacity, this.AllocationStack);

				throw new OutOfMemoryException($"Requested capacity is too large: {newCapacity}. Limit is {this.memoryManager.options.MaximumStreamCapacity}.");
			}

			if (this.largeBuffer != null)
			{
				if (newCapacity > this.largeBuffer.Length)
				{
					var newBuffer = this.memoryManager.GetLargeBuffer(newCapacity, this.id, this.tag);
					Debug.Assert(this.length <= Int32.MaxValue);
					this.InternalRead(newBuffer, 0, (int)this.length, 0);
					this.ReleaseLargeBuffer();
					this.largeBuffer = newBuffer;
				}
			}
			else
			{
				// Let's save some re-allocation of the blocks list
				var blocksRequired = (newCapacity / this.memoryManager.options.BlockSize) + 1;
				if (this.blocks.Capacity < blocksRequired)
				{
					this.blocks.Capacity = (int)blocksRequired;
				}
				while (this.Capacity64 < newCapacity)
				{
					this.blocks.Add((this.memoryManager.GetBlock()));
				}
			}
		}

		/// <summary>
		/// Release the large buffer (either stores it for eventual release or returns it immediately).
		/// </summary>
		private void ReleaseLargeBuffer()
		{
			Debug.Assert(this.largeBuffer != null);

			if (this.memoryManager.options.AggressiveBufferReturn)
			{
				this.memoryManager.ReturnLargeBuffer(this.largeBuffer!, this.id, this.tag);
			}
			else
			{
				// We most likely will only ever need space for one
				this.dirtyBuffers ??= new List<byte[]>(1);
				this.dirtyBuffers.Add(this.largeBuffer!);
			}

			this.largeBuffer = null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AssertLengthIsSmall()
		{
			Debug.Assert(this.length <= Int32.MaxValue, "this.length was assumed to be <= Int32.MaxValue, but was larger.");
		}
		#endregion
	}
}
