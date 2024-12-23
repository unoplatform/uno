#if !__ANDROID__
#nullable enable
using System;
using System.Buffers;
using System.Linq;
using System.Runtime.InteropServices;

namespace Windows.UI.Xaml.Media.Imaging;

public partial class RenderTargetBitmap
{
	private unsafe class UnmanagedArrayOfBytes
	{
		public nint Pointer;
		public int Length { get; }

		public UnmanagedArrayOfBytes(int length)
		{
			Length = length;
			Pointer = Marshal.AllocHGlobal(length);
			GC.AddMemoryPressure(length);
		}

		public byte this[int index]
		{
			get
			{
				return ((byte*)Pointer.ToPointer())[index];
			}
			set
			{
				((byte*)Pointer.ToPointer())[index] = value;
			}
		}

		~UnmanagedArrayOfBytes()
		{
			Marshal.FreeHGlobal(Pointer);
			GC.RemoveMemoryPressure(Length);
		}
	}

	// This is to avoid LOH array allocations
	// https://stackoverflow.com/questions/52190423/c-sharp-access-unmanaged-array-using-memoryt-or-arraysegmentt
	private sealed unsafe class UnmanagedMemoryManager<T> : MemoryManager<T>
		where T : unmanaged
	{
		private readonly T* _pointer;
		private readonly int _length;

		/// <summary>
		/// Create a new UnmanagedMemoryManager instance at the given pointer and size
		/// </summary>
		public UnmanagedMemoryManager(T* pointer, int length)
		{
			if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
			_pointer = pointer;
			_length = length;
		}
		/// <summary>
		/// Obtains a span that represents the region
		/// </summary>
		public override Span<T> GetSpan() => new(_pointer, _length);

		/// <summary>
		/// Provides access to a pointer that represents the data (note: no actual pin occurs)
		/// </summary>
		public override MemoryHandle Pin(int elementIndex = 0)
		{
			if (elementIndex < 0 || elementIndex >= _length)
				throw new ArgumentOutOfRangeException(nameof(elementIndex));
			return new MemoryHandle(_pointer + elementIndex);
		}

		/// <summary>
		/// Has no effect
		/// </summary>
		public override void Unpin() { }

		/// <summary>
		/// Releases all resources associated with this object
		/// </summary>
		protected override void Dispose(bool disposing) { }
	}
}
#endif
